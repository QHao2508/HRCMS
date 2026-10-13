namespace HorseClub.BLL.Contracts;

/// <summary>Bác sĩ thú y cập nhật trạng thái phác đồ điều trị (hoàn thành / đang điều trị).</summary>
public sealed record TreatmentStatusRequest(bool Completed);
