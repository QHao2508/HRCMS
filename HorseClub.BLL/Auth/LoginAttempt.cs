using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;

namespace HorseClub.BLL.Auth;

public sealed record LoginAttempt(LoginStatus Status, User? User = null);
