using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using HorseClub.BLL.Messaging;
using Microsoft.AspNetCore.Identity;

namespace HorseClub.BLL.Common;

public sealed class ClubStartup(IAuthRepository repository, IUnitOfWork unitOfWork, IConfiguration configuration)
{
    /// <summary>
    /// Kiểm tra/khởi tạo tài khoản quản lý theo bootstrap cấu hình; không tạo mật khẩu mặc định công khai.
    /// </summary>
    /// <param name="securityLimits">Giá trị kiểu SecurityOptions dùng trong Initialize.</param>
    /// <param name="token">Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng.</param>
    /// <remarks>Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.</remarks>
    public async Task Initialize(SecurityOptions securityLimits, CancellationToken token = default)
    {
        var email = configuration["Bootstrap:ManagerEmail"];
        var password = configuration["Bootstrap:ManagerPassword"];
        if (string.IsNullOrWhiteSpace(email) != string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(Messages.Get(MessageKey.ConfigureBothBootstrapManagerEmailAndBootstrapManagerPasswordOrNeither));
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || await repository.HasManagerAsync(token)) return;
        AuthenticationService.CheckPassword(password, securityLimits);
        Ensure.That(new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email), Messages.Get(MessageKey.BootstrapManagerEmailIsInvalid));
        var manager = new User
        {
            Email = AuthenticationService.Normalize(email),
            UserName = configuration["Bootstrap:ManagerUserName"] ?? "clubmanager",
            FirstName = configuration["Bootstrap:ManagerFirstName"] ?? "Club",
            LastName = configuration["Bootstrap:ManagerLastName"] ?? "Manager",
            Role = Role.ClubManager,
            EmailVerified = true
        };
        manager.PasswordHash = new PasswordHasher<User>().HashPassword(manager, password);
        repository.AddUser(manager);
        await unitOfWork.SaveChangesAsync(token);
    }

    /// <summary>
    /// Kiểm tra kết nối database cho health endpoint bằng cancellation token của request.
    /// </summary>
    /// <param name="token">Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng.</param>
    public Task<bool> CanConnect(CancellationToken token) => repository.CanConnectAsync(token);
}
