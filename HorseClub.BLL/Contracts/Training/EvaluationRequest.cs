using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Contracts;

public sealed record EvaluationRequest([property: Required] string Comment, bool AdjustFutureSessions);
