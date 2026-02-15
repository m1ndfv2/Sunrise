using System.Net;
using Microsoft.EntityFrameworkCore;
using Sunrise.Shared.Database.Models.Clans;
using Sunrise.Shared.Database.Models.Users;
using Sunrise.Shared.Enums.Clans;
using Sunrise.Tests;
using Sunrise.Tests.Abstracts;
using Sunrise.Tests.Extensions;

namespace Sunrise.Server.Tests.API.ClanController;

[Collection("Integration tests collection")]
public class ApiLeaveClanTests(IntegrationDatabaseFixture fixture) : ApiTest(fixture)
{
    [Fact]
    public async Task TestLeaveClanSuccessfully()
    {
        var client = App.CreateClient().UseClient("api");

        var creator = await CreateTestUser();
        var clan = await CreateClan(creator);

        var member = await CreateTestUser();
        await JoinClan(clan.Id, member);

        client.UseUserAuthToken(await GetUserAuthTokens(member));

        var response = await client.PostAsync("clan/leave", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedUser = await Database.Users.GetUser(id: member.Id);
        Assert.NotNull(updatedUser);
        Assert.Null(updatedUser.ClanId);

        var membership = await Database.DbContext.ClanMembers
            .FirstOrDefaultAsync(cm => cm.ClanId == clan.Id && cm.UserId == member.Id);

        Assert.Null(membership);
    }

    [Fact]
    public async Task TestLeaveClanWhenUserNotInClan()
    {
        var client = App.CreateClient().UseClient("api");

        var user = await CreateTestUser();
        client.UseUserAuthToken(await GetUserAuthTokens(user));

        var response = await client.PostAsync("clan/leave", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TestRestrictedUserCannotLeaveClan()
    {
        var client = App.CreateClient().UseClient("api");

        var creator = await CreateTestUser();
        var clan = await CreateClan(creator);

        var member = await CreateTestUser();
        await JoinClan(clan.Id, member);

        await Database.Users.Moderation.RestrictPlayer(member.Id, null, "restricted for leave test");

        client.UseUserAuthToken(await GetUserAuthTokens(member));

        var response = await client.PostAsync("clan/leave", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TestCreatorCannotLeaveClan()
    {
        var client = App.CreateClient().UseClient("api");

        var creator = await CreateTestUser();
        var clan = await CreateClan(creator);

        client.UseUserAuthToken(await GetUserAuthTokens(creator));

        var response = await client.PostAsync("clan/leave", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var updatedUser = await Database.Users.GetUser(id: creator.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(clan.Id, updatedUser.ClanId);

        var creatorMembership = await Database.DbContext.ClanMembers
            .FirstOrDefaultAsync(cm => cm.ClanId == clan.Id && cm.UserId == creator.Id);

        Assert.NotNull(creatorMembership);
        Assert.Equal(ClanRole.Creator, creatorMembership.Role);
    }

    private async Task<Clan> CreateClan(User creator)
    {
        var createResult = await Database.Clans.CreateClan($"clan_{Guid.NewGuid():N}", null, creator);
        Assert.True(createResult.IsSuccess, createResult.Error);

        return createResult.Value;
    }

    private async Task JoinClan(int clanId, User user)
    {
        var joinResult = await Database.Clans.JoinClan(clanId, user.Id);
        Assert.Equal(Sunrise.Shared.Database.Repositories.ClanRepository.JoinClanResult.Success, joinResult);
    }
}
