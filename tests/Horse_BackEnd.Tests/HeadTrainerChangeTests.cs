using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Horse_BackEnd.Tests;

/// <summary>Mỗi ngựa chỉ có một HeadTrainer; đổi người phải có lý do và báo cho chủ ngựa, người cũ, người mới.</summary>
public sealed class HeadTrainerChangeTests
{
    [Fact]
    public async Task ReplacingHeadTrainerRequiresReasonKeepsOneActiveAndNotifiesOwnerOldAndNew()
    {
        await using var f = new ClubFactory();
        var owner = await f.User(Role.HorseOwner); var oldHead = await f.User(Role.HeadTrainer); var newHead = await f.User(Role.HeadTrainer);
        var horse = await f.Horse(owner); using var manager = await f.Client();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var url = $"/api/horses/{horse.Id}/assignments";

        // Lần giao đầu tiên không cần lý do.
        await ClubFactory.Post(manager, url, new AssignmentRequest(oldHead.Id, Role.HeadTrainer, today, ""));
        // Xác nhận lại đúng người đang phụ trách không phải là đổi người: không cần lý do.
        await ClubFactory.Post(manager, url, new AssignmentRequest(oldHead.Id, Role.HeadTrainer, today, "Confirmed"));
        // Đổi sang người khác mà không có lý do bị từ chối, dữ liệu giữ nguyên.
        var missing = await manager.PostAsJsonAsync(url, new AssignmentRequest(newHead.Id, Role.HeadTrainer, today, "", ChangeReason: "   "), ClubFactory.Json);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal("HeadTrainerChangeReasonRequired", (await missing.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("messageKey").GetString());

        await ClubFactory.Post(manager, url, new AssignmentRequest(newHead.Id, Role.HeadTrainer, today, "", ChangeReason: "HLV cũ nghỉ phép dài hạn"));

        await WorkerTests.Read(f, async db =>
        {
            var heads = await db.Assignments.Where(x => x.HorseId == horse.Id && x.Role == Role.HeadTrainer).ToListAsync();
            Assert.Equal(newHead.Id, Assert.Single(heads, x => x.Active).StaffId);
            Assert.False(heads.Single(x => x.StaffId == oldHead.Id).Active);
            Assert.Equal("HLV cũ nghỉ phép dài hạn", heads.Single(x => x.Active).Notes);
            var codes = await db.Notifications.Where(x => x.ReferenceId == horse.Id).Select(x => new { x.RecipientId, x.MessageCode }).ToListAsync();
            Assert.Contains(codes, x => x.RecipientId == owner.Id && x.MessageCode == "HeadTrainerChangedForOwner");
            Assert.Contains(codes, x => x.RecipientId == oldHead.Id && x.MessageCode == "YouWereReplacedAsHeadTrainer");
            Assert.Contains(codes, x => x.RecipientId == newHead.Id && x.MessageCode == "YouAreNowHeadTrainer");
            var audit = await db.Audit.Where(x => x.ReferenceId == horse.Id && x.Action == AuditAction.HorseStaffAssigned).Select(x => x.Detail).ToListAsync();
            var change = JsonDocument.Parse(Assert.Single(audit, x => x.StartsWith('{'))).RootElement;
            Assert.Equal("HLV cũ nghỉ phép dài hạn", change.GetProperty("Reason").GetString());
            Assert.Equal(newHead.Id, change.GetProperty("To").GetGuid());
        });

        // HeadTrainer cũ không còn quyền chọn Trainer; người mới thì có.
        var trainer = await f.User(Role.Trainer);
        using var oldClient = await f.Client(oldHead);
        Assert.Equal(HttpStatusCode.Forbidden, (await oldClient.PostAsJsonAsync(url, new AssignmentRequest(trainer.Id, Role.Trainer, today, ""), ClubFactory.Json)).StatusCode);
        // Manager không thể tự thêm Trainer, kể cả khi ghi lý do.
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync(url, new AssignmentRequest(trainer.Id, Role.Trainer, today, "Need extra trainer", ChangeReason: "Need extra trainer"), ClubFactory.Json)).StatusCode);
    }
}
