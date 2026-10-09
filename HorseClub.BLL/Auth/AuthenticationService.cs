using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace HorseClub.BLL.Auth;

public sealed class AuthenticationService(IAuthRepository repository, IUnitOfWork unitOfWork, TimeProvider clock, IOptions<SecurityOptions> options, IDataProtectionProvider protection, CurrentUser current, PageReader pager, ClubEvents events, PendingRegistrationCleanup cleanup) : IAuthenticationService
{
    /// <summary>
    /// Đối chiếu tài khoản, trạng thái kích hoạt và SecurityStamp để xác định token còn hợp lệ; trả null khi phiên bị thu hồi.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="stamp">Giá trị kiểu string? dùng trong ValidateSession.</param>
    public async Task<User?> ValidateSession(Guid id, string? stamp)
    {
        var user = await repository.GetSessionUserAsync(id);
        return user is { Active: true, EmailVerified: true } && user.SecurityStamp == stamp ? user : null;
    }
    private readonly SecurityOptions settings = options.Value;
    private readonly PasswordHasher<User> hasher = new();
    private readonly PasswordHasher<EmailChallenge> challengeHasher = new();
    /// <summary>
    /// Trim khoảng trắng và chuyển email/tên đăng nhập về chữ thường để tra cứu và kiểm tra trùng thống nhất.
    /// </summary>
    /// <param name="value">Giá trị kiểu string dùng trong Normalize.</param>
    public static string Normalize(string value) => value.Trim().ToLowerInvariant();
    /// <summary>
    /// Chuyển User thành UserResponse; chỉ trả các trường hồ sơ được phép, không đưa password hash hoặc SecurityStamp ra API.
    /// </summary>
    /// <param name="u">Giá trị kiểu User dùng trong View.</param>
    public static UserResponse View(User u) => new(u.Id, u.Email, u.UserName, u.FirstName, u.LastName, u.Phone, u.Address, u.Role, u.EmailVerified, u.Active);

