using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class WorkerSqlServerTests
{
    [SqlServerFact]
    public async Task SeparateHostsDoNotSendSameEmailWhileFirstDeliveryIsInFlight()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sender = new TestMailSender(async (_, token) => { entered.TrySetResult(); await release.Task.WaitAsync(token); });
        await using var factory = new ClubFactory(mailSender: sender); var clock = new WorkerClock();
        var message = await WorkerTests.SeedEmail(factory, clock);
        await using var replica = factory.Replica();
        using var first = WorkerTests.Email(factory, clock); using var second = WorkerTests.Email(replica, clock);
        var inFlight = first.RunOnce();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(0, await second.RunOnce().WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Single(sender.Calls);
        }
        finally { release.TrySetResult(); await inFlight; }
        Assert.Equal(0, await second.RunOnce());
        await WorkerTests.Read(factory, async db => Assert.NotNull((await db.EmailMessages.FindAsync(message.Id))!.SentAt));
        Assert.Single(sender.Calls);
    }

    [SqlServerFact]
    public async Task SeparateHostsReminderProducesOneNotificationPerRecipientAndEvent()
    {
        await using var factory = new ClubFactory(); var clock = new WorkerClock();
        await WorkerTests.SeedReminders(factory, clock);
        await using var replica = factory.Replica();
        using var first = WorkerTests.Reminder(factory, clock); using var second = WorkerTests.Reminder(replica, clock);
        var outcomes = await Task.WhenAll(first.RunOnce(), second.RunOnce());
        Assert.Equal(4, outcomes.Sum());
        Assert.Equal(0, await first.RunOnce()); Assert.Equal(0, await second.RunOnce());
        await WorkerTests.Read(factory, async db =>
        {
            var notes = await db.Notifications.ToListAsync(); Assert.Equal(4, notes.Count);
            Assert.Equal(4, notes.Select(x => (x.RecipientId, x.Type, x.ReferenceId)).Distinct().Count());
        });
    }

    [SqlServerFact]
    public async Task FailedReminderCommitRollsBackFlagAndNotificationsThenCanRetry()
    {
        await using var factory = new ClubFactory(); var clock = new WorkerClock();
        await WorkerTests.SeedReminders(factory, clock);
        await WorkerTests.Read(factory, async db => await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailWorkerNotification ON Notifications AFTER INSERT AS THROW 51000, 'Injected worker failure', 1;"));
        using var worker = WorkerTests.Reminder(factory, clock);
        await Assert.ThrowsAsync<DbUpdateException>(() => worker.RunOnce());
        await WorkerTests.Read(factory, async db =>
        {
            Assert.Empty(await db.Notifications.ToListAsync());
            Assert.All(await db.PreventiveCare.ToListAsync(), x => Assert.False(x.ReminderSent));
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER FailWorkerNotification");
        });
        Assert.Equal(4, await worker.RunOnce()); Assert.Equal(0, await worker.RunOnce());
    }
}
