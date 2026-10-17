using HorseClub.DAL.Entities;

namespace HorseClub.BLL.Contracts;

public sealed record RegistrationReviewResponse(
    HorseRegistration Registration,
    Guid? HorseId);
