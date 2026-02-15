using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sunrise.API.Attributes;
using Sunrise.API.Extensions;
using Sunrise.API.Objects.Keys;
using Sunrise.API.Serializable.Request;
using Sunrise.API.Serializable.Response;
using Sunrise.Shared.Attributes;
using Sunrise.Shared.Database;
using Sunrise.Shared.Database.Models.Clans;
using Sunrise.Shared.Database.Models.Users;
using Sunrise.Shared.Database.Objects;
using Sunrise.Shared.Database.Repositories;
using Sunrise.Shared.Enums.Beatmaps;
using Sunrise.Shared.Enums.Clans;
using Sunrise.Shared.Enums.Users;
using Sunrise.Shared.Repositories;

namespace Sunrise.API.Controllers;

[ApiController]
[ApiHttpTrace]
[Route("/clan")]
[Subdomain("api")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetailsResponseType), StatusCodes.Status400BadRequest)]
public class ClanController(DatabaseService database, SessionRepository sessions) : ControllerBase
{
    [HttpPost]
    [Authorize]
    [EndpointDescription("Create clan")]
    [ProducesResponseType(typeof(ClanDetailsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateClan([FromBody] CreateClanRequest request, CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUserOrThrow();

        if (user.IsRestricted())
            return Problem(ApiErrorResponse.Detail.UserIsRestricted, statusCode: StatusCodes.Status400BadRequest);

        if (user.ClanId.HasValue)
            return Problem(ApiErrorResponse.Detail.UserAlreadyInClan, statusCode: StatusCodes.Status400BadRequest);

        var clanName = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(clanName))
            return Problem("Clan name cannot be empty.", statusCode: StatusCodes.Status400BadRequest);

        var createResult = await database.Clans.CreateClan(clanName, request.AvatarUrl?.Trim(), user, ct);
        if (createResult.IsFailure)
            return Problem(createResult.Error, statusCode: StatusCodes.Status400BadRequest);

        return Ok(await BuildClanDetailsResponse(createResult.Value, user.DefaultGameMode, ct));
    }

    [HttpPost("{id:int}/join")]
    [Authorize]
    [EndpointDescription("Join clan")]
    [ProducesResponseType(typeof(ClanDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponseType), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetailsResponseType), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinClan([Range(1, int.MaxValue)] int id, CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUserOrThrow();

        if (user.IsRestricted())
            return Problem(ApiErrorResponse.Detail.UserIsRestricted, statusCode: StatusCodes.Status403Forbidden);

        if (user.ClanId.HasValue)
            return Problem(ApiErrorResponse.Detail.UserAlreadyInClan, statusCode: StatusCodes.Status400BadRequest);

        var joinResult = await database.Clans.JoinClan(id, user.Id, ct);
        if (joinResult != ClanRepository.JoinClanResult.Success)
        {
            if (joinResult == ClanRepository.JoinClanResult.ClanNotFound)
                return Problem(ApiErrorResponse.Detail.ClanNotFound, statusCode: StatusCodes.Status404NotFound);

            if (joinResult == ClanRepository.JoinClanResult.UserAlreadyInClan)
                return Problem(ApiErrorResponse.Detail.UserAlreadyInClan, statusCode: StatusCodes.Status400BadRequest);

            return Problem(ApiErrorResponse.Detail.UnknownErrorOccurred, statusCode: StatusCodes.Status400BadRequest);
        }

        var clan = await database.Clans.GetClanById(id, new QueryOptions(true), ct);
        if (clan == null)
            return Problem(ApiErrorResponse.Detail.ClanNotFound, statusCode: StatusCodes.Status404NotFound);

        return Ok(await BuildClanDetailsResponse(clan, user.DefaultGameMode, ct));
    }


    [HttpPost("leave")]
    [Authorize]
    [EndpointDescription("Leave clan")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponseType), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetailsResponseType), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LeaveClan(CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUserOrThrow();

        if (user.IsRestricted())
            return Problem(ApiErrorResponse.Detail.UserIsRestricted, statusCode: StatusCodes.Status403Forbidden);

        var leaveResult = await database.Clans.LeaveClan(user.Id, ct);
        if (leaveResult != ClanRepository.LeaveClanResult.Success)
        {
            if (leaveResult == ClanRepository.LeaveClanResult.UserNotInClan)
                return Problem(ApiErrorResponse.Detail.UserNotInClan, statusCode: StatusCodes.Status400BadRequest);

            if (leaveResult == ClanRepository.LeaveClanResult.ClanNotFound)
                return Problem(ApiErrorResponse.Detail.ClanNotFound, statusCode: StatusCodes.Status404NotFound);

            if (leaveResult == ClanRepository.LeaveClanResult.CreatorCannotLeaveClan)
                return Problem(ApiErrorResponse.Detail.ClanCreatorCannotLeaveClan, statusCode: StatusCodes.Status400BadRequest);

            return Problem(ApiErrorResponse.Detail.UnknownErrorOccurred, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok();
    }


    [HttpPatch("name")]
    [Authorize]
    [EndpointDescription("Change clan name")]
    [ProducesResponseType(typeof(ClanDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponseType), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetailsResponseType), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditClanName([FromBody] EditClanNameRequest request, CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUserOrThrow();

        if (user.IsRestricted())
            return Problem(ApiErrorResponse.Detail.UserIsRestricted, statusCode: StatusCodes.Status403Forbidden);

        var clanName = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(clanName))
            return Problem("Clan name cannot be empty.", statusCode: StatusCodes.Status400BadRequest);

        var (result, nextChangeAt) = await database.Clans.UpdateClanName(
            user.Id,
            clanName,
            user.Privilege.HasFlag(UserPrivilege.Supporter),
            ct);

        if (result != ClanRepository.EditClanNameResult.Success)
            return result switch
            {
                ClanRepository.EditClanNameResult.UserNotInClan => Problem(ApiErrorResponse.Detail.UserNotInClan,
                    statusCode: StatusCodes.Status400BadRequest),
                ClanRepository.EditClanNameResult.ClanNotFound => Problem(ApiErrorResponse.Detail.ClanNotFound,
                    statusCode: StatusCodes.Status404NotFound),
                ClanRepository.EditClanNameResult.UserIsNotClanCreator => Problem(ApiErrorResponse.Detail.InsufficientPrivileges,
                    statusCode: StatusCodes.Status403Forbidden),
                ClanRepository.EditClanNameResult.ClanNameAlreadyTaken => Problem(ApiErrorResponse.Detail.ClanNameAlreadyTaken,
                    statusCode: StatusCodes.Status400BadRequest),
                ClanRepository.EditClanNameResult.NameChangeOnCooldown => Problem(ApiErrorResponse.Detail.ClanNameChangeOnCooldown(nextChangeAt ?? DateTime.UtcNow),
                    statusCode: StatusCodes.Status400BadRequest),
                _ => Problem(ApiErrorResponse.Detail.UnknownErrorOccurred, statusCode: StatusCodes.Status400BadRequest)
            };

        return await BuildCurrentUserClanDetailsResponse(user, ct);
    }

    [HttpPatch("avatar")]
    [Authorize]
    [EndpointDescription("Change clan avatar")]
    [ProducesResponseType(typeof(ClanDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponseType), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetailsResponseType), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditClanAvatar([FromBody] EditClanAvatarRequest request, CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUserOrThrow();

        if (user.IsRestricted())
            return Problem(ApiErrorResponse.Detail.UserIsRestricted, statusCode: StatusCodes.Status403Forbidden);

        var result = await database.Clans.UpdateClanAvatar(user.Id, request.AvatarUrl?.Trim(), ct);

        if (result != ClanRepository.EditClanResult.Success)
            return result switch
            {
                ClanRepository.EditClanResult.UserNotInClan => Problem(ApiErrorResponse.Detail.UserNotInClan,
                    statusCode: StatusCodes.Status400BadRequest),
                ClanRepository.EditClanResult.ClanNotFound => Problem(ApiErrorResponse.Detail.ClanNotFound,
                    statusCode: StatusCodes.Status404NotFound),
                ClanRepository.EditClanResult.UserIsNotClanCreator => Problem(ApiErrorResponse.Detail.InsufficientPrivileges,
                    statusCode: StatusCodes.Status403Forbidden),
                _ => Problem(ApiErrorResponse.Detail.UnknownErrorOccurred, statusCode: StatusCodes.Status400BadRequest)
            };

        return await BuildCurrentUserClanDetailsResponse(user, ct);
    }

    [HttpPatch("description")]
    [Authorize]
    [EndpointDescription("Change clan description")]
    [ProducesResponseType(typeof(ClanDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponseType), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetailsResponseType), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditClanDescription([FromBody] EditClanDescriptionRequest request, CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUserOrThrow();

        if (user.IsRestricted())
            return Problem(ApiErrorResponse.Detail.UserIsRestricted, statusCode: StatusCodes.Status403Forbidden);

        var result = await database.Clans.UpdateClanDescription(user.Id, request.Description?.Trim(), ct);

        if (result != ClanRepository.EditClanResult.Success)
            return result switch
            {
                ClanRepository.EditClanResult.UserNotInClan => Problem(ApiErrorResponse.Detail.UserNotInClan,
                    statusCode: StatusCodes.Status400BadRequest),
                ClanRepository.EditClanResult.ClanNotFound => Problem(ApiErrorResponse.Detail.ClanNotFound,
                    statusCode: StatusCodes.Status404NotFound),
                ClanRepository.EditClanResult.UserIsNotClanCreator => Problem(ApiErrorResponse.Detail.InsufficientPrivileges,
                    statusCode: StatusCodes.Status403Forbidden),
                _ => Problem(ApiErrorResponse.Detail.UnknownErrorOccurred, statusCode: StatusCodes.Status400BadRequest)
            };

        return await BuildCurrentUserClanDetailsResponse(user, ct);
    }

    [HttpGet("{id:int}")]
    [EndpointDescription("Get clan details")]
    [ProducesResponseType(typeof(ClanDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponseType), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClan(
        [Range(1, int.MaxValue)] int id,
        [FromQuery(Name = "mode")] GameMode mode = GameMode.Standard,
        CancellationToken ct = default)
    {
        var clan = await database.Clans.GetClanById(id, new QueryOptions(true), ct);
        if (clan == null)
            return Problem(ApiErrorResponse.Detail.ClanNotFound, statusCode: StatusCodes.Status404NotFound);

        return Ok(await BuildClanDetailsResponse(clan, mode, ct));
    }

    [HttpGet("leaderboard")]
    [EndpointDescription("Get clans leaderboard sorted by total clan pp")]
    [ProducesResponseType(typeof(ClansLeaderboardResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClanLeaderboard(
        [FromQuery(Name = "mode")] GameMode mode = GameMode.Standard,
        [Range(1, 100)] [FromQuery(Name = "limit")] int limit = 50,
        [Range(1, int.MaxValue)] [FromQuery(Name = "page")] int page = 1,
        CancellationToken ct = default)
    {
        var clans = await database.Clans.GetClansByLeaderboard(mode, new QueryOptions(true, new Pagination(page, limit)), ct);
        var totalCount = await database.Clans.CountClans(ct);

        var clanResponses = new List<ClanResponse>(clans.Count);
        foreach (var clan in clans)
        {
            clanResponses.Add(new ClanResponse(clan, await database.Clans.GetClanTotalPp(clan.Id, mode, ct)));
        }

        return Ok(new ClansLeaderboardResponse(clanResponses, totalCount));
    }

    private async Task<IActionResult> BuildCurrentUserClanDetailsResponse(User user, CancellationToken ct)
    {
        if (!user.ClanId.HasValue)
            return Problem(ApiErrorResponse.Detail.UserNotInClan, statusCode: StatusCodes.Status400BadRequest);

        var clan = await database.Clans.GetClanById(user.ClanId.Value, new QueryOptions(true), ct);
        if (clan == null)
            return Problem(ApiErrorResponse.Detail.ClanNotFound, statusCode: StatusCodes.Status404NotFound);

        return Ok(await BuildClanDetailsResponse(clan, user.DefaultGameMode, ct));
    }

    private async Task<ClanDetailsResponse> BuildClanDetailsResponse(Clan clan, GameMode mode, CancellationToken ct)
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
