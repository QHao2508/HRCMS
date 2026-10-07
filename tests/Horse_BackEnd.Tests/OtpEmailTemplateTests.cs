using System.Net.Mail;
using System.Text;
using HorseClub.BLL.Messaging;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class OtpEmailTemplateTests
{
    [Theory]
    [InlineData(MessageKey.VerificationEmailSubject, MessageKey.VerificationEmailBody)]
    [InlineData(MessageKey.PasswordResetEmailSubject, MessageKey.PasswordResetEmailBody)]
    [InlineData(MessageKey.StaffInvitationEmailSubject, MessageKey.StaffInvitationEmailBody)]
    public void EachOtpEmailIncludesDynamicCodeExpiryAndEncodesUserContent(MessageKey subjectKey, MessageKey bodyKey)
    {
        var subject = Messages.Get(subjectKey);
        var username = "<img src=x onerror=alert(1)> {{OTP_CODE}}";
        var body = Messages.Get(bodyKey, "731805", 7, username);
        var html = OtpEmailRenderer.Render(subject, body)!;
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("731805", html);
        Assert.DoesNotContain("229403", html);
        Assert.Contains("7 ph", html); // Vietnamese text may be HTML entity encoded.
        Assert.Contains("#0056b3", html);
        Assert.Contains("border: 2px dashed", html);
        Assert.Contains("#fff9e6", html);
        Assert.DoesNotContain("{{TITLE}}", html);
        Assert.DoesNotContain("[cite:", html);
        Assert.DoesNotContain("<img", html);
        if (bodyKey == MessageKey.StaffInvitationEmailBody)
        {
            Assert.Contains("&lt;img", html);
            Assert.Contains("{{OTP_CODE}}", html); // User data is not substituted a second time.
        }
        else Assert.DoesNotContain("{{OTP_CODE}}", html);
    }

    [Fact]
    public async Task SmtpMessageIsMultipartWithPlainTextAndHtmlAlternatives()
    {
        var subject = Messages.Get(MessageKey.VerificationEmailSubject);
        var body = Messages.Get(MessageKey.VerificationEmailBody, "731805", 10, "owner");
        using var message = ClubMailSender.CreateMessage(new EmailOptions { FromAddress = "sender@example.test" }, "recipient@example.test", subject, body);
        Assert.False(message.IsBodyHtml);
        Assert.Equal(body, message.Body);
        var alternate = Assert.Single(message.AlternateViews);
        Assert.Equal("text/html", alternate.ContentType.MediaType);
        using var reader = new StreamReader(alternate.ContentStream, Encoding.UTF8, leaveOpen: true);
        Assert.Contains("731805", await reader.ReadToEndAsync());
        alternate.ContentStream.Position = 0;
        var root = Path.Combine(Path.GetTempPath(), "hrcms-email-template-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var smtp = new SmtpClient { DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory, PickupDirectoryLocation = root };
            await smtp.SendMailAsync(message);
            var content = await File.ReadAllTextAsync(Assert.Single(Directory.GetFiles(root)));
            Assert.Contains("multipart/alternative", content);
            Assert.Contains("text/plain", content);
            Assert.Contains("text/html", content);
        }
        finally
        {
            foreach (var file in Directory.GetFiles(root)) File.Delete(file);
            Directory.Delete(root);
        }
    }

    [Fact]
    public void NonOtpMessagesStayPlainText()
    {
        Assert.Null(OtpEmailRenderer.Render("A general message", "A message without a generated OTP."));
        using var message = ClubMailSender.CreateMessage(new EmailOptions { FromAddress = "sender@example.test" }, "recipient@example.test", "General", "<plain-text-content>");
        Assert.Empty(message.AlternateViews);
        Assert.Equal("<plain-text-content>", message.Body);
        Assert.False(message.IsBodyHtml);
    }
}
