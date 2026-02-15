using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Sunrise.Shared.Database.Extensions;
using Sunrise.Shared.Database.Models.Clans;
using Sunrise.Shared.Database.Models.Users;
using Sunrise.Shared.Database.Objects;
using Sunrise.Shared.Enums.Beatmaps;
using Sunrise.Shared.Enums.Clans;
using Sunrise.Shared.Utils;

namespace Sunrise.Shared.Database.Repositories;

public class ClanRepository(Lazy<DatabaseService> databaseService, SunriseDbContext dbContext)
{
    public async Task<Result<Clan>> CreateClan(string name, string? avatarUrl, User creator, CancellationToken ct = default)
    {
        if (creator.ClanId.HasValue)
            return Result.Failure<Clan>("User is already in a clan.");

        var isNameTaken = await dbContext.Clans.AnyAsync(c => c.Name == name, cancellationToken: ct);
        if (isNameTaken)
            return Result.Failure<Clan>("Clan name is already taken.");

        Clan clan = null!;

        var createClanResult = await databaseService.Value.CommitAsTransactionAsync(async () =>
        {
            clan = new Clan
            {
                Name = name,
                AvatarUrl = avatarUrl
            };

            dbContext.Clans.Add(clan);
            await dbContext.SaveChangesAsync(ct);

            creator.ClanId = clan.Id;
            dbContext.UpdateEntity(creator);

            dbContext.ClanMembers.Add(new ClanMember
            {
                ClanId = clan.Id,
                UserId = creator.Id,
                Role = ClanRole.Creator
            });
        }, ct);

        return createClanResult.IsFailure
            ? Result.Failure<Clan>(createClanResult.Error)
            : Result.Success(clan);
    }

    public async Task<Clan?> GetClanById(int id, QueryOptions? options = null, CancellationToken ct = default)
    {
        return await dbContext.Clans
            .Where(c => c.Id == id)
            .UseQueryOptions(options)
            .FirstOrDefaultAsync(cancellationToken: ct);
    }

    public async Task<List<Clan>> GetClansByLeaderboard(GameMode mode, QueryOptions? options = null, CancellationToken ct = default)
    {
        var clans = await dbContext.Clans
            .OrderByDescending(c => c.Members
                 .Where(m => m.User.AccountStatus == Sunrise.Shared.Enums.Users.UserAccountStatus.Active)
                .Sum(m => m.User.UserStats
                    .Where(s => s.GameMode == mode)
                    .Select(s => s.PerformancePoints)
                    .FirstOrDefault()))
            .ThenBy(c => c.Id)
            .UseQueryOptions(options)
            .ToListAsync(cancellationToken: ct);

        return clans;
    }

    public async Task<List<ClanMember>> GetClanMembersByPp(int clanId, GameMode mode, CancellationToken ct = default)
    {
        return await dbContext.ClanMembers
            .Where(cm => cm.ClanId == clanId)
            .Include(cm => cm.User)
            .ThenInclude(u => u.UserFiles)
            .Include(cm => cm.User)
            .ThenInclude(u => u.UserStats.Where(us => us.GameMode == mode))
            .OrderByDescending(cm => cm.User.UserStats.Where(us => us.GameMode == mode).Select(us => us.PerformancePoints).FirstOrDefault())
            .ThenBy(cm => cm.UserId)
            .ToListAsync(cancellationToken: ct);
    }

    public async Task<double> GetClanTotalPp(int clanId, GameMode mode, CancellationToken ct = default)
    {
        return await dbContext.ClanMembers
             .Where(cm => cm.ClanId == clanId && cm.User.AccountStatus == Sunrise.Shared.Enums.Users.UserAccountStatus.Active)
            .SumAsync(cm => cm.User.UserStats
                .Where(us => us.GameMode == mode)
                .Select(us => us.PerformancePoints)
                .FirstOrDefault(), ct);
    }

    public async Task<int> CountClans(CancellationToken ct = default)
    {
        return await dbContext.Clans.CountAsync(cancellationToken: ct);
    }
}
