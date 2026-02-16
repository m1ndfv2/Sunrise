using Sunrise.API.Serializable.Response;
using Sunrise.Shared.Database;
using Sunrise.Shared.Database.Models.Clans;
using Sunrise.Shared.Enums.Beatmaps;
using Sunrise.Shared.Enums.Clans;
using Sunrise.Shared.Repositories;

namespace Sunrise.API.Services;

public static class ClanResponseBuilder
{
    public static async Task<ClanDetailsResponse> BuildClanDetailsResponse(
        DatabaseService database,
        SessionRepository sessions,
        Clan clan,
        GameMode mode,
        CancellationToken ct)
    {
        var members = await database.Clans.GetClanMembersByPp(clan.Id, mode, ct);
        var totalPp = await database.Clans.GetClanTotalPp(clan.Id, mode, ct);

        return new ClanDetailsResponse(
            new ClanResponse(clan, totalPp),
            members.Select(cm => new ClanMemberResponse(
                new UserResponse(sessions, cm.User),
                cm.Role == ClanRole.Creator ? "creator" : "member",
                cm.User.UserStats.FirstOrDefault(us => us.GameMode == mode)?.PerformancePoints ?? 0)).ToList());
    }
}
