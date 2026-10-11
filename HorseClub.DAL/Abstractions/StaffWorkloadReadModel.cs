namespace HorseClub.DAL.Abstractions;
public sealed record StaffWorkItem(string Kind, Guid HorseId, Guid ReferenceId, string HorseName, string State);
public sealed record StaffWorkloadReadModel(int Assignments, int Sessions, int CareTasks, List<StaffWorkItem> Preview) {
    public bool HasOpenWork => Assignments + Sessions + CareTasks > 0;
}
