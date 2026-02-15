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
using Sunrise.Shared.Database.Objects;
using Sunrise.Shared.Enums.Beatmaps;
using Sunrise.Shared.Enums.Clans;
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
            return Problem("User is already in a clan.", statusCode: StatusCodes.Status400BadRequest);

        var clanName = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(clanName))
            return Problem("Clan name cannot be empty.", statusCode: StatusCodes.Status400BadRequest);

        var createResult = await database.Clans.CreateClan(clanName, request.AvatarUrl?.Trim(), user, ct);
        if (createResult.IsFailure)
            return Problem(createResult.Error, statusCode: StatusCodes.Status400BadRequest);

        var members = await database.Clans.GetClanMembersByPp(createResult.Value.Id, user.DefaultGameMode, ct);
        var totalPp = await database.Clans.GetClanTotalPp(createResult.Value.Id, user.DefaultGameMode, ct);

        return Ok(new ClanDetailsResponse(
            new ClanResponse(createResult.Value, totalPp),
            members.Select(cm => new ClanMemberResponse(
                new UserResponse(sessions, cm.User),
                cm.Role == ClanRole.Creator ? "creator" : "member",
                cm.User.UserStats.FirstOrDefault(us => us.GameMode == user.DefaultGameMode)?.PerformancePoints ?? 0)).ToList()));
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
            return Problem("Clan not found.", statusCode: StatusCodes.Status404NotFound);

        var members = await database.Clans.GetClanMembersByPp(clan.Id, mode, ct);
        var totalPp = await database.Clans.GetClanTotalPp(clan.Id, mode, ct);

        return Ok(new ClanDetailsResponse(
            new ClanResponse(clan, totalPp),
            members.Select(cm => new ClanMemberResponse(
                new UserResponse(sessions, cm.User),
                cm.Role == ClanRole.Creator ? "creator" : "member",
                cm.User.UserStats.FirstOrDefault(us => us.GameMode == mode)?.PerformancePoints ?? 0)).ToList()));
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
}
