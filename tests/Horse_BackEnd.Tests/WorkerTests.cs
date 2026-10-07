using System.Collections.Concurrent;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Horse_BackEnd.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class WorkerTests
{
    [Fact]
    public async Task ExpiredAndSupersededCodesAreDiscardedWithoutMailDelivery()
    {
        var sender = new TestMailSender(); await using var factory = new ClubFactory(mailSender: sender);
        var clock = new WorkerClock(); var owner = await factory.User(Role.HorseOwner);
        var expired = await SeedEmail(factory, clock, "expired"); var superseded = await SeedEmail(factory, clock, "superseded");
        var valid = await SeedEmail(factory, clock, "valid");
        await Read(factory, async db =>
        {
            var consumed = new EmailChallenge { UserId = owner.Id, Consumed = true, ExpiresAt = clock.GetUtcNow().AddMinutes(10) };
            db.Challenges.Add(consumed);
            var expiredRow = (await db.EmailMessages.FindAsync(expired.Id))!;
            expiredRow.ExpiresAt = clock.GetUtcNow(); expiredRow.Attempts = 8; expiredRow.NextAttemptAt = clock.GetUtcNow().AddDays(1);
            var replaced = (await db.EmailMessages.FindAsync(superseded.Id))!;
            replaced.ChallengeId = consumed.Id; replaced.NextAttemptAt = clock.GetUtcNow().AddDays(1);
            (await db.EmailMessages.FindAsync(valid.Id))!.ExpiresAt = clock.GetUtcNow().AddMinutes(1);
            await db.SaveChangesAsync();
        });
        using var worker = Email(factory, clock);
        Assert.Equal(3, await worker.RunOnce()); Assert.Equal(0, await worker.RunOnce());
        Assert.Equal("valid", Assert.Single(sender.Calls));
        await Read(factory, async db =>
        {
            foreach (var id in new[] { expired.Id, superseded.Id })
            {
                var row = (await db.EmailMessages.FindAsync(id))!;
                Assert.Equal(clock.GetUtcNow(), row.DiscardedAt); Assert.Null(row.SentAt); Assert.Equal("", row.Body);
            }
            Assert.NotNull((await db.EmailMessages.FindAsync(valid.Id))!.SentAt);
        });
    }

    [Fact]
    public async Task DisabledHostedWorkersDoNotProcessQueuedWork()
    {
        var sender = new TestMailSender(); await using var factory = new ClubFactory(mailSender: sender); var clock = new WorkerClock();
        var message = await SeedEmail(factory, clock);
        using var email = Email(factory, clock, new WorkerOptions { Enabled = false });
        using var reminder = Reminder(factory, clock, new WorkerOptions { Enabled = false });
        await email.StartAsync(CancellationToken.None); await reminder.StartAsync(CancellationToken.None);
        await Task.WhenAll(email.ExecuteTask!, reminder.ExecuteTask!).WaitAsync(TimeSpan.FromSeconds(5));
        await email.StopAsync(CancellationToken.None); await reminder.StopAsync(CancellationToken.None);
        Assert.Empty(sender.Calls);
        await Read(factory, async db => Assert.Null((await db.EmailMessages.FindAsync(message.Id))!.SentAt));
    }

    [Fact]
    public async Task WorkerMigrationPreservesLegacyEmailAndExistingRowsOnUpgrade()
    {
        await using var factory = new ClubFactory(); var clock = new WorkerClock();
        var email = await SeedEmail(factory, clock);
        await Read(factory, async db =>
        {
            var migrator = db.GetService<IMigrator>();
            var initial = db.Database.GetMigrations().First();
            await migrator.MigrateAsync(initial);
            await migrator.MigrateAsync();
            db.ChangeTracker.Clear();
            var row = (await db.EmailMessages.FindAsync(email.Id))!;
            Assert.Equal("code-secret", row.Body); Assert.Null(row.ChallengeId); Assert.Null(row.ExpiresAt); Assert.Null(row.DiscardedAt);
            Assert.NotEmpty(await db.Users.ToListAsync());
            Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync());
        });
    }

    [Fact]
    public async Task FailedEmailRetriesOnlyWhenDueAndRestartUsesPersistedSchedule()
    {
        var attempts = 0;
        var sender = new TestMailSender((_, _) => ++attempts == 1 ? Task.FromException(new InvalidOperationException("SMTP down")) : Task.CompletedTask);
        await using var factory = new ClubFactory(mailSender: sender);
        var clock = new WorkerClock(); var message = await SeedEmail(factory, clock);
        using var worker = Email(factory, clock);
        Assert.Equal(1, await worker.RunOnce());
        await Read(factory, async db =>
        {
            var row = (await db.EmailMessages.FindAsync(message.Id))!;
            Assert.Equal(1, row.Attempts); Assert.Null(row.SentAt);
            Assert.Equal(clock.GetUtcNow().AddMinutes(2), row.NextAttemptAt); Assert.Equal("code-secret", row.Body);
        });
        Assert.Equal(0, await worker.RunOnce()); Assert.Single(sender.Calls);
        clock.Advance(TimeSpan.FromMinutes(2));
        using var restarted = Email(factory, clock);
        Assert.Equal(1, await restarted.RunOnce()); Assert.Equal(0, await restarted.RunOnce());
        await Read(factory, async db =>
        {
            var row = (await db.EmailMessages.FindAsync(message.Id))!;
            Assert.Equal(clock.GetUtcNow(), row.SentAt); Assert.Equal("", row.Body); Assert.Null(row.NextAttemptAt);
        });
        Assert.Equal(2, sender.Calls.Count);
    }

    [Fact]
    public async Task EmailBatchSkipsFutureSentAndExhaustedMessagesAndStopsAtAttemptLimit()
    {
        var sender = new TestMailSender((subject, _) => subject == "fail" ? Task.FromException(new InvalidOperationException()) : Task.CompletedTask);
        await using var factory = new ClubFactory(mailSender: sender);
        var clock = new WorkerClock();
        var failed = await SeedEmail(factory, clock, "fail");
        var healthy = await SeedEmail(factory, clock, "healthy", clock.GetUtcNow().AddSeconds(1));
        await Read(factory, async db =>
        {
            db.EmailMessages.AddRange(new EmailMessage { Recipient = "unused@example.test", NextAttemptAt = clock.GetUtcNow().AddDays(1) },
                new EmailMessage { Recipient = "unused@example.test", SentAt = clock.GetUtcNow() },
                new EmailMessage { Recipient = "unused@example.test", Attempts = 2 });
            await db.SaveChangesAsync();
        });
        using var worker = Email(factory, clock, new WorkerOptions { EmailBatchSize = 1, MaxEmailAttempts = 2 });
        Assert.Equal(1, await worker.RunOnce()); Assert.Single(sender.Calls);
        Assert.Equal(1, await worker.RunOnce()); Assert.Equal(2, sender.Calls.Count); // Healthy email is not held behind a failed email.
        clock.Advance(TimeSpan.FromMinutes(2)); Assert.Equal(1, await worker.RunOnce());
        clock.Advance(TimeSpan.FromMinutes(4)); Assert.Equal(0, await worker.RunOnce());
        await Read(factory, async db =>
        {
            Assert.Equal(2, (await db.EmailMessages.FindAsync(failed.Id))!.Attempts);
            Assert.NotNull((await db.EmailMessages.FindAsync(healthy.Id))!.SentAt);
        });
        Assert.Equal(3, sender.Calls.Count);
    }

    [Fact]
    public async Task CancellationPreservesEarlierCommitAndPendingEmailCanResume()
    {
        using var cancellation = new CancellationTokenSource();
        var cancelSend = true;
        var sender = new TestMailSender((subject, token) =>
        {
            if (subject == "second" && cancelSend) { cancellation.Cancel(); token.ThrowIfCancellationRequested(); }
            return Task.CompletedTask;
        });
        await using var factory = new ClubFactory(mailSender: sender); var clock = new WorkerClock();
        var first = await SeedEmail(factory, clock, "first");
        var second = await SeedEmail(factory, clock, "second", clock.GetUtcNow().AddSeconds(1));
        using var worker = Email(factory, clock);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => worker.RunOnce(cancellation.Token));
        await Read(factory, async db =>
        {
            Assert.NotNull((await db.EmailMessages.FindAsync(first.Id))!.SentAt);
            var row = (await db.EmailMessages.FindAsync(second.Id))!;
            Assert.Null(row.SentAt); Assert.Equal(0, row.Attempts); Assert.Equal("code-secret", row.Body);
        });
        cancelSend = false;
        using var restarted = Email(factory, clock);
        Assert.Equal(1, await restarted.RunOnce());
        Assert.Single(sender.Calls, x => x == "first");
        Assert.Equal(1, factory.Services.GetRequiredService<WriteGate>().Semaphore.CurrentCount);
    }

    [Fact]
    public async Task MailSendTimeoutBecomesRetryAndReleasesGate()
    {
        var sender = new TestMailSender((_, token) => Task.Delay(Timeout.Infinite, token));
        await using var factory = new ClubFactory(mailSender: sender); var clock = new WorkerClock();
        var message = await SeedEmail(factory, clock);
        using var worker = Email(factory, clock, new WorkerOptions { EmailSendTimeoutSeconds = 1 });
        Assert.Equal(1, await worker.RunOnce().WaitAsync(TimeSpan.FromSeconds(10)));
        await Read(factory, async db =>
        {
            var row = (await db.EmailMessages.FindAsync(message.Id))!;
            Assert.Equal(1, row.Attempts); Assert.Null(row.SentAt); Assert.Equal(clock.GetUtcNow().AddMinutes(2), row.NextAttemptAt);
        });
        Assert.Equal(1, factory.Services.GetRequiredService<WriteGate>().Semaphore.CurrentCount);
    }

    [Fact]
    public async Task ReminderWaitsForActiveVetAndUsesLocalDateWithoutStarvingBatch()
    {
        await using var factory = new ClubFactory(new Dictionary<string, string?> { ["Business:TimeZoneId"] = "Asia/Ho_Chi_Minh" });
        var clock = new WorkerClock(); // 17:30 UTC = next local day.
        var owner = await factory.User(Role.HorseOwner); var vet = await factory.User(Role.Veterinarian);
        var withoutVet = await factory.Horse(owner); var withVet = await factory.Horse(owner, vet);
        var today = new DateOnly(2026, 10, 5);
        var waiting = new PreventiveCare { HorseId = withoutVet.Id, DueDate = today.AddDays(-5) };
        var due = new PreventiveCare { HorseId = withVet.Id, DueDate = today };
        await Read(factory, async db =>
        {
            db.PreventiveCare.AddRange(waiting, due, new PreventiveCare { HorseId = withVet.Id, DueDate = today.AddDays(1) },
                new PreventiveCare { HorseId = withVet.Id, DueDate = today, CompletedDate = today });
            await db.SaveChangesAsync();
        });
        using var worker = Reminder(factory, clock, new WorkerOptions { ReminderBatchSize = 1 });
        Assert.Equal(1, await worker.RunOnce()); Assert.Equal(0, await worker.RunOnce());
        await Read(factory, async db =>
        {
            Assert.False((await db.PreventiveCare.FindAsync(waiting.Id))!.ReminderSent);
            Assert.True((await db.PreventiveCare.FindAsync(due.Id))!.ReminderSent);
            Assert.Single(await db.Notifications.ToListAsync());
            db.Assignments.Add(new StaffAssignment { HorseId = withoutVet.Id, StaffId = vet.Id, Role = Role.Veterinarian });
            (await db.Users.FindAsync(vet.Id))!.Active = false; await db.SaveChangesAsync();
        });
        Assert.Equal(0, await worker.RunOnce());
        await Read(factory, async db => { (await db.Users.FindAsync(vet.Id))!.Active = true; await db.SaveChangesAsync(); });
        using var restarted = Reminder(factory, clock);
        Assert.Equal(1, await restarted.RunOnce()); Assert.Equal(0, await restarted.RunOnce());
        await Read(factory, async db => Assert.Single(await db.Notifications.Where(x => x.ReferenceId == waiting.Id && x.RecipientId == vet.Id).ToListAsync()));
    }

    [Fact]
    public async Task OverdueAndFollowUpRemindersRespectScopeStateAndDoNotRepeatAfterRestart()
    {
        await using var factory = new ClubFactory(); var clock = new WorkerClock();
        var data = await SeedReminders(factory, clock);
        using var worker = Reminder(factory, clock);
        Assert.Equal(4, await worker.RunOnce());
        using var restarted = Reminder(factory, clock);
        Assert.Equal(0, await restarted.RunOnce());
        await Read(factory, async db =>
        {
            var notes = await db.Notifications.ToListAsync();
            Assert.Equal(4, notes.Count);
            Assert.Equal(new[] { data.Trainer.Id, data.Rider.Id }.Order(), notes.Where(x => x.Type == NotificationType.SessionOverdue).Select(x => x.RecipientId).Order());
            Assert.Single(notes, x => x.Type == NotificationType.MedicalFollowUpDue && x.RecipientId == data.Vet.Id);
            Assert.Single(notes, x => x.Type == NotificationType.MedicalPreventiveDue && x.RecipientId == data.Vet.Id);
        });
    }

    [Fact]
    public async Task DevelopmentFileDeliveryCreatesOneFileAndClearsOutboxBody()
    {
        var directory = Path.Combine(Path.GetTempPath(), "horseclub-mail-" + Guid.NewGuid().ToString("N"));
        var sender = new ClubMailSender(Options.Create(new EmailOptions { Mode = EmailDeliveryMode.DevelopmentFile }), new MailEnvironment(directory));
        try
        {
            await using var factory = new ClubFactory(mailSender: sender); var clock = new WorkerClock();
            var message = await SeedEmail(factory, clock);
            using var worker = Email(factory, clock);
            Assert.Equal(1, await worker.RunOnce()); Assert.Equal(0, await worker.RunOnce());
            var file = Assert.Single(Directory.GetFiles(Path.Combine(directory, "App_Data/mail"), "*.eml"));
            var content = await File.ReadAllTextAsync(file);
            Assert.Contains("To: worker@example.test", content); Assert.Contains("Subject: message", content); Assert.Contains("code-secret", content);
            await Read(factory, async db => Assert.Equal("", (await db.EmailMessages.FindAsync(message.Id))!.Body));
        }
        finally
        {
            // Remove only files created in this test-owned directory, then the empty parents.
            var mail = Path.Combine(directory, "App_Data/mail");
            if (Directory.Exists(mail)) { foreach (var file in Directory.GetFiles(mail)) File.Delete(file); Directory.Delete(mail); }
            if (Directory.Exists(Path.Combine(directory, "App_Data"))) Directory.Delete(Path.Combine(directory, "App_Data"));
            if (Directory.Exists(directory)) Directory.Delete(directory);
        }
    }

    [Fact]
    public async Task EnabledSmtpWorkerRejectsMissingConfigurationAtStartup()
    {
        await using var factory = new ClubFactory(new Dictionary<string, string?>
        {
            ["Workers:Enabled"] = "true", ["Email:Mode"] = "Smtp", ["Email:Host"] = "", ["Email:From"] = ""
        });
        var error = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
        Assert.Contains(error.Failures, x => x.Contains("SMTP Host/From"));
    }

    internal static EmailWorker Email(ClubFactory factory, TimeProvider clock, WorkerOptions? options = null) => new(
        factory.Services.GetRequiredService<IServiceScopeFactory>(), factory.Services.GetRequiredService<WriteGate>(),
        NullLogger<EmailWorker>.Instance, clock, Options.Create(options ?? new WorkerOptions()));
    internal static ReminderWorker Reminder(ClubFactory factory, TimeProvider clock, WorkerOptions? options = null) => new(
        factory.Services.GetRequiredService<IServiceScopeFactory>(), factory.Services.GetRequiredService<WriteGate>(), clock,
        NullLogger<ReminderWorker>.Instance, Options.Create(options ?? new WorkerOptions()), factory.Services.GetRequiredService<IOptions<BusinessOptions>>());
    internal static async Task Read(ClubFactory factory, Func<ClubDbContext, Task> action)
    {
        using var scope = factory.Services.CreateScope(); await action(scope.ServiceProvider.GetRequiredService<ClubDbContext>());
    }
    internal static async Task<EmailMessage> SeedEmail(ClubFactory factory, WorkerClock clock, string subject = "message", DateTimeOffset? created = null)
    {
        var message = new EmailMessage { Recipient = "worker@example.test", Subject = subject, Body = "code-secret", CreatedAt = created ?? clock.GetUtcNow() };
        await Read(factory, async db => { db.EmailMessages.Add(message); await db.SaveChangesAsync(); }); return message;
    }
    internal static async Task<(User Trainer, User Rider, User Vet)> SeedReminders(ClubFactory factory, WorkerClock clock)
    {
        var owner = await factory.User(Role.HorseOwner); var trainer = await factory.User(Role.Trainer);
        var rider = await factory.User(Role.WorkRider); var vet = await factory.User(Role.Veterinarian);
        var horse = await factory.Horse(owner, trainer, vet); var archived = await factory.Horse(owner, trainer, vet);
        await Read(factory, async db =>
        {
            (await db.Horses.FindAsync(archived.Id))!.Archived = true;
            var template = new TrainingTemplate { Name = "Worker" }; db.Templates.Add(template);
            var active = new TrainingPlan { HorseId = horse.Id, TrainerId = trainer.Id, TemplateId = template.Id };
            var paused = new TrainingPlan { HorseId = horse.Id, TrainerId = trainer.Id, TemplateId = template.Id, Status = PlanStatus.Paused };
            var archivedPlan = new TrainingPlan { HorseId = archived.Id, TrainerId = trainer.Id, TemplateId = template.Id };
            db.Plans.AddRange(active, paused, archivedPlan);
            TrainingSession Session(TrainingPlan plan, SessionStatus status, int minutes) => new() { HorseId = plan.HorseId, PlanId = plan.Id, RiderId = rider.Id, Status = status, ScheduledAt = clock.GetUtcNow().AddMinutes(minutes) };
            db.Sessions.AddRange(Session(active, SessionStatus.Assigned, -61), Session(active, SessionStatus.Assigned, -60),
                Session(active, SessionStatus.Assigned, 10), Session(active, SessionStatus.Completed, -120),
                Session(active, SessionStatus.Planned, -120), Session(paused, SessionStatus.Assigned, -120), Session(archivedPlan, SessionStatus.Assigned, -120));
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            var medical = new MedicalRecord { HorseId = horse.Id, VeterinarianId = vet.Id }; db.MedicalRecords.Add(medical);
            db.Treatments.AddRange(new TreatmentPlan { HorseId = horse.Id, MedicalRecordId = medical.Id, FollowUpDate = today },
                new TreatmentPlan { HorseId = horse.Id, MedicalRecordId = medical.Id, FollowUpDate = today, Completed = true },
                new TreatmentPlan { HorseId = horse.Id, MedicalRecordId = medical.Id, FollowUpDate = today.AddDays(1) });
            db.PreventiveCare.AddRange(new PreventiveCare { HorseId = horse.Id, DueDate = today }, new PreventiveCare { HorseId = archived.Id, DueDate = today });
            await db.SaveChangesAsync();
        });
        return (trainer, rider, vet);
    }
}

internal sealed class WorkerClock : TimeProvider
{
    private DateTimeOffset now = new(2026, 10, 4, 17, 30, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(TimeSpan amount) => now += amount;
}
internal sealed class TestMailSender(Func<string, CancellationToken, Task>? send = null) : IClubMailSender
{
    public ConcurrentQueue<string> Calls { get; } = new();
    public Task Send(string recipient, string subject, string body, CancellationToken token)
    {
        Calls.Enqueue(subject); return send?.Invoke(subject, token) ?? Task.CompletedTask;
    }
}

internal sealed class MailEnvironment(string directory) : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "WorkerTests";
    public string ContentRootPath { get; set; } = directory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = directory;
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
