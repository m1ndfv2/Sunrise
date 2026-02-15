using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Sunrise.Shared.Database.Models.Users;

namespace Sunrise.Shared.Database.Models.Clans;

[Table("clan")]
[Index(nameof(Name), IsUnique = true)]
public class Clan
{
    public int Id { get; set; }

    [MaxLength(32)]
    public string Name { get; set; }

    [MaxLength(2048)]
    public string? AvatarUrl { get; set; }

    [MaxLength(2048)]
    public string? Description { get; set; }

    public DateTime? NameChangedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<ClanMember> Members { get; set; } = new List<ClanMember>();
}
