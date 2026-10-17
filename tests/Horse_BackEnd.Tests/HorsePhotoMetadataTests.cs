using System.Net;
using System.Net.Http.Json;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class HorsePhotoMetadataTests
{
    [Fact]
    public async Task ProfileLinksLatestPhotoToStoredBlobWithoutExposingOtherOwnersIntake()
    {
        await using var factory = new ClubFactory(); var owner = await factory.User(Role.HorseOwner); var other = await factory.User(Role.HorseOwner);
        var horse = await factory.Horse(owner); var otherHorse = await factory.Horse(other); var newest = Guid.NewGuid().ToString("N");
        await WorkerTests.Read(factory, async db =>
        {
            db.Attachments.Add(new Attachment { RegistrationId = horse.RegistrationId, StorageName = Guid.NewGuid().ToString("N"), Type = AttachmentType.HorsePhoto, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1) });
            db.Attachments.Add(new Attachment { RegistrationId = horse.RegistrationId, StorageName = newest, Type = AttachmentType.HorsePhoto, FileName = "horse.png", ContentType = "image/png", Length = 123 });
            db.Attachments.Add(new Attachment { RegistrationId = horse.RegistrationId, StorageName = Guid.NewGuid().ToString("N"), Type = AttachmentType.Certificate, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(1) });
            await db.SaveChangesAsync();
        });
        using var client = await factory.Client(owner);
        var profile = await client.GetFromJsonAsync<HorseDetailResponse>($"/api/horses/{horse.Id}", ClubFactory.Json);
        Assert.NotNull(profile!.Photo); Assert.Equal(newest, profile.Photo.StorageName); Assert.Equal("image/png", profile.Photo.ContentType);
        Assert.Equal($"/api/horses/{horse.Id}/photo", profile.Photo.ContentUrl); Assert.Equal("horse.png", profile.Photo.FileName);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/horses/{otherHorse.Id}")).StatusCode);
        using var otherClient = await factory.Client(other);
        Assert.Null((await otherClient.GetFromJsonAsync<HorseDetailResponse>($"/api/horses/{otherHorse.Id}", ClubFactory.Json))!.Photo);
    }
}
