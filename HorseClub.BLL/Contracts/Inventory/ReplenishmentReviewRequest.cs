using System.Text.Json.Serialization;

namespace HorseClub.BLL.Contracts;

public sealed record ReplenishmentReviewRequest([property: JsonRequired] bool Approve, string Notes);
