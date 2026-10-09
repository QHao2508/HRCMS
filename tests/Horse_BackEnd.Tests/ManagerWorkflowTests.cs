using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class ManagerWorkflowTests
{
    [Theory]
    [InlineData(Role.ClubManager)]
    [InlineData(Role.HeadTrainer)]
    [InlineData(Role.Trainer)]
    [InlineData(Role.WorkRider)]
    [InlineData(Role.Veterinarian)]
    [InlineData(Role.Groom)]
    public async Task InvitationsSupportAllInternalRolesAndProofIsEmailBoundAndSingleUse(Role invitedRole)
    {
        await using var f=new ClubFactory(new Dictionary<string,string?>{["Security:AuthRequestsPerMinute"]="1000"});using var manager=await f.Client();using var anonymous=f.CreateClient();
        foreach(var role in new[]{invitedRole})
        {
            var email=$"{role.ToString().ToLowerInvariant()}@invite.test";
            await ClubFactory.Post(manager,"/api/staff",new StaffRequest(email,"invited"+role,"Test",role.ToString(),"123456789","Address",role));
            Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.PostAsJsonAsync("/api/auth/login",new{email,password=ClubFactory.Password})).StatusCode);
            var code=await f.Code(email,"Invite");
            var wrong=await ClubFactory.Post(anonymous,"/api/auth/invitation/verify",new VerifyRequest(email,"000000"));Assert.False(wrong.GetProperty("verified").GetBoolean());
            var verification=await ClubFactory.Post(anonymous,"/api/auth/invitation/verify",new VerifyRequest(email,code));Assert.True(verification.GetProperty("verified").GetBoolean());
            var token=verification.GetProperty("setupToken").GetString()!;
            var replay=await ClubFactory.Post(anonymous,"/api/auth/invitation/verify",new VerifyRequest(email,code));Assert.False(replay.GetProperty("verified").GetBoolean());
            var other=await ClubFactory.Post(anonymous,"/api/auth/invitation/password",new InvitationPasswordRequest("other@invite.test",token,ClubFactory.Password,ClubFactory.Password));Assert.False(other.GetProperty("changed").GetBoolean());
            var result=await ClubFactory.Post(anonymous,"/api/auth/invitation/password",new InvitationPasswordRequest(email,token,ClubFactory.Password,ClubFactory.Password));Assert.True(result.GetProperty("changed").GetBoolean());
            var used=await ClubFactory.Post(anonymous,"/api/auth/invitation/password",new InvitationPasswordRequest(email,token,ClubFactory.Password,ClubFactory.Password));Assert.False(used.GetProperty("changed").GetBoolean());
            Assert.Equal(HttpStatusCode.OK,(await anonymous.PostAsJsonAsync("/api/auth/login",new{email,password=ClubFactory.Password})).StatusCode);
        }
        Assert.Equal(HttpStatusCode.BadRequest,(await manager.PostAsJsonAsync("/api/staff",new StaffRequest("owner@invite.test","invitedowner","Test","Owner","123456789","Address",Role.HorseOwner),ClubFactory.Json)).StatusCode);
    }

    [Fact]
    public async Task WebsiteUpdatesRequireManagerRejectStaleVersionsAndAuditTheActor()
    {
        await using var f=new ClubFactory();using var manager=await f.Client();using var anonymous=f.CreateClient();var owner=await f.User(Role.HorseOwner);using var ownerClient=await f.Client(owner);
        var initial=(await anonymous.GetFromJsonAsync<WebsiteResponse>("/api/website",ClubFactory.Json))!;
        var update=new WebsiteRequest(initial.Version,"New heading",initial.HeroDescription,initial.FeaturesTitle,initial.FeaturesDescription,initial.Features);
        Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.PutAsJsonAsync("/api/website",update)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await ownerClient.PutAsJsonAsync("/api/website",update)).StatusCode);
        var response=await manager.PutAsJsonAsync("/api/website",update);Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.Equal("New heading",(await anonymous.GetFromJsonAsync<WebsiteResponse>("/api/website",ClubFactory.Json))!.HeroTitle);
        Assert.Equal(HttpStatusCode.Conflict,(await manager.PutAsJsonAsync("/api/website",update)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await ownerClient.GetAsync("/api/audit")).StatusCode);
        var audit=await manager.GetFromJsonAsync<JsonElement>("/api/audit");Assert.Contains(audit.GetProperty("items").EnumerateArray(),x=>x.GetProperty("action").GetString()=="WebsiteContentUpdated");
        var filtered=await manager.GetFromJsonAsync<JsonElement>("/api/audit?search=not-existing-actor");Assert.Equal(0,filtered.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest,(await manager.GetAsync("/api/audit?from=2026-10-10&to=2026-10-01")).StatusCode);
    }

    [Fact]
    public async Task WebsiteUploadsValidateFileAndOnlyExposePublishedAssets()
    {
        await using var f=new ClubFactory();using var manager=await f.Client();using var anonymous=f.CreateClient();
        var initial=(await anonymous.GetFromJsonAsync<WebsiteResponse>("/api/website",ClubFactory.Json))!;
        var bytes=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLbtAAAAABJRU5ErkJggg==");
        using var body=new MultipartFormDataContent();body.Add(new ByteArrayContent(bytes),"file","crest.png");body.Add(new StringContent(initial.Version.ToString()),"version");
        var uploaded=await manager.PostAsync("/api/website/assets/Logo",body);Assert.Equal(HttpStatusCode.OK,uploaded.StatusCode);
        var saved=(await uploaded.Content.ReadFromJsonAsync<WebsiteResponse>(ClubFactory.Json))!;Assert.NotNull(saved.LogoVersion);
        var download=await anonymous.GetAsync("/api/website/assets/Logo");Assert.Equal(HttpStatusCode.OK,download.StatusCode);Assert.Equal(bytes,await download.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.BadRequest,(await anonymous.GetAsync("/api/website/assets/private-photo")).StatusCode);
        using var invalid=new MultipartFormDataContent();invalid.Add(new ByteArrayContent(bytes),"file","crest.webp");invalid.Add(new StringContent(saved.Version.ToString()),"version");
        Assert.Equal(HttpStatusCode.BadRequest,(await manager.PostAsync("/api/website/assets/Hero",invalid)).StatusCode);
    }

    [Fact]
    public async Task TrainingSearchUsesNamesBeforePaginationAndMaintainsHorseScope()
    {
        await using var f=new ClubFactory();var owner=await f.User(Role.HorseOwner);var outsider=await f.User(Role.HorseOwner);var trainer=await f.User(Role.Trainer);var rider=await f.User(Role.WorkRider);var horse=await f.Horse(owner,trainer,rider);
        using(var scope=f.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<ClubDbContext>();var template=new TrainingTemplate{Name="Template"};db.Templates.Add(template);
            var plan=new TrainingPlan{HorseId=horse.Id,TrainerId=trainer.Id,TemplateId=template.Id,Goal="Endurance"};db.Plans.Add(plan);
            db.Sessions.Add(new TrainingSession{HorseId=horse.Id,PlanId=plan.Id,RiderId=rider.Id,ScheduledAt=DateTimeOffset.UtcNow});await db.SaveChangesAsync();
        }
        using var client=await f.Client(owner);using var denied=await f.Client(outsider);
        var plans=await client.GetFromJsonAsync<JsonElement>("/api/training/plans?search=Thunder&pageSize=1");Assert.Equal(1,plans.GetProperty("total").GetInt32());Assert.Equal("Thunder",plans.GetProperty("items")[0].GetProperty("horseName").GetString());Assert.Equal("Test Trainer",plans.GetProperty("items")[0].GetProperty("trainerName").GetString());
        var sessions=await client.GetFromJsonAsync<JsonElement>("/api/training/sessions?search=WorkRider&pageSize=1");Assert.Equal(1,sessions.GetProperty("total").GetInt32());Assert.Equal("Test WorkRider",sessions.GetProperty("items")[0].GetProperty("riderName").GetString());
        var scoped=await denied.GetFromJsonAsync<JsonElement>("/api/training/plans?search=Thunder");Assert.Equal(0,scoped.GetProperty("total").GetInt32());
    }
}
