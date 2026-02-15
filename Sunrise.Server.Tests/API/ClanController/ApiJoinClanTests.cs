using System.Net;
using Microsoft.EntityFrameworkCore;
using Sunrise.API.Serializable.Response;
using Sunrise.Shared.Database.Models.Clans;
using Sunrise.Shared.Database.Models.Users;
using Sunrise.Tests;
using Sunrise.Tests.Abstracts;
using Sunrise.Tests.Extensions;
using Sunrise.Tests.Utils;

namespace Sunrise.Server.Tests.API.ClanController;

[Collection("Integration tests collection")]
public class ApiJoinClanTests(IntegrationDatabaseFixture fixture) : ApiTest(fixture)
{
    [Fact]
    public async Task TestJoinClanSuccessfully()
    {
        var client = App.CreateClient().UseClient("api");

        var creator = await CreateTestUser();
        var clan = await CreateClan(creator);

        var member = await CreateTestUser();
        client.UseUserAuthToken(await GetUserAuthTokens(member));

        var response = await client.PostAsync($"clan/{clan.Id}/join", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var details = await response.Content.ReadFromJsonAsyncWithAppConfig<ClanDetailsResponse>();
        Assert.NotNull(details);
        Assert.Equal(clan.Id, details.Clan.Id);
        Assert.Contains(details.Members, m => m.User.Id == member.Id && m.Role == "member");

        var joinedUser = await Database.Users.GetUser(id: member.Id);
        Assert.NotNull(joinedUser);
        Assert.Equal(clan.Id, joinedUser.ClanId);

        var clanMembership = await Database.DbContext.ClanMembers
            .FirstOrDefaultAsync(cm => cm.ClanId == clan.Id && cm.UserId == member.Id);

        Assert.NotNull(clanMembership);
        Assert.Equal(Sunrise.Shared.Enums.Clans.ClanRole.Member, clanMembership.Role);
    }

    [Fact]
    public async Task TestJoinClanWhenAlreadyInAnotherClan()
    {
        var client = App.CreateClient().UseClient("api");

        var creator1 = await CreateTestUser();
        var firstClan = await CreateClan(creator1);

        var creator2 = await CreateTestUser();
        var secondClan = await CreateClan(creator2);

        var member = await CreateTestUser();
        var joinFirstResponse = await JoinClan(firstClan.Id, member, client);
        Assert.Equal(HttpStatusCode.OK, joinFirstResponse.StatusCode);

        var joinSecondResponse = await client.PostAsync($"clan/{secondClan.Id}/join", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, joinSecondResponse.StatusCode);
    }

    [Fact]
    public async Task TestJoinNonExistingClan()
    {
        var client = App.CreateClient().UseClient("api");

        client.UseUserAuthToken(await GetUserAuthTokens());

        var response = await client.PostAsync("clan/999999/join", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TestRestrictedUserCannotJoinClan()
    {
        var client = App.CreateClient().UseClient("api");

        var creator = await CreateTestUser();
        var clan = await CreateClan(creator);

        var restrictedUser = await CreateTestUser();
        client.UseUserAuthToken(await GetUserAuthTokens(restrictedUser));

        await Database.Users.Moderation.RestrictPlayer(restrictedUser.Id, null, "restricted for testing");

        var response = await client.PostAsync($"clan/{clan.Id}/join", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TestDuplicateJoinReturnsBadRequestAndNoDuplicateMembership()
    {
        var client = App.CreateClient().UseClient("api");

        var creator = await CreateTestUser();
        var clan = await CreateClan(creator);

        var member = await CreateTestUser();
        var firstJoinResponse = await JoinClan(clan.Id, member, client);
        Assert.Equal(HttpStatusCode.OK, firstJoinResponse.StatusCode);

        var secondJoinResponse = await client.PostAsync($"clan/{clan.Id}/join", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, secondJoinResponse.StatusCode);

        var membershipsCount = await Database.DbContext.ClanMembers
            .CountAsync(cm => cm.ClanId == clan.Id && cm.UserId == member.Id);

        Assert.Equal(1, membershipsCount);
    }

    private async Task<Clan> CreateClan(User creator)
    {
        var createResult = await Database.Clans.CreateClan($"clan_{Guid.NewGuid():N}", null, creator);
        Assert.True(createResult.IsSuccess, createResult.Error);

        return createResult.Value;
    }

    private async Task<HttpResponseMessage> JoinClan(int clanId, User user, HttpClient client)
    {
        client.UseUserAuthToken(await GetUserAuthTokens(user));
        return await client.PostAsync($"clan/{clanId}/join", new StringContent(string.Empty));
    }
}
