using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sunrise.API.Objects.Keys;
using Sunrise.API.Serializable.Response;
using Sunrise.Tests;
using Sunrise.Tests.Abstracts;
using Sunrise.Tests.Extensions;

namespace Sunrise.Server.Tests.API.UserController;

[Collection("Integration tests collection")]
public class ApiUserGetUserClanTests(IntegrationDatabaseFixture fixture) : ApiTest(fixture)
{
    [Fact]
    public async Task TestGetUserClanUserNotFound()
    {
        // Arrange
        var client = App.CreateClient().UseClient("api");

        // Act
        var response = await client.GetAsync("user/999999/clan");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var responseContent = await response.Content.ReadFromJsonAsyncWithAppConfig<ProblemDetails>();
        Assert.Contains(ApiErrorResponse.Detail.UserNotFound, responseContent?.Detail);
    }

    [Fact]
    public async Task TestGetUserClanWhenUserNotInClan()
    {
        // Arrange
        var client = App.CreateClient().UseClient("api");
        var user = await CreateTestUser();

        // Act
        var response = await client.GetAsync($"user/{user.Id}/clan");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var responseContent = await response.Content.ReadFromJsonAsyncWithAppConfig<ProblemDetails>();
        Assert.Contains(ApiErrorResponse.Detail.ClanNotFound, responseContent?.Detail);
    }

    [Fact]
    public async Task TestGetUserClanWhenClanIsMissing()
    {
        // Arrange
        var client = App.CreateClient().UseClient("api");
        var user = await CreateTestUser();

        user.ClanId = 999999;
        Database.DbContext.Users.Update(user);
        await Database.DbContext.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"user/{user.Id}/clan");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var responseContent = await response.Content.ReadFromJsonAsyncWithAppConfig<ProblemDetails>();
        Assert.Contains(ApiErrorResponse.Detail.ClanNotFound, responseContent?.Detail);
    }

    [Fact]
    public async Task TestGetUserClan()
    {
        // Arrange
        var client = App.CreateClient().UseClient("api");

        var creator = await CreateTestUser();
        var createResult = await Database.Clans.CreateClan($"clan_{Guid.NewGuid():N}", "https://cdn.sunrise.test/clan.png", creator);
        Assert.True(createResult.IsSuccess, createResult.Error);

        var clan = createResult.Value;

        var member = await CreateTestUser();
        var joinResult = await Database.Clans.JoinClan(clan.Id, member.Id);
        Assert.Equal(Sunrise.Shared.Database.Repositories.ClanRepository.JoinClanResult.Success, joinResult);

        // Act
        var response = await client.GetAsync($"user/{creator.Id}/clan");

        // Assert
        response.EnsureSuccessStatusCode();

        var details = await response.Content.ReadFromJsonAsyncWithAppConfig<ClanDetailsResponse>();
        Assert.NotNull(details);

        Assert.Equal(clan.Id, details.Clan.Id);
        Assert.Equal(clan.Name, details.Clan.Name);
        Assert.Equal(clan.AvatarUrl, details.Clan.AvatarUrl);
        Assert.Contains(details.Members, m => m.User.Id == creator.Id && m.Role == "creator");
        Assert.Contains(details.Members, m => m.User.Id == member.Id && m.Role == "member");

        var fromClanEndpoint = await client.GetFromJsonAsyncWithAppConfig<ClanDetailsResponse>($"clan/{clan.Id}");
        Assert.NotNull(fromClanEndpoint);

        Assert.Equal(fromClanEndpoint.Clan.Id, details.Clan.Id);
        Assert.Equal(fromClanEndpoint.Members.Count, details.Members.Count);
    }
}
