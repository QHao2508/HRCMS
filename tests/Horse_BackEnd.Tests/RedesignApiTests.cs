using System.Net.Http.Json;
using System.Text.Json;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Horse_BackEnd.Tests;

/// <summary>Dữ liệu chỉ đọc bổ sung cho giao diện mới: cột phụ của danh sách ngựa (B1), số đếm trên Tổng quan (B2), tổng hợp sức khỏe (B3) và lọc giống/chuồng (B4).</summary>
public sealed class RedesignApiTests
{
    [Fact]
    public async Task HorseListCarriesStallLeadTrainerNextSessionAndPhotoFlagWithinScope()
    {
        await using var f = new ClubFactory();
        var owner = await f.User(Role.HorseOwner);
        var trainer = await f.User(Role.Trainer);
        var head = await f.User(Role.HeadTrainer);
        var horse = await f.Horse(owner, head, trainer);
        var bare = await f.Horse(await f.User(Role.HorseOwner));
        var next = DateTimeOffset.UtcNow.AddDays(1);
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            var stable = new Stable { Name = "Khu A" };
            var stall = new Stall { StableId = stable.Id, Name = "A-04" };
            db.Stables.Add(stable); db.Stalls.Add(stall);
            db.Occupancies.Add(new StallOccupancy { StallId = stall.Id, HorseId = horse.Id });
            var template = new TrainingTemplate { Name = "Test" };
            var plan = new TrainingPlan { HorseId = horse.Id, TrainerId = trainer.Id, TemplateId = template.Id };
            db.Templates.Add(template); db.Plans.Add(plan);
            db.Sessions.Add(new TrainingSession { HorseId = horse.Id, PlanId = plan.Id, ScheduledAt = next.AddDays(2), Status = SessionStatus.Planned });
            db.Sessions.Add(new TrainingSession { HorseId = horse.Id, PlanId = plan.Id, ScheduledAt = next, Status = SessionStatus.Assigned });
            db.Sessions.Add(new TrainingSession { HorseId = horse.Id, PlanId = plan.Id, ScheduledAt = DateTimeOffset.UtcNow.AddDays(-1), Status = SessionStatus.Assigned });
            db.Sessions.Add(new TrainingSession { HorseId = horse.Id, PlanId = plan.Id, ScheduledAt = next.AddHours(-1), Status = SessionStatus.Skipped });
            await db.SaveChangesAsync();
        }
        using var manager = await f.Client();
        var items = (await manager.GetFromJsonAsync<JsonElement>("/api/horses?pageSize=50")).GetProperty("items").EnumerateArray().ToList();
        var row = items.Single(x => x.GetProperty("id").GetGuid() == horse.Id);
        Assert.Equal("A-04", row.GetProperty("stallName").GetString());
        // Huấn luyện viên được ưu tiên hơn huấn luyện viên trưởng.
        Assert.Equal($"{trainer.FirstName} {trainer.LastName}", row.GetProperty("leadTrainerName").GetString());
        // Buổi gần nhất còn ở tương lai và chưa kết thúc; buổi quá khứ và buổi đã bỏ qua không tính.
        Assert.Equal(next.ToUnixTimeSeconds(), row.GetProperty("nextSessionAt").GetDateTimeOffset().ToUnixTimeSeconds());
        Assert.False(row.GetProperty("hasPhoto").GetBoolean());
        var empty = items.Single(x => x.GetProperty("id").GetGuid() == bare.Id);
        Assert.Equal(JsonValueKind.Null, empty.GetProperty("stallName").ValueKind);
        Assert.Equal(JsonValueKind.Null, empty.GetProperty("leadTrainerName").ValueKind);
        Assert.Equal(JsonValueKind.Null, empty.GetProperty("nextSessionAt").ValueKind);

        // Chủ ngựa khác không thấy ngựa ngoài phạm vi qua cột bổ sung.
        using var ownerClient = await f.Client(owner);
        var own = (await ownerClient.GetFromJsonAsync<JsonElement>("/api/horses")).GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(horse.Id, Assert.Single(own).GetProperty("id").GetGuid());

