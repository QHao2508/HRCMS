namespace HorseClub.BLL.Contracts;

public sealed record PreventiveCompletionRequest(DateOnly CompletedDate, string Notes);