    /// <summary>
    /// Kiểm tra chính sách mật khẩu/căn cước, giải phóng đăng ký quá hạn, kiểm tra trùng rồi tạo chủ ngựa và xếp email OTP vào hàng đợi trong cùng transaction.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.</remarks>
    public async Task<User> Register(RegisterRequest r)
    {
        Ensure.That(r.Password == r.ConfirmPassword, Messages.Get(MessageKey.PasswordsDoNotMatch));
        ValidatePassword(r.Password);
        Ensure.That(!settings.RequireNationalId || !string.IsNullOrWhiteSpace(r.NationalId), Messages.Get(MessageKey.NationalIDIsRequiredByClubPolicy));
        if (!string.IsNullOrWhiteSpace(r.NationalId)) Ensure.That(r.NationalId.Length == settings.NationalIdDigits && r.NationalId.All(char.IsAsciiDigit), Messages.Get(MessageKey.InvalidNationalIDFormat));
        var email = Normalize(r.Email); var name = Normalize(r.UserName);
        await cleanup.RemoveExpired(100, email: email, userName: name);
        Ensure.That(!await repository.HasIdentityConflictAsync(email, name), Messages.Get(MessageKey.EmailOrUsernameIsAlreadyRegistered), 409, "account_exists");
        var user = new User { Email = email, UserName = name, FirstName = r.FirstName.Trim(), LastName = r.LastName.Trim(), Phone = r.Phone.Trim(), Address = r.Address.Trim(), Role = Role.HorseOwner, CreatedAt = clock.GetUtcNow() };
        user.PasswordHash = hasher.HashPassword(user, r.Password);
        if (!string.IsNullOrWhiteSpace(r.NationalId)) user.NationalIdProtected = protection.CreateProtector("HorseClub.PersonalData.NationalId").Protect(r.NationalId);
        repository.AddUser(user);
        await Challenge(user, ChallengePurpose.Verify);
        await unitOfWork.SaveChangesAsync();
        return user;
    }
    /// <summary>
    /// Tạo tài khoản nhân viên theo vai trò quản lý chọn, chưa đặt mật khẩu; sinh lời mời OTP để nhân viên tự kích hoạt.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.</remarks>
    public async Task<User> CreateStaff(StaffRequest r)
    {
        Ensure.That(Enum.IsDefined(r.Role) && r.Role != Role.HorseOwner, Messages.Get(MessageKey.UseThisEndpointForInternalStaffRolesOnly));
        var email = Normalize(r.Email); var name = Normalize(r.UserName);
        Ensure.That(!await repository.HasIdentityConflictAsync(email, name), Messages.Get(MessageKey.EmailOrUsernameIsAlreadyRegistered), 409, "account_exists");
        var user = new User { Email = email, UserName = name, FirstName = r.FirstName.Trim(), LastName = r.LastName.Trim(), Phone = r.Phone, Address = r.Address, Role = r.Role };
        // Staff choose their own password with a one-use email invitation; no shared initial password.
        repository.AddUser(user);
        await Challenge(user, ChallengePurpose.Invite);
        await unitOfWork.SaveChangesAsync();
        return user;
    }
    /// <summary>
    /// Giới hạn tần suất gửi lại, vô hiệu mã cũ, sinh OTP 6 chữ số, lưu hash và tạo EmailMessage. OTP đăng ký không được vượt hạn tài khoản 24 giờ.
    /// </summary>
    /// <param name="user">Tài khoản đã tra cứu/kiểm; response chỉ được lấy trường cho phép.</param>
    /// <param name="purpose">Enum mục đích OTP Verify/Reset/Invite; ngăn dùng mã của luồng khác.</param>
    public async Task Challenge(User user, ChallengePurpose purpose)
    {
        var now = clock.GetUtcNow();
        // A generic response also applies to known/throttled accounts, avoiding email enumeration.
        if (await repository.HasRecentChallengeAsync(user.Id, purpose, now.AddSeconds(-settings.ResendSeconds))) return;
        var previous = await repository.GetPendingChallengesAsync(user.Id, purpose);
        foreach (var p in previous) p.Consumed = true;
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var minutes = purpose switch { ChallengePurpose.Invite => settings.InvitationMinutes, ChallengePurpose.Reset => settings.ResetMinutes, _ => settings.VerificationMinutes };
        var expires = now.AddMinutes(minutes);
        if (purpose == ChallengePurpose.Verify && expires > user.CreatedAt.AddHours(settings.PendingRegistrationHours))
            expires = user.CreatedAt.AddHours(settings.PendingRegistrationHours);
        var challenge = new EmailChallenge { UserId = user.Id, Purpose = purpose, CreatedAt = now, ExpiresAt = expires };
        challenge.CodeHash = challengeHasher.HashPassword(challenge, code);
        repository.AddEmailChallenge(challenge);
        var (subject, body) = purpose switch
        {
            ChallengePurpose.Verify => (MessageKey.VerificationEmailSubject, MessageKey.VerificationEmailBody),
            ChallengePurpose.Reset => (MessageKey.PasswordResetEmailSubject, MessageKey.PasswordResetEmailBody),
            ChallengePurpose.Invite => (MessageKey.StaffInvitationEmailSubject, MessageKey.StaffInvitationEmailBody),
            _ => throw new ArgumentOutOfRangeException(nameof(purpose))
        };
        repository.AddEmailMessage(new EmailMessage { Recipient = user.Email, Subject = Messages.Get(subject), Body = Messages.Get(body, code, minutes, user.UserName), ChallengeId = challenge.Id, ExpiresAt = challenge.ExpiresAt });
    }
    /// <summary>
    /// Kiểm tra mã OTP mới nhất theo đúng mục đích, hạn dùng và số lần thử; tăng số lần thử, so sánh hash, đánh dấu dùng một lần khi thành công hoặc vượt giới hạn.
    /// </summary>
    /// <param name="user">Tài khoản đã tra cứu/kiểm; response chỉ được lấy trường cho phép.</param>
    /// <param name="purpose">Enum mục đích OTP Verify/Reset/Invite; ngăn dùng mã của luồng khác.</param>
    /// <param name="code">OTP 6 chữ số theo đúng mục đích; không log hoặc lưu vào URL.</param>
    public async Task<bool> Consume(User user, ChallengePurpose purpose, string code)
    {
        var c = await repository.GetLatestChallengeAsync(user.Id, purpose);
        if (c is null || c.ExpiresAt <= clock.GetUtcNow() || c.Attempts >= settings.MaxCodeAttempts) return false;
        c.Attempts++;
        // Invalid formats still consume an attempt, preventing unlimited guessing.
        var ok = code.Length == 6 && code.All(char.IsAsciiDigit)
            && challengeHasher.VerifyHashedPassword(c, c.CodeHash, code) != PasswordVerificationResult.Failed;
        if (ok || c.Attempts >= settings.MaxCodeAttempts) c.Consumed = true;
        return ok;
    }
    /// <summary>
    /// Kiểm tra username/email và password hash, xử lý khóa đăng nhập; trả LoginAttempt phân biệt mật khẩu sai, cần xác thực, đăng ký quá hạn và tài khoản hợp lệ.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.</remarks>
    public async Task<LoginAttempt> Login(LoginRequest r)
    {
        // Keep the existing JSON field "email" compatible, while accepting a username too.
        var identifier = Normalize(r.Email);
        var user = await repository.GetLoginUserAsync(identifier);
        if (user is null)
        {
            // Hash verification work is still performed for unknown email addresses.
            var dummy = new User(); hasher.HashPassword(dummy, r.Password);
            return new(LoginStatus.InvalidCredentials);
        }
        if (user.LockedUntil > clock.GetUtcNow()) return new(LoginStatus.InvalidCredentials);
        if (!user.Active || (!user.EmailVerified && user.Role != Role.HorseOwner) || string.IsNullOrEmpty(user.PasswordHash) || hasher.VerifyHashedPassword(user, user.PasswordHash, r.Password) == PasswordVerificationResult.Failed)
        {
            user.FailedLogins++;
            if (user.FailedLogins >= settings.MaxLoginAttempts) { user.LockedUntil = clock.GetUtcNow().AddMinutes(settings.LockoutMinutes); user.FailedLogins = 0; }
            await unitOfWork.SaveChangesAsync(); return new(LoginStatus.InvalidCredentials);
        }
        user.FailedLogins = 0; user.LockedUntil = null;
        await unitOfWork.SaveChangesAsync();
        return new(user.EmailVerified ? LoginStatus.Authenticated
            : user.CreatedAt <= clock.GetUtcNow().AddHours(-settings.PendingRegistrationHours) ? LoginStatus.RegistrationExpired
            : LoginStatus.VerificationRequired, user);
    }
    /// <summary>
    /// Kiểm tra độ mạnh, hash mật khẩu mới, xác thực tài khoản và đổi SecurityStamp để thu hồi các phiên cũ; xóa bộ đếm đăng nhập sai.
    /// </summary>
    /// <param name="user">Tài khoản đã tra cứu/kiểm; response chỉ được lấy trường cho phép.</param>
    /// <param name="password">Mật khẩu trong bộ nhớ cho kiểm/hash; không ghi ra log hoặc response.</param>
    public void SetPassword(User user, string password)
    {
        ValidatePassword(password);
        user.PasswordHash = hasher.HashPassword(user, password);
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.EmailVerified = true; user.FailedLogins = 0; user.LockedUntil = null;
    }
    /// <summary>
    /// Tạo danh tính claims từ tài khoản đã kiểm tra, gồm user ID, username, role và stamp để API cấp bearer token.
    /// </summary>
    /// <param name="user">Tài khoản đã tra cứu/kiểm; response chỉ được lấy trường cho phép.</param>
    public static ClaimsPrincipal Principal(User user) => new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Role, user.Role.ToString()), new Claim("stamp", user.SecurityStamp)
    ], IdentityConstants.BearerScheme));
    /// <summary>
    /// Áp dụng SecurityOptions hiện tại vào kiểm tra độ dài, chữ hoa, chữ thường và chữ số của mật khẩu.
    /// </summary>
    /// <param name="password">Mật khẩu trong bộ nhớ cho kiểm/hash; không ghi ra log hoặc response.</param>
    public void ValidatePassword(string password) => CheckPassword(password, settings);
    /// <summary>
    /// Kiểm tra mật khẩu theo một SecurityOptions được truyền vào; ném lỗi nghiệp vụ nếu không đáp ứng chính sách.
    /// </summary>
    /// <param name="password">Mật khẩu trong bộ nhớ cho kiểm/hash; không ghi ra log hoặc response.</param>
    /// <param name="settings">Các giới hạn/chính sách cấu hình áp dụng tại thời điểm chạy.</param>
    public static void CheckPassword(string password, SecurityOptions settings) => Ensure.That(password.Length >= settings.PasswordMinLength && password.Length <= settings.PasswordMaxLength && password.Any(char.IsUpper) && password.Any(char.IsLower) && password.Any(char.IsDigit),
        Messages.Get(MessageKey.PasswordNeedsCharactersIncludingUppercaseLowercaseAndADigit, settings.PasswordMinLength, settings.PasswordMaxLength));

    /// <summary>
    /// Bao bọc kết quả đăng ký thành phản hồi Created chứa hồ sơ; đăng ký không tự cấp token đăng nhập.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    public async Task<OperationResult> RegisterAccount(RegisterRequest r)
    { return OperationResult.Created("/api/auth/me", AuthenticationService.View(await Register(r))); }

    /// <summary>
    /// Chỉ xác thực chủ ngựa đang hoạt động và còn hạn đăng ký; tiêu thụ OTP Verify, cập nhật EmailVerified và trả cờ verified.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.</remarks>
    public async Task<OperationResult> VerifyEmail(VerifyRequest r)
    {
        var u = await repository.GetUserByEmailAsync(AuthenticationService.Normalize(r.Email));
        var ok = u is { Active: true, EmailVerified: false, Role: Role.HorseOwner }
            && u.CreatedAt > clock.GetUtcNow().AddHours(-settings.PendingRegistrationHours) && await Consume(u, ChallengePurpose.Verify, r.Code);
        if (ok) u!.EmailVerified = true;
        await unitOfWork.SaveChangesAsync();
        return OperationResult.Ok(new VerificationResponse(ok));
    }

    /// <summary>
    /// Tạo lại OTP Verify cho chủ ngựa chưa xác thực còn hạn; luôn trả thông báo chung để không tiết lộ email có tồn tại hay không.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.</remarks>
    public async Task<OperationResult> ResendVerification(EmailRequest r)
    {
        var u = await repository.GetUserByEmailAsync(AuthenticationService.Normalize(r.Email));
        if (u is { Active: true, EmailVerified: false, Role: Role.HorseOwner }
            && u.CreatedAt > clock.GetUtcNow().AddHours(-settings.PendingRegistrationHours)) await Challenge(u, ChallengePurpose.Verify);
        await unitOfWork.SaveChangesAsync(); return OperationResult.Ok(new MessageResponse(Messages.Get(MessageKey.IfEligibleACodeWillBeEmailed)));
    }

    /// <summary>
    /// Xếp OTP Reset cho tài khoản đang hoạt động và đã xác thực; trả thông báo chung cả khi tài khoản không đủ điều kiện.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.</remarks>
    public async Task<OperationResult> ForgotPassword(EmailRequest r)
    {
        var u = await repository.GetUserByEmailAsync(AuthenticationService.Normalize(r.Email));
        if (u is { Active: true, EmailVerified: true }) await Challenge(u, ChallengePurpose.Reset);
        await unitOfWork.SaveChangesAsync(); return OperationResult.Ok(new MessageResponse(Messages.Get(MessageKey.IfEligibleAResetCodeWillBeEmailed)));
    }

    /// <summary>
    /// Lấy tài khoản đã xác thực của request và chuyển sang DTO hồ sơ an toàn.
    /// </summary>
    public async Task<UserResponse> GetProfile()
    { return AuthenticationService.View(await current.Get()); }

    /// <summary>
    /// Đổi SecurityStamp của tài khoản hiện tại để các access/refresh token cũ bị từ chối.
    /// </summary>
    /// <remarks>Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.</remarks>
    public async Task<OperationResult> Logout()
    {
        (await current.Get()).SecurityStamp = Guid.NewGuid().ToString();
        await unitOfWork.SaveChangesAsync(); return OperationResult.NoContent();
    }

    /// <summary>
    /// Phân trang tài khoản nhân viên cho quản lý, hỗ trợ lọc role và trả trạng thái xác thực/kích hoạt.
    /// </summary>
    /// <param name="page">Giá trị kiểu int? dùng trong ListStaff.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListStaff.</param>
    /// <param name="role">Role enum chính xác của backend để kiểm quyền/lọc dữ liệu.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).</remarks>
    public async Task<PageResponse<StaffResponse>> ListStaff(int? page, int? pageSize, Role? role)
    {
        Ensure.Role(await current.Get(), Role.ClubManager);
        return PageReader.Map(await pager.Page(page, pageSize, (p, size) => repository.ListStaffAsync(role, false, p, size)), x => new StaffResponse(x.Id, x.UserName, x.FirstName, x.LastName, x.Email, x.Role, x.Active, x.EmailVerified));
    }

    /// <summary>
    /// Trả danh bạ nhân viên đang hoạt động phục vụ chọn người phân công; lọc role và phân trang, không trả thông tin xác thực nhạy cảm.
    /// </summary>
    /// <param name="role">Role enum chính xác của backend để kiểm quyền/lọc dữ liệu.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListStaffDirectory.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListStaffDirectory.</param>
    public async Task<PageResponse<StaffDirectoryResponse>> ListStaffDirectory(Role? role, int? page, int? pageSize)
    {
        await current.Get();
        return PageReader.Map(await pager.Page(page, pageSize, (p, size) => repository.ListStaffAsync(role, true, p, size)), x => new StaffDirectoryResponse(x.Id, x.FirstName, x.LastName, x.Role));
    }

    /// <summary>
    /// Yêu cầu quyền quản lý, tạo nhân viên và lời mời OTP, lưu audit; trả hồ sơ tài khoản mới.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<InvitationVerificationResponse> VerifyInvitation(VerifyRequest r)
    {
        var u=await repository.GetUserByEmailAsync(Normalize(r.Email));
        if(u is not {Active:true,EmailVerified:false} || u.Role==Role.HorseOwner) return new(false);
        var ok=await Consume(u,ChallengePurpose.Invite,r.Code);
        if(!ok){await unitOfWork.SaveChangesAsync();return new(false);}
        u.SecurityStamp=Guid.NewGuid().ToString();
        var proof=protection.CreateProtector("HorseClub.Invitation.PasswordSetup.v1").ToTimeLimitedDataProtector().Protect(u.Id+"|"+u.SecurityStamp,TimeSpan.FromMinutes(settings.VerificationMinutes));
        await unitOfWork.SaveChangesAsync();return new(true,proof);
    }
    public async Task<PasswordChangedResponse> CompleteInvitation(InvitationPasswordRequest r)
    {
        Ensure.That(r.Password==r.ConfirmPassword,Messages.Get(MessageKey.PasswordsDoNotMatch));ValidatePassword(r.Password);
        string proof;try{proof=protection.CreateProtector("HorseClub.Invitation.PasswordSetup.v1").ToTimeLimitedDataProtector().Unprotect(r.SetupToken);}catch(CryptographicException){return new(false);}
        var u=await repository.GetUserByEmailAsync(Normalize(r.Email));
        if(u is not {Active:true,EmailVerified:false} || u.Role==Role.HorseOwner || proof!=u.Id+"|"+u.SecurityStamp)return new(false);
        SetPassword(u,r.Password);await unitOfWork.SaveChangesAsync();return new(true);
    }
    public async Task<OperationResult> ResendInvitation(Guid id)
    {
        Ensure.Role(await current.Get(),Role.ClubManager);var u=Ensure.Found(await repository.FindUserAsync(id));
        Ensure.That(u.Active && !u.EmailVerified && u.Role!=Role.HorseOwner,Messages.Get(MessageKey.UseThisEndpointForInternalStaffRolesOnly));
        u.SecurityStamp=Guid.NewGuid().ToString();await Challenge(u,ChallengePurpose.Invite);
        await events.Audit(AuditAction.StaffInvitationResent,id);await unitOfWork.SaveChangesAsync();return OperationResult.NoContent();
    }
    public async Task<OperationResult> InviteStaff(StaffRequest r)
    {
        Ensure.Role(await current.Get(), Role.ClubManager);
        var u = await CreateStaff(r);
        await events.Audit(AuditAction.StaffCreated, u.Id);
        await unitOfWork.SaveChangesAsync();
        return OperationResult.Created($"/api/staff/{u.Id}", AuthenticationService.View(u));
    }

    /// <summary>
    /// Cho quản lý bật/tắt tài khoản nhân viên, thu hồi phiên bằng stamp và ghi audit; không áp dụng cho quản lý hoặc chủ ngựa.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> SetStaffActive(Guid id, ActiveRequest r)
    {
        Ensure.Role(await current.Get(), Role.ClubManager);
        var u = Ensure.Found(await repository.FindUserAsync(id));
        Ensure.That(u.Role != Role.ClubManager && u.Role != Role.HorseOwner, Messages.Get(MessageKey.OnlyStaffAccountsCanBeChangedHere));
        u.Active = r.Active; u.SecurityStamp = Guid.NewGuid().ToString();
        await events.Audit(AuditAction.StaffActiveChanged, id);
        await unitOfWork.SaveChangesAsync(); return OperationResult.NoContent();
    }

    /// <summary>
    /// Kiểm tra mật khẩu xác nhận và điều kiện tài khoản theo Reset/Invite; chỉ đặt mật khẩu khi OTP đúng mục đích được tiêu thụ thành công.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <param name="purpose">Enum mục đích OTP Verify/Reset/Invite; ngăn dùng mã của luồng khác.</param>
    /// <remarks>Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.</remarks>
    public async Task<OperationResult> ChangePassword(ResetRequest r, ChallengePurpose purpose)
    {
        Ensure.That(r.Password == r.ConfirmPassword, Messages.Get(MessageKey.PasswordsDoNotMatch));
        ValidatePassword(r.Password);
        var u = await repository.GetUserByEmailAsync(AuthenticationService.Normalize(r.Email));
        var eligible = u is { Active: true } && (purpose == ChallengePurpose.Invite
            ? !u.EmailVerified && u.Role != Role.HorseOwner
            : purpose == ChallengePurpose.Reset && u.EmailVerified);
        var ok = eligible && await Consume(u!, purpose, r.Code);
        if (ok) SetPassword(u!, r.Password);
        await unitOfWork.SaveChangesAsync(); return OperationResult.Ok(new PasswordChangedResponse(ok));
    }

}
