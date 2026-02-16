using System.Text.Json.Serialization;
using Sunrise.Shared.Database.Models.Clans;

namespace Sunrise.API.Serializable.Response;

public class ClanResponse
{
    public ClanResponse(Clan clan, double totalPp)
    {
        Id = clan.Id;
        Name = clan.Name;
        AvatarUrl = clan.AvatarUrl;
        Description = clan.Description;
        TotalPp = totalPp;
        CreatedAt = clan.CreatedAt;
    }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("total_pp")]
    public double TotalPp { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class ClanMemberResponse
{
    public ClanMemberResponse(UserResponse user, string role, double pp)
    {
        User = user;
        Role = role;
        Pp = pp;
    }

    [JsonPropertyName("user")]
    public UserResponse User { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("pp")]
    public double Pp { get; set; }
}

public class ClanDetailsResponse
{
    public ClanDetailsResponse(ClanResponse clan, List<ClanMemberResponse> members)
    {
        Clan = clan;
        Members = members;
    }

    [JsonPropertyName("clan")]
    public ClanResponse Clan { get; set; }

    [JsonPropertyName("members")]
    public List<ClanMemberResponse> Members { get; set; }
}

public class ClansLeaderboardResponse
{
    public ClansLeaderboardResponse(List<ClanResponse> clans, int totalCount)
    {
        Clans = clans;
        TotalCount = totalCount;
    }

    [JsonPropertyName("clans")]
    public List<ClanResponse> Clans { get; set; }

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
}
