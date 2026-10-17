using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Contracts;

public sealed record InventoryRequest([property: Required] string Name, [property: Required] string Category,
    [property: Required] string Unit, [property: Range(0, 1000000)] decimal MinimumStock);
