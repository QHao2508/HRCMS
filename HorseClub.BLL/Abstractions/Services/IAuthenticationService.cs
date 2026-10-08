using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace HorseClub.BLL.Abstractions.Services;

/// <summary>Application operations implemented by AuthenticationService.</summary>
public interface IAuthenticationService
{
    Task<User?> ValidateSession(Guid id, string? stamp);
    Task<User> Register(RegisterRequest r);
    Task<User> CreateStaff(StaffRequest r);
    Task Challenge(User user, ChallengePurpose purpose);
    Task<bool> Consume(User user, ChallengePurpose purpose, string code);
    Task<LoginAttempt> Login(LoginRequest r);
    void SetPassword(User user, string password);
    void ValidatePassword(string password);
    Task<OperationResult> RegisterAccount(RegisterRequest r);
    Task<OperationResult> VerifyEmail(VerifyRequest r);
    Task<OperationResult> ResendVerification(EmailRequest r);
    Task<OperationResult> ForgotPassword(EmailRequest r);
    Task<UserResponse> GetProfile();
    Task<OperationResult> Logout();
    Task<PageResponse<StaffResponse>> ListStaff(int? page, int? pageSize, Role? role);
    Task<PageResponse<StaffDirectoryResponse>> ListStaffDirectory(Role? role, int? page, int? pageSize);
    Task<OperationResult> InviteStaff(StaffRequest r);
    Task<OperationResult> SetStaffActive(Guid id, ActiveRequest r);
    Task<OperationResult> ChangePassword(ResetRequest r, ChallengePurpose purpose);
}
