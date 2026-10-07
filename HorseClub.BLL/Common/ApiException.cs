namespace HorseClub.BLL.Common;

public sealed class ApiException(int status, string code, string message, Guid? referenceId = null) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
    public Guid? ReferenceId { get; } = referenceId;
}
