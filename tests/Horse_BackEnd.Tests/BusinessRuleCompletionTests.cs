using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Horse_BackEnd.Tests;

public sealed class BusinessRuleCompletionTests
{
    private static RegisterRequest Registration(string email,string username)=>new(email,username,"Test","Owner","0900000000","Test address",ClubFactory.Password,ClubFactory.Password);
    [Theory]
    [InlineData("name with spaces")]
    [InlineData("name@example.test")]
    [InlineData("tênngựa")]
    [InlineData("ab")]
    public async Task UsernameFormatIsEnforcedByBackend(string username)
    {
        await using var f=new ClubFactory(); using var c=f.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/api/auth/register",Registration("format@example.test",username))).StatusCode);
    }
    [Fact]
    public async Task RegistrationNormalizesIdentityAndConcurrentDuplicatesCreateOnlyOneAccount()
    {
        await using var f=new ClubFactory(); using var c=f.CreateClient();
        var responses=await Task.WhenAll(c.PostAsJsonAsync("/api/auth/register",Registration("Case@Example.Test"," Account_One ")),c.PostAsJsonAsync("/api/auth/register",Registration("case@example.test","account_one")));
        Assert.Single(responses,r=>r.StatusCode==HttpStatusCode.Created); Assert.Single(responses,r=>r.StatusCode==HttpStatusCode.Conflict);
        await WorkerTests.Read(f,async db=>{var u=Assert.Single(await db.Users.Where(u=>u.Email=="case@example.test").ToListAsync());Assert.Equal("account_one",u.UserName);});
        using var m=await f.Client();
        var emailConflict=await m.PostAsJsonAsync("/api/staff",new StaffRequest("case@example.test","other_staff","Staff","One","090","Address",Role.Groom),ClubFactory.Json);
        Assert.Equal(HttpStatusCode.Conflict,emailConflict.StatusCode);Assert.Equal("email_exists",(await emailConflict.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
        var nameConflict=await m.PostAsJsonAsync("/api/staff",new StaffRequest("other@example.test","ACCOUNT_ONE","Staff","One","090","Address",Role.Groom),ClubFactory.Json);
        Assert.Equal(HttpStatusCode.Conflict,nameConflict.StatusCode);Assert.Equal("username_exists",(await nameConflict.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
    }
    [Fact]
    public async Task StaffDisableRequiresHandoverOrEmergencyAndRevokesTheOldToken()
    {
        await using var f=new ClubFactory();using var m=await f.Client();var groom=await f.User(Role.Groom);var owner=await f.User(Role.HorseOwner);var h=await f.Horse(owner,groom);using var gc=await f.Client(groom);using var oc=await f.Client(owner);
        Assert.Equal(HttpStatusCode.Forbidden,(await oc.GetAsync($"/api/staff/{groom.Id}/workload")).StatusCode);
        var work=await m.GetFromJsonAsync<JsonElement>($"/api/staff/{groom.Id}/workload");Assert.Equal(1,work.GetProperty("assignments").GetInt32());Assert.True(work.GetProperty("hasOpenWork").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict,(await m.PutAsJsonAsync($"/api/staff/{groom.Id}/active",new ActiveRequest(false))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await m.PutAsJsonAsync($"/api/staff/{groom.Id}/active",new ActiveRequest(false,true," "))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await m.PutAsJsonAsync($"/api/staff/{groom.Id}/active",new ActiveRequest(false,true,"Emergency security review"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await gc.GetAsync("/api/auth/me")).StatusCode);
        await WorkerTests.Read(f,async db=>{Assert.True(await db.Assignments.AnyAsync(a=>a.HorseId==h.Id&&a.Active));Assert.Contains("Emergency",(await db.Audit.SingleAsync(a=>a.ReferenceId==groom.Id&&a.Action==AuditAction.StaffActiveChanged)).Detail);});
    }
    [Fact]
    public async Task GroomReplacementTransfersPendingCareAndDoesNotCompleteInProgressWork()
    {
        await using var f=new ClubFactory();using var m=await f.Client();var old=await f.User(Role.Groom);var next=await f.User(Role.Groom);var owner=await f.User(Role.HorseOwner);var h=await f.Horse(owner,old);
        var task=(await ClubFactory.Post(m,"/api/care/tasks",new CareRequest(h.Id,old.Id,null,CareType.Feeding,DateTimeOffset.UtcNow,"Feed",1))).GetProperty("id").GetGuid();
        await ClubFactory.Post(m,$"/api/horses/{h.Id}/assignments",new AssignmentRequest(next.Id,Role.Groom,DateOnly.FromDateTime(DateTime.UtcNow),"Handover"));
        await WorkerTests.Read(f,async db=>{var t=(await db.CareTasks.FindAsync(task))!;Assert.Equal(next.Id,t.GroomId);Assert.Equal(CareStatus.Pending,t.Status);});
        using var gc=await f.Client(next);await ClubFactory.Post(gc,$"/api/care/tasks/{task}/record",new CareCompletionRequest(CareStatus.InProgress,null,"Started"));
        Assert.Equal(HttpStatusCode.Conflict,(await m.PostAsJsonAsync($"/api/horses/{h.Id}/assignments",new AssignmentRequest(old.Id,Role.Groom,DateOnly.FromDateTime(DateTime.UtcNow),"Back"),ClubFactory.Json)).StatusCode);
        await WorkerTests.Read(f,async db=>Assert.Single(await db.Assignments.Where(a=>a.HorseId==h.Id&&a.Active&&a.Role==Role.Groom).ToListAsync(),a=>a.StaffId==next.Id));
        await ClubFactory.Post(m,$"/api/horses/{h.Id}/assignments",new AssignmentRequest(old.Id,Role.Groom,DateOnly.FromDateTime(DateTime.UtcNow),"Emergency handover",true,"Previous groom unavailable"));
        await WorkerTests.Read(f,async db=>{var t=(await db.CareTasks.FindAsync(task))!;Assert.Equal(old.Id,t.GroomId);Assert.Equal(CareStatus.InProgress,t.Status);Assert.Null(t.CompletedAt);});
    }
    [Fact]
    public async Task IncidentResolutionRequiresTextPersistsActorAndCannotBeRepeated()
    {
        await using var f=new ClubFactory();var owner=await f.User(Role.HorseOwner);var vet=await f.User(Role.Veterinarian);var groom=await f.User(Role.Groom);var h=await f.Horse(owner,vet,groom);using var gc=await f.Client(groom);using var vc=await f.Client(vet);
        var incident=(await ClubFactory.Post(gc,"/api/care/incidents",new IncidentRequest(h.Id,DateTimeOffset.UtcNow.AddMinutes(-1),IncidentType.CareObservation,"Observation",IncidentSeverity.NeedsReview,Role.Veterinarian))).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Forbidden,(await gc.PostAsJsonAsync($"/api/care/incidents/{incident}/resolve",new IncidentResolutionRequest("Done"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await vc.PostAsJsonAsync($"/api/care/incidents/{incident}/resolve",new IncidentResolutionRequest(" "))).StatusCode);
        await ClubFactory.Post(vc,$"/api/care/incidents/{incident}/resolve",new IncidentResolutionRequest(" Checked and routed to care "));
        Assert.Equal(HttpStatusCode.Conflict,(await vc.PostAsJsonAsync($"/api/care/incidents/{incident}/resolve",new IncidentResolutionRequest("Again"))).StatusCode);
        await WorkerTests.Read(f,async db=>{var i=(await db.Incidents.FindAsync(incident))!;Assert.Equal("Checked and routed to care",i.Resolution);Assert.Equal(vet.Id,i.ResolvedBy);Assert.NotNull(i.ResolvedAt);Assert.Equal(1,await db.Audit.CountAsync(a=>a.ReferenceId==incident&&a.Action==AuditAction.IncidentResolved));});
    }
    [Fact]
    public async Task PartialReceiptsLinkMovementsPreventOverReceiptAndPreserveArchivedItemName()
    {
        await using var f=new ClubFactory();using var m=await f.Client();var groom=await f.User(Role.Groom);using var gc=await f.Client(groom);
        var item=(await ClubFactory.Post(m,"/api/inventory",new InventoryRequest("Receipt item","Food","kg",0))).GetProperty("id").GetGuid();
        var request=(await ClubFactory.Post(gc,"/api/inventory/replenishments",new ReplenishmentRequestDto(item,10,"Original request"))).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict,(await m.PostAsJsonAsync($"/api/inventory/replenishments/{request}/receive",new ReplenishmentReceiptRequest(1,"Early"))).StatusCode);
        await ClubFactory.Post(m,$"/api/inventory/replenishments/{request}/review",new ReplenishmentReviewRequest(true,"Approved"));
        Assert.Equal(HttpStatusCode.Conflict,(await m.PostAsJsonAsync($"/api/inventory/{item}/archive",new{})).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await gc.PostAsJsonAsync($"/api/inventory/replenishments/{request}/receive",new ReplenishmentReceiptRequest(1,"Unauthorized"))).StatusCode);
        var first=await ClubFactory.Post(m,$"/api/inventory/replenishments/{request}/receive",new ReplenishmentReceiptRequest(4,"Batch 1"));Assert.Equal("PartiallyReceived",first.GetProperty("request").GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.BadRequest,(await m.PostAsJsonAsync($"/api/inventory/replenishments/{request}/receive",new ReplenishmentReceiptRequest(7,"Too much"))).StatusCode);
        await ClubFactory.Post(m,$"/api/inventory/replenishments/{request}/receive",new ReplenishmentReceiptRequest(6,"Batch 2"));
        Assert.Equal(HttpStatusCode.Conflict,(await m.PostAsJsonAsync($"/api/inventory/replenishments/{request}/receive",new ReplenishmentReceiptRequest(1,"Again"))).StatusCode);
        await WorkerTests.Read(f,async db=>{var r=(await db.Replenishments.FindAsync(request))!;Assert.Equal(ReplenishmentStatus.Received,r.Status);Assert.Equal(10,r.ReceivedQuantity);Assert.Equal("Original request",r.Notes);Assert.Equal("Approved",r.ReviewNotes);Assert.Equal(10,(await db.Inventory.FindAsync(item))!.Stock);Assert.Equal(2,await db.StockMovements.CountAsync(x=>x.ReplenishmentRequestId==request));});
        await ClubFactory.Post(m,$"/api/inventory/{item}/movements",new MovementRequest(-10,"Used"));await ClubFactory.Post(m,$"/api/inventory/{item}/archive",new{});
        var list=await gc.GetFromJsonAsync<JsonElement>("/api/inventory/replenishments");Assert.Equal("Receipt item",list.GetProperty("items")[0].GetProperty("itemName").GetString());
    }
    [Fact]
    public async Task CompetingReceiptsDoNotOverReceive()
    {
        await using var f=new ClubFactory();using var m=await f.Client();var item=(await ClubFactory.Post(m,"/api/inventory",new InventoryRequest("Concurrent","Food","kg",0))).GetProperty("id").GetGuid();var request=(await ClubFactory.Post(m,"/api/inventory/replenishments",new ReplenishmentRequestDto(item,10,"Need"))).GetProperty("id").GetGuid();await ClubFactory.Post(m,$"/api/inventory/replenishments/{request}/review",new ReplenishmentReviewRequest(true,"Approved"));
        var responses=await Task.WhenAll(Enumerable.Range(0,3).Select(_=>m.PostAsJsonAsync($"/api/inventory/replenishments/{request}/receive",new ReplenishmentReceiptRequest(6,"Receipt"))));Assert.Single(responses,r=>r.IsSuccessStatusCode);
        await WorkerTests.Read(f,async db=>{Assert.Equal(6,(await db.Inventory.FindAsync(item))!.Stock);Assert.Equal(6,(await db.Replenishments.FindAsync(request))!.ReceivedQuantity);Assert.Single(await db.StockMovements.Where(x=>x.ItemId==item).ToListAsync());});
    }
    [Fact]
    public async Task LifecycleMigrationPreservesLegacyRowsWithoutInventingHistory()
    {
        await using var f=new ClubFactory(); using var m=await f.Client();var owner=await f.User(Role.HorseOwner);var h=await f.Horse(owner);
        await WorkerTests.Read(f,async db=>{
            var item=new InventoryItem{Name="Legacy item",Stock=12};db.Inventory.Add(item);
            var request=new ReplenishmentRequest{ItemId=item.Id,RequestedBy=owner.Id,Quantity=5,Status=ReplenishmentStatus.Approved,Notes="Legacy approval"};db.Replenishments.Add(request);
            var incident=new Incident{HorseId=h.Id,ReporterId=owner.Id,Resolved=true,Description="Legacy incident",OccurredAt=DateTimeOffset.UtcNow};db.Incidents.Add(incident);
            await db.SaveChangesAsync();
            var previous=db.Database.GetMigrations().Where(x=>!x.EndsWith("_BusinessRuleLifecycle")).Last();
            var migrator=db.GetService<IMigrator>();await migrator.MigrateAsync(previous);await migrator.MigrateAsync();db.ChangeTracker.Clear();
            var r=(await db.Replenishments.FindAsync(request.Id))!;Assert.Equal("Legacy approval",r.Notes);Assert.Equal(0,r.ReceivedQuantity);Assert.Null(r.ReceivedAt);
            var i=(await db.Incidents.FindAsync(incident.Id))!;Assert.True(i.Resolved);Assert.Null(i.Resolution);Assert.Null(i.ResolvedAt);
            Assert.Equal(12,(await db.Inventory.FindAsync(item.Id))!.Stock);Assert.Null((await db.Horses.FindAsync(h.Id))!.ArchiveCategory);
        });
    }
    [Fact]
    public async Task ArchiveStoresExplicitReasonAndDoesNotChangeMedicalStatus()
    {
        await using var f=new ClubFactory();using var m=await f.Client();var owner=await f.User(Role.HorseOwner);var h=await f.Horse(owner);
        await ClubFactory.Post(m,$"/api/horses/{h.Id}/archive",new HorseArchiveRequest(HorseArchiveCategory.Transferred,"Moved to another club"));
        await WorkerTests.Read(f,async db=>{var horse=(await db.Horses.FindAsync(h.Id))!;Assert.Equal(HorseArchiveCategory.Transferred,horse.ArchiveCategory);Assert.Equal("Moved to another club",horse.ArchiveReason);Assert.NotNull(horse.ArchivedAt);Assert.NotNull(horse.ArchivedBy);Assert.Equal(HealthStatus.Fit,horse.HealthStatus);});
    }
    [Fact]
    public async Task IntervalOverlapBlocksHorseAndRiderButAllowsAdjacentSessionsAndChecksMidSessionRestriction()
    {
        await using var f=new ClubFactory();var owner=await f.User(Role.HorseOwner);var trainer=await f.User(Role.Trainer);var rider=await f.User(Role.WorkRider);var horse=await f.Horse(owner,trainer);using var tc=await f.Client(trainer);
        Guid planId=Guid.Empty;var at=DateTimeOffset.UtcNow.AddHours(1);
        await WorkerTests.Read(f,async db=>{var template=new TrainingTemplate{Name="Template",Goal="Goal"};db.Templates.Add(template);var plan=new TrainingPlan{HorseId=horse.Id,TrainerId=trainer.Id,TemplateId=template.Id,StartDate=DateOnly.FromDateTime(at.UtcDateTime),EndDate=DateOnly.FromDateTime(at.UtcDateTime).AddDays(2),Status=PlanStatus.Active};db.Plans.Add(plan);await db.SaveChangesAsync();planId=plan.Id;});
        SessionRequest Request(DateTimeOffset time,int? minutes=60)=>new(time,TrainingType.Trot,1000,Intensity.Light,"Sand","Target","",rider.Id,minutes);
        var session=await ClubFactory.Post(tc,$"/api/training/plans/{planId}/sessions",Request(at));Assert.Equal(at.AddMinutes(60).UtcTicks,session.GetProperty("scheduledEndAt").GetDateTimeOffset().UtcTicks);
        Assert.Equal(HttpStatusCode.Conflict,(await tc.PostAsJsonAsync($"/api/training/plans/{planId}/sessions",Request(at.AddMinutes(30)),ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await tc.PostAsJsonAsync($"/api/training/plans/{planId}/sessions",Request(at.AddMinutes(30),null),ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(tc,$"/api/training/plans/{planId}/sessions",Request(at.AddMinutes(60)));
        var nextHorse=await f.Horse(owner,trainer);Guid nextPlan=Guid.Empty;
        await WorkerTests.Read(f,async db=>{var p=new TrainingPlan{HorseId=nextHorse.Id,TrainerId=trainer.Id,TemplateId=(await db.Templates.FirstAsync()).Id,StartDate=DateOnly.FromDateTime(at.UtcDateTime),EndDate=DateOnly.FromDateTime(at.UtcDateTime).AddDays(2),Status=PlanStatus.Active};db.Plans.Add(p);await db.SaveChangesAsync();nextPlan=p.Id;});
        Assert.Equal(HttpStatusCode.Conflict,(await tc.PostAsJsonAsync($"/api/training/plans/{nextPlan}/sessions",Request(at.AddMinutes(30)),ClubFactory.Json)).StatusCode);
        await WorkerTests.Read(f,async db=>{var record=new MedicalRecord{HorseId=horse.Id,VeterinarianId=trainer.Id,ExaminationAt=at};db.MedicalRecords.Add(record);db.Restrictions.Add(new MedicalRestriction{HorseId=horse.Id,MedicalRecordId=record.Id,ValidFrom=at.AddHours(3).AddMinutes(30),BlockAllTraining=true,Reason="Rest"});await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.Conflict,(await tc.PostAsJsonAsync($"/api/training/plans/{planId}/sessions",Request(at.AddHours(3)),ClubFactory.Json)).StatusCode);
    }
}
