using System.Text.Json;
using HorseClub.BLL.Messaging;
using Xunit;

namespace Horse_BackEnd.Tests;

/// <summary>
/// Hợp đồng mã thông điệp giữa Backend và Frontend. File docs/contracts/server-message-keys.json liệt kê mọi
/// MessageKey client có thể nhận (lỗi API, thông báo) cùng số tham số; Frontend kiểm đủ bản dịch theo file này.
/// Khi thêm/sửa MessageKey: chạy test với HRCMS_UPDATE_CONTRACTS=1 để ghi lại file, rồi chép sang Frontend.
/// </summary>
public sealed class MessageContractTests
{
    private static string ContractPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "HorseClub.slnx"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "docs", "contracts", "server-message-keys.json");
    }

    internal static string ExpectedContract()
    {
        var keys = Enum.GetValues<MessageKey>().Where(x => !Messages.ServerOnly.Contains(x)).OrderBy(x => x.ToString(), StringComparer.Ordinal)
            .ToDictionary(x => x.ToString(), Messages.ArgumentCount);
        return JsonSerializer.Serialize(new { description = "MessageKey gửi cho client (messageKey của lỗi API, messageCode của thông báo) và số tham số {0}, {1}…", keys },
            new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }).Replace("\r\n", "\n") + "\n";
    }

    [Fact]
    public void ServerMessageContractFileIsCurrent()
    {
        var path = ContractPath(); var expected = ExpectedContract();
        if (Environment.GetEnvironmentVariable("HRCMS_UPDATE_CONTRACTS") == "1") File.WriteAllText(path, expected);
        Assert.True(File.Exists(path), "Chạy test với HRCMS_UPDATE_CONTRACTS=1 để tạo " + path);
        Assert.Equal(expected, File.ReadAllText(path).Replace("\r\n", "\n"));
    }

    [Fact]
    public void ClientVisibleCodesNeverUseServerOnlyKeys()
    {
        // Thông báo và lỗi API phải dùng khóa có trong hợp đồng; khóa server-only chỉ cho email/cấu hình/ghi chú.
        foreach (var key in Messages.ServerOnly) Assert.False(string.IsNullOrWhiteSpace(Messages.Get(key)));
        var notification = HorseClub.BLL.Common.ClubEvents.Create(Guid.NewGuid(), HorseClub.DAL.Enums.NotificationType.CareAssigned, MessageKey.ACareTaskWasAssignedToYou);
        Assert.Equal(nameof(MessageKey.ACareTaskWasAssignedToYou), notification.MessageCode);
        Assert.DoesNotContain(MessageKey.ACareTaskWasAssignedToYou, Messages.ServerOnly);
    }
}
