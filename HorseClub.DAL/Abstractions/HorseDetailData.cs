namespace HorseClub.DAL.Abstractions;

/// <summary>Dữ liệu trang chi tiết ngựa đọc trong một lượt truy vấn SQL (giảm số lần gọi Azure SQL).</summary>
public sealed record HorseDetailData(HorseRegistration Registration, Attachment? Photo, Measurement? LatestMeasurement, List<StaffAssignment> Assignments, StallOccupancy? Occupancy, Dictionary<Guid, string> StaffNames);
