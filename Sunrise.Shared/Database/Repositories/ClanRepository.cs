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
    public enum JoinClanResult
    {
        Success,
        ClanNotFound,
        UserNotFound,
        UserAlreadyInClan
    }

    public enum LeaveClanResult
    {
        Success,
        UserNotFound,
        UserNotInClan,
        ClanNotFound,
        CreatorCannotLeaveClan
    }

    public enum EditClanResult
    {
        Success,
        UserNotFound,
        UserNotInClan,
        ClanNotFound,
        UserIsNotClanCreator
    }

    public enum EditClanNameResult
    {
        Success,
        UserNotFound,
        UserNotInClan,
        ClanNotFound,
        UserIsNotClanCreator,
        ClanNameAlreadyTaken,
        NameChangeOnCooldown
    }

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

    public async Task<JoinClanResult> JoinClan(int clanId, int userId, CancellationToken ct = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            var clanExists = await dbContext.Clans.AnyAsync(c => c.Id == clanId, ct);
            if (!clanExists)
                return JoinClanResult.ClanNotFound;

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null)
                return JoinClanResult.UserNotFound;

            if (user.ClanId.HasValue)
                return JoinClanResult.UserAlreadyInClan;

            user.ClanId = clanId;
            dbContext.ClanMembers.Add(new ClanMember
            {
                ClanId = clanId,
                UserId = userId,
                Role = ClanRole.Member
            });

            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return JoinClanResult.Success;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("Duplicate entry") == true)
        {
            await transaction.RollbackAsync(ct);
            return JoinClanResult.UserAlreadyInClan;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }


    public async Task<LeaveClanResult> LeaveClan(int userId, CancellationToken ct = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null)
                return LeaveClanResult.UserNotFound;

            if (!user.ClanId.HasValue)
                return LeaveClanResult.UserNotInClan;

            var clanId = user.ClanId.Value;

            var clanExists = await dbContext.Clans.AnyAsync(c => c.Id == clanId, ct);
            if (!clanExists)
                return LeaveClanResult.ClanNotFound;

            var membership = await dbContext.ClanMembers.FirstOrDefaultAsync(cm => cm.ClanId == clanId && cm.UserId == userId, ct);
            if (membership?.Role == ClanRole.Creator)
                return LeaveClanResult.CreatorCannotLeaveClan;

            if (membership != null)
            {
                dbContext.ClanMembers.Remove(membership);
            }

            user.ClanId = null;

            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return LeaveClanResult.Success;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }


    public async Task<EditClanResult> UpdateClanAvatar(int userId, string? avatarUrl, CancellationToken ct = default)
    {
        var state = await GetClanEditState(userId, ct);
        if (state.Result != EditClanResult.Success)
            return state.Result;

        state.Clan!.AvatarUrl = avatarUrl;

        await dbContext.SaveChangesAsync(ct);

        return EditClanResult.Success;
    }

    public async Task<EditClanResult> UpdateClanDescription(int userId, string? description, CancellationToken ct = default)
    {
        var state = await GetClanEditState(userId, ct);
        if (state.Result != EditClanResult.Success)
            return state.Result;

        state.Clan!.Description = description;

        await dbContext.SaveChangesAsync(ct);

        return EditClanResult.Success;
    }

    public async Task<(EditClanNameResult Result, DateTime? NextChangeAt)> UpdateClanName(int userId, string name, bool hasSupporterPrivilege,
        CancellationToken ct = default)
    {
        var state = await GetClanEditState(userId, ct);
        if (state.Result != EditClanResult.Success)
            return state.Result switch
            {
                EditClanResult.UserNotFound => (EditClanNameResult.UserNotFound, null),
                EditClanResult.UserNotInClan => (EditClanNameResult.UserNotInClan, null),
                EditClanResult.ClanNotFound => (EditClanNameResult.ClanNotFound, null),
                EditClanResult.UserIsNotClanCreator => (EditClanNameResult.UserIsNotClanCreator, null),
                _ => (EditClanNameResult.ClanNotFound, null)
            };

        var clan = state.Clan!;
        var normalizedName = name.Trim();

        if (!string.Equals(clan.Name, normalizedName, StringComparison.Ordinal))
        {
            var isNameTaken = await dbContext.Clans.AnyAsync(c => c.Name == normalizedName && c.Id != clan.Id, ct);
            if (isNameTaken)
                return (EditClanNameResult.ClanNameAlreadyTaken, null);

            var cooldown = hasSupporterPrivilege ? TimeSpan.FromDays(30) : TimeSpan.FromDays(36500);
            var lastNameChange = clan.NameChangedAt ?? clan.CreatedAt;

            if (lastNameChange.Add(cooldown) > DateTime.UtcNow)
                return (EditClanNameResult.NameChangeOnCooldown, lastNameChange.Add(cooldown));

            clan.Name = normalizedName;
            clan.NameChangedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(ct);
        }

        return (EditClanNameResult.Success, null);
    }

    private async Task<(EditClanResult Result, Clan? Clan)> GetClanEditState(int userId, CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return (EditClanResult.UserNotFound, null);

        if (!user.ClanId.HasValue)
            return (EditClanResult.UserNotInClan, null);

        var clan = await dbContext.Clans.FirstOrDefaultAsync(c => c.Id == user.ClanId, ct);
        if (clan == null)
            return (EditClanResult.ClanNotFound, null);

        var isCreator = await dbContext.ClanMembers.AnyAsync(cm => cm.ClanId == clan.Id && cm.UserId == userId && cm.Role == ClanRole.Creator, ct);
        if (!isCreator)
            return (EditClanResult.UserIsNotClanCreator, null);

        return (EditClanResult.Success, clan);
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
