using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Sunrise.Shared.Database.Models.Users;
using Sunrise.Shared.Enums.Clans;

namespace Sunrise.Shared.Database.Models.Clans;

[Table("clan_member")]
[Index(nameof(ClanId), nameof(UserId), IsUnique = true)]
[Index(nameof(UserId), IsUnique = true)]
public class ClanMember
{
    public int Id { get; set; }

    [ForeignKey(nameof(ClanId))]
    public Clan Clan { get; set; }
    public int ClanId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; }
    public int UserId { get; set; }

    public ClanRole Role { get; set; } = ClanRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
