using System.Data.Common;
using System.Diagnostics;
using System.Net.Http.Json;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Horse_BackEnd.Tests;

public sealed class PerformanceTests(ITestOutputHelper output)
{
    [SqlServerFact]
    public async Task DashboardUsesOneAggregateCommandAndPreservesOwnerScope()
    {
        var commands = new CommandCounter();
        await using var factory = new ClubFactory(interceptor: commands);
        var owner = await factory.User(Role.HorseOwner);
        var other = await factory.User(Role.HorseOwner);
        var trainer = await factory.User(Role.Trainer);
        var rider = await factory.User(Role.WorkRider);
        var groom = await factory.User(Role.Groom);
        var horse = await factory.Horse(owner, trainer, groom);
        await factory.Horse(other);
        var now = DateTimeOffset.UtcNow;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            var template = new TrainingTemplate { Name = "Test" };
            var plan = new TrainingPlan { HorseId = horse.Id, TrainerId = trainer.Id, TemplateId = template.Id };
            db.Templates.Add(template); db.Plans.Add(plan);
            db.Sessions.Add(new TrainingSession { HorseId = horse.Id, PlanId = plan.Id, RiderId = rider.Id, ScheduledAt = now.AddMinutes(-1), Status = SessionStatus.Assigned });
            db.CareTasks.Add(new CareTask { HorseId = horse.Id, GroomId = groom.Id, ScheduledAt = now });
            var examination = new MedicalRecord { HorseId = horse.Id, ExaminationAt = now };
            db.MedicalRecords.Add(examination);
            db.Restrictions.Add(new MedicalRestriction { HorseId = horse.Id, MedicalRecordId = examination.Id, ValidFrom = now.AddDays(-1), TrainingLock = true });
            db.Notifications.Add(new Notification { RecipientId = owner.Id });
            db.Notifications.Add(new Notification { RecipientId = other.Id });
            await db.SaveChangesAsync();
        }
        using var client = await factory.Client(owner);
        commands.Reset();
        var watch = Stopwatch.StartNew();
        var response = await client.GetFromJsonAsync<DashboardResponse>("/api/dashboard", ClubFactory.Json);
        watch.Stop();
        Assert.NotNull(response);
        Assert.Equal(new DashboardResponse(1, 1, 1, 1, 1, 1, 1), response);
        // One identity lookup plus one SQL command containing all seven counts.
        Assert.Equal(2, commands.Readers);
        output.WriteLine($"Dashboard: {commands.Readers} SQL readers including identity; {watch.Elapsed.TotalMilliseconds:F1} ms on local SQL Server.");

        using var emptyClient = await factory.Client(await factory.User(Role.HorseOwner));
        commands.Reset();
        Assert.Equal(new DashboardResponse(0, 0, 0, 0, 0, 0, 0),
            await emptyClient.GetFromJsonAsync<DashboardResponse>("/api/dashboard", ClubFactory.Json));
        Assert.Equal(2, commands.Readers);
    }

    [SqlServerFact]
    public async Task SlowEmailDoesNotBlockAnUnrelatedWriteOnTheSameHost()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sender = new TestMailSender(async (_, token) => { entered.TrySetResult(); await release.Task.WaitAsync(token); });
        await using var factory = new ClubFactory(mailSender: sender);
        using var manager = await factory.Client();
        var clock = new WorkerClock();
        await WorkerTests.SeedEmail(factory, clock);
        using var worker = WorkerTests.Email(factory, clock);
        var delivery = worker.RunOnce();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var write = await manager.PostAsJsonAsync("/api/care/stables", new NameRequest("Concurrent stable"))
                .WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(write.IsSuccessStatusCode, await write.Content.ReadAsStringAsync());
            Assert.False(delivery.IsCompleted);
        }
        finally { release.TrySetResult(); await delivery; }
    }

    [SqlServerFact]
    public async Task PaginatedReadsDoNotTrackReturnedEntities()
    {
        await using var factory = new ClubFactory();
        var owner = await factory.User(Role.HorseOwner);
        await factory.Horse(owner);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        var pager = scope.ServiceProvider.GetRequiredService<PageReader>();
        var page = await pager.Page(db.Horses.OrderBy(x => x.Id), 1, 10);
        Assert.Single(page.Items);
        Assert.Empty(db.ChangeTracker.Entries<Horse>());
    }

    private sealed class CommandCounter : DbCommandInterceptor
    {
        private int readers;
        public int Readers => Volatile.Read(ref readers);
        public void Reset() => Interlocked.Exchange(ref readers, 0);
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref readers);
            return ValueTask.FromResult(result);
        }
    }
}
