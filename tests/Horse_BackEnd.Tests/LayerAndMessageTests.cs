using System.Globalization;
using Horse_BackEnd.Data;
using Horse_BackEnd.Services;
using HorseClub.BLL.Messaging;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class LayerAndMessageTests
{
    [Fact]
    public void LayerDependenciesHaveNoReverseReferences()
    {
        var api = typeof(Program).Assembly;
        var business = typeof(AuthenticationService).Assembly;
        var data = typeof(ClubDbContext).Assembly;
        Assert.Equal("HorseClub.BLL", business.GetName().Name);
        Assert.Equal("HorseClub.DAL", data.GetName().Name);
        Assert.Contains(api.GetReferencedAssemblies(), x => x.Name == business.GetName().Name);
        Assert.Contains(business.GetReferencedAssemblies(), x => x.Name == data.GetName().Name);
        Assert.DoesNotContain(business.GetReferencedAssemblies(), x => x.Name == api.GetName().Name);
        Assert.DoesNotContain(data.GetReferencedAssemblies(), x => x.Name == business.GetName().Name || x.Name == api.GetName().Name);
    }

    [Fact]
    public void EveryMessageKeyHasANonEmptyTemplate()
    {
        foreach (var key in Enum.GetValues<MessageKey>()) Assert.False(string.IsNullOrWhiteSpace(Messages.Get(key)));
    }

    [Fact]
    public void DynamicMessagesFormatValuesAndEmailExpirationWithInvariantCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN");
            Assert.Equal("Staff must be an active Trainer.", Messages.Get(MessageKey.StaffMustBeAnActive, "Trainer"));
            var expires = new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
            var email = Messages.Get(MessageKey.YourCodeExpiresAtNeverShareThisCode, "Verify", "123456", expires);
            Assert.Contains("Verify code: 123456", email);
            Assert.Contains(expires.ToString("O", CultureInfo.InvariantCulture), email);
            Assert.DoesNotContain("{0}", email);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}
