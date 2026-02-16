using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sunrise.API.Objects.Keys;
using Sunrise.API.Serializable.Response;
using Sunrise.Shared.Database.Models.Clans;
using Sunrise.Shared.Database.Models.Users;
using Sunrise.Shared.Enums.Clans;
using Sunrise.Tests;
using Sunrise.Tests.Abstracts;
using Sunrise.Tests.Extensions;

namespace Sunrise.Server.Tests.API.ClanController;

[Collection("Integration tests collection")]
public class ApiKickClanMemberTests(IntegrationDatabaseFixture fixture) : ApiTest(fixture)
{
    [Fact]
    public async Task TestClanCreatorCanKickMember()
    {
        var client = App.CreateClient().UseClient("api");

        var creator = await CreateTestUser();
        var clan = await CreateClan(creator);

        var member = await CreateTestUser();
        await JoinClan(clan.Id, member);

        client.UseUserAuthToken(await GetUserAuthTokens(creator));

        var response = await client.PostAsync($"clan/kick/{member.Id}", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var details = await response.Content.ReadFromJsonAsyncWithAppConfig<ClanDetailsResponse>();
        Assert.NotNull(details);
        Assert.DoesNotContain(details.Members, m => m.User.Id == member.Id);

        var updatedMember = await Database.Users.GetUser(id: member.Id);
        Assert.NotNull(updatedMember);
        Assert.Null(updatedMember.ClanId);

        var membership = await Database.DbContext.ClanMembers
            .FirstOrDefaultAsync(cm => cm.ClanId == clan.Id && cm.UserId == member.Id);

        Assert.Null(membership);
    }

    [Fact]
    public async Task TestNonCreatorCannotKickMember()
    {
        var client = App.CreateClient().UseClient("api");

        var creator = await CreateTestUser();
        var clan = await CreateClan(creator);

        var member = await CreateTestUser();
        await JoinClan(clan.Id, member);

        var anotherMember = await CreateTestUser();
        await JoinClan(clan.Id, anotherMember);

        client.UseUserAuthToken(await GetUserAuthTokens(member));

        var response = await client.PostAsync($"clan/kick/{anotherMember.Id}", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var responseContent = await response.Content.ReadFromJsonAsyncWithAppConfig<ProblemDetails>();
        Assert.Contains(ApiErrorResponse.Detail.InsufficientPrivileges, responseContent?.Detail);
    }

    [Fact]
    public async Task TestClanCreatorCannotKickYourself()
    {
        var client = App.CreateClient().UseClient("api");

        var creator = await CreateTestUser();
        var clan = await CreateClan(creator);

        client.UseUserAuthToken(await GetUserAuthTokens(creator));

        var response = await client.PostAsync($"clan/kick/{creator.Id}", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseContent = await response.Content.ReadFromJsonAsyncWithAppConfig<ProblemDetails>();
        Assert.Contains(ApiErrorResponse.Detail.CannotKickYourselfFromClan, responseContent?.Detail);

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
