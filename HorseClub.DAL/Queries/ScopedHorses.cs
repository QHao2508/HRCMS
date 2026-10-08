using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;

namespace HorseClub.DAL.Queries;

internal static class ScopedHorses
{
    public static IQueryable<Horse> For(ClubDbContext db, HorseScope scope)
    {
        var query = db.Horses.Where(x => !x.Archived);
        if (scope.OwnerId.HasValue) query = query.Where(x => x.OwnerId == scope.OwnerId);
        if (scope.StaffId.HasValue) query = query.Where(x => db.Assignments.Any(a => a.HorseId == x.Id && a.StaffId == scope.StaffId && a.Active));
        if (scope.RiderId.HasValue) query = query.Where(x => db.Sessions.Any(s => s.HorseId == x.Id && s.RiderId == scope.RiderId));
        return query;
    }
}
