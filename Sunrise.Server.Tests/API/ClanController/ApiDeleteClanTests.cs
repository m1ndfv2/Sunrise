using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sunrise.API.Objects.Keys;
using Sunrise.Shared.Database.Models.Clans;
using Sunrise.Shared.Database.Models.Users;
using Sunrise.Tests;
using Sunrise.Tests.Abstracts;
using Sunrise.Tests.Extensions;

namespace Sunrise.Server.Tests.API.ClanController;

[Collection("Integration tests collection")]
public class ApiDeleteClanTests(IntegrationDatabaseFixture fixture) : ApiTest(fixture)
{
    [Fact]
    public async Task TestClanCreatorCanDeleteOwnClan()
    {
        var client = App.CreateClient().UseClient("api");

        var creator = await CreateTestUser();
        var clan = await CreateClan(creator);

        var member = await CreateTestUser();
        await JoinClan(clan.Id, member);

        client.UseUserAuthToken(await GetUserAuthTokens(creator));

        var response = await client.DeleteAsync("clan");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var deletedClan = await Database.Clans.GetClanById(clan.Id);
        Assert.Null(deletedClan);

        var updatedCreator = await Database.Users.GetUser(id: creator.Id);
        var updatedMember = await Database.Users.GetUser(id: member.Id);

        Assert.NotNull(updatedCreator);
        Assert.NotNull(updatedMember);
        Assert.Null(updatedCreator.ClanId);
        Assert.Null(updatedMember.ClanId);

        var membershipsCount = await Database.DbContext.ClanMembers.CountAsync(cm => cm.ClanId == clan.Id);
        Assert.Equal(0, membershipsCount);
    }

    [Fact]
    public async Task TestNonCreatorCannotDeleteClan()
    {
        var client = App.CreateClient().UseClient("api");

        var creator = await CreateTestUser();
        var clan = await CreateClan(creator);

        var member = await CreateTestUser();
        await JoinClan(clan.Id, member);

        client.UseUserAuthToken(await GetUserAuthTokens(member));

        var response = await client.DeleteAsync("clan");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var responseContent = await response.Content.ReadFromJsonAsyncWithAppConfig<ProblemDetails>();
        Assert.Contains(ApiErrorResponse.Detail.InsufficientPrivileges, responseContent?.Detail);
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