        // Hồ sơ chi tiết có tên chuồng đang ở cho phần đầu trang.
        var detail = await manager.GetFromJsonAsync<JsonElement>($"/api/horses/{horse.Id}");
        Assert.Equal("A-04", detail.GetProperty("horse").GetProperty("stallName").GetString());
    }

    [Fact]
    public async Task DashboardCountsOpenIncidentsInScopeAndRoleOnlyQueues()
    {
        await using var f = new ClubFactory();
        var owner = await f.User(Role.HorseOwner);
        var groom = await f.User(Role.Groom);
        var horse = await f.Horse(owner, groom);
        await f.Horse(await f.User(Role.HorseOwner));
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            db.Incidents.Add(new Incident { HorseId = horse.Id, ReporterId = groom.Id, OccurredAt = DateTimeOffset.UtcNow, Description = "Đi khập khiễng", RoutedTo = Role.Veterinarian });
            db.Incidents.Add(new Incident { HorseId = horse.Id, ReporterId = groom.Id, OccurredAt = DateTimeOffset.UtcNow, Description = "Đã xử lý", RoutedTo = Role.Veterinarian, Resolved = true });
            db.Inventory.Add(new InventoryItem { Name = "Cám", Category = "Thức ăn", Unit = "kg", Stock = 2, MinimumStock = 5 });
            db.Inventory.Add(new InventoryItem { Name = "Cỏ khô", Category = "Thức ăn", Unit = "kg", Stock = 50, MinimumStock = 5 });
            db.Inventory.Add(new InventoryItem { Name = "Đã lưu trữ", Category = "Khác", Unit = "cái", Stock = 0, MinimumStock = 5, Archived = true });
            db.Registrations.Add(new HorseRegistration { OwnerId = owner.Id, Name = "Sao Mai", Status = RegistrationStatus.PendingReview });
            await db.SaveChangesAsync();
        }
        using var manager = await f.Client();
        var m = await manager.GetFromJsonAsync<JsonElement>("/api/dashboard");
        Assert.True(m.GetProperty("openIncidents").GetInt32() >= 1);
        Assert.True(m.GetProperty("pendingReviews").GetInt32() >= 1);
        Assert.True(m.GetProperty("lowStockItems").GetInt32() >= 1);

        using var ownerClient = await f.Client(owner);
        var o = await ownerClient.GetFromJsonAsync<JsonElement>("/api/dashboard");
        Assert.Equal(1, o.GetProperty("openIncidents").GetInt32());
        Assert.Equal(0, o.GetProperty("pendingReviews").GetInt32());
        Assert.Equal(0, o.GetProperty("lowStockItems").GetInt32());

        using var groomClient = await f.Client(groom);
        var g = await groomClient.GetFromJsonAsync<JsonElement>("/api/dashboard");
        Assert.Equal(1, g.GetProperty("openIncidents").GetInt32());
        Assert.Equal(0, g.GetProperty("pendingReviews").GetInt32());
        Assert.True(g.GetProperty("lowStockItems").GetInt32() >= 1);
    }

    [Fact]
    public async Task HorseListFiltersByBreedAndStallAndSummaryStaysInScope()
    {
        await using var f = new ClubFactory();
        var owner = await f.User(Role.HorseOwner);
        var arabian = await f.Horse(owner);
        var thoroughbred = await f.Horse(owner);
        var other = await f.Horse(await f.User(Role.HorseOwner));
        Guid stallId;
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            (await db.Horses.FindAsync(arabian.Id))!.Breed = "Arabian";
            var t = (await db.Horses.FindAsync(thoroughbred.Id))!; t.Breed = "Thoroughbred"; t.HealthStatus = HealthStatus.Injured;
            (await db.Horses.FindAsync(other.Id))!.Breed = "Akhal-Teke";
            var stable = new Stable { Name = "Khu B" };
            var stall = new Stall { StableId = stable.Id, Name = "B-02" };
            var hidden = new Stall { StableId = stable.Id, Name = "B-09" };
            db.Stables.Add(stable); db.Stalls.AddRange(stall, hidden);
            db.Occupancies.Add(new StallOccupancy { StallId = stall.Id, HorseId = thoroughbred.Id });
            db.Occupancies.Add(new StallOccupancy { StallId = hidden.Id, HorseId = other.Id });
            await db.SaveChangesAsync(); stallId = stall.Id;
        }
        using var client = await f.Client(owner);
        var byBreed = (await client.GetFromJsonAsync<JsonElement>("/api/horses?breed=Arabian")).GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(arabian.Id, Assert.Single(byBreed).GetProperty("id").GetGuid());
        var byStall = (await client.GetFromJsonAsync<JsonElement>($"/api/horses?stallId={stallId}")).GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(thoroughbred.Id, Assert.Single(byStall).GetProperty("id").GetGuid());

        var summary = await client.GetFromJsonAsync<JsonElement>("/api/horses/summary");
        var health = summary.GetProperty("health");
        Assert.Equal(1, health.GetProperty("Fit").GetInt32());
        Assert.Equal(1, health.GetProperty("Injured").GetInt32());
        Assert.Equal(0, health.GetProperty("Isolated").GetInt32());
        // Giống và chuồng của ngựa chủ khác không lộ ra.
        Assert.Equal(new[] { "Arabian", "Thoroughbred" }, summary.GetProperty("breeds").EnumerateArray().Select(x => x.GetString()).ToArray());
        Assert.Equal("B-02", Assert.Single(summary.GetProperty("stalls").EnumerateArray().ToList()).GetProperty("name").GetString());
    }
}
