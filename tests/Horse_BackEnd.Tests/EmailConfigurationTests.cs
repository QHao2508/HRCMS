using Microsoft.Extensions.Configuration;
using HorseClub.DAL.Enums;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class EmailConfigurationTests
{
    [Fact]
    public void UserSecretsStyleKeysBindSmtpSettingsAndGmailRequiresTls()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:Provider"] = "Smtp",
            ["Email:FromAddress"] = "sender@example.test",
            ["Email:FromName"] = "HRCMS",
            ["Email:Smtp:Host"] = "smtp.gmail.com",
            ["Email:Smtp:Port"] = "587",
            ["Email:Smtp:Username"] = "sender@example.test",
            ["Email:Smtp:Password"] = "test-only-app-password",
            ["Email:Smtp:EnableSsl"] = "true"
        }).Build();
        var settings = configuration.GetSection(EmailOptions.Section).Get<EmailOptions>()!;
        Assert.Equal(EmailDeliveryMode.Smtp, settings.Provider);
        Assert.Equal("sender@example.test", settings.FromAddress);
        Assert.Equal("HRCMS", settings.FromName);
        Assert.Equal(587, settings.Smtp.Port);
        Assert.Equal("sender@example.test", settings.Smtp.Username);
        Assert.True(settings.CanDeliver(false));
        settings.Smtp.EnableSsl = false;
        Assert.False(settings.CanDeliver(false));
        settings.Smtp.EnableSsl = true;
        settings.Smtp.Port = 0;
        Assert.False(settings.CanDeliver(false));
    }
}
