using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Sunrise.API.Serializable.Request;
using Sunrise.API.Serializable.Response;
using Sunrise.Shared.Database.Models.Clans;
using Sunrise.Shared.Database.Models.Users;
using Sunrise.Shared.Enums.Users;
using Sunrise.Tests;
using Sunrise.Tests.Abstracts;
using Sunrise.Tests.Extensions;

namespace Sunrise.Server.Tests.API.ClanController;

[Collection("Integration tests collection")]
public class ApiEditClanTests(IntegrationDatabaseFixture fixture) : ApiTest(fixture)
{
    [Fact]
    public async Task TestClanCreatorCanEditAvatarAndDescription()
    {
        var client = App.CreateClient().UseClient("api");

        var creator = await CreateTestUser();
        var clan = await CreateClan(creator);

        client.UseUserAuthToken(await GetUserAuthTokens(creator));

        var avatarResponse = await client.PatchAsJsonAsync("clan/avatar", new EditClanAvatarRequest
        {
            AvatarUrl = "https://cdn.sunrise.test/avatar.png"
        });
        Assert.Equal(HttpStatusCode.OK, avatarResponse.StatusCode);

        var descriptionResponse = await client.PatchAsJsonAsync("clan/description", new EditClanDescriptionRequest
        {
            Description = "Top players only"
        });
        Assert.Equal(HttpStatusCode.OK, descriptionResponse.StatusCode);

        var details = await descriptionResponse.Content.ReadFromJsonAsyncWithAppConfig<ClanDetailsResponse>();
        Assert.NotNull(details);
        Assert.Equal("https://cdn.sunrise.test/avatar.png", details.Clan.AvatarUrl);
        Assert.Equal("Top players only", details.Clan.Description);

        var updatedClan = await Database.Clans.GetClanById(clan.Id);
        Assert.NotNull(updatedClan);
        Assert.Equal("https://cdn.sunrise.test/avatar.png", updatedClan.AvatarUrl);
        Assert.Equal("Top players only", updatedClan.Description);
    }

    [Fact]
    public async Task TestClanCreatorCanEditAvatarUsingPost()
    {
        var client = App.CreateClient().UseClient("api");

        var creator = await CreateTestUser();
        var clan = await CreateClan(creator);

        client.UseUserAuthToken(await GetUserAuthTokens(creator));

        var response = await client.PostAsJsonAsync("clan/avatar", new EditClanAvatarRequest
        {
            AvatarUrl = "https://cdn.sunrise.test/new-avatar.png"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var details = await response.Content.ReadFromJsonAsyncWithAppConfig<ClanDetailsResponse>();
        Assert.NotNull(details);
        Assert.Equal("https://cdn.sunrise.test/new-avatar.png", details.Clan.AvatarUrl);

        var updatedClan = await Database.Clans.GetClanById(clan.Id);
        Assert.NotNull(updatedClan);
        Assert.Equal("https://cdn.sunrise.test/new-avatar.png", updatedClan.AvatarUrl);
    }

    [Fact]
    public async Task TestClanNameChangeHasHundredYearCooldownForRegularUser()
    {
        var client = App.CreateClient().UseClient("api");

        var creator = await CreateTestUser();
        var clan = await CreateClan(creator);

        client.UseUserAuthToken(await GetUserAuthTokens(creator));

        var firstRename = await client.PatchAsJsonAsync("clan/name", new EditClanNameRequest
        {
            Name = $"renamed_{Guid.NewGuid():N}"[..18]
        });
        Assert.Equal(HttpStatusCode.OK, firstRename.StatusCode);

        var secondRename = await client.PatchAsJsonAsync("clan/name", new EditClanNameRequest
        {
            Name = $"second_{Guid.NewGuid():N}"[..18]
        });

        Assert.Equal(HttpStatusCode.BadRequest, secondRename.StatusCode);

        var problem = await secondRename.Content.ReadFromJsonAsyncWithAppConfig<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("change your clan name", problem.Detail);
    }

    [Fact]
    public async Task TestSupporterCanChangeClanNameOncePerMonth()
    {
        var client = App.CreateClient().UseClient("api");

        var creator = await CreateTestUser();
        creator.Privilege |= UserPrivilege.Supporter;
        Database.DbContext.Users.Update(creator);
        await Database.DbContext.SaveChangesAsync();

        var clan = await CreateClan(creator);

        client.UseUserAuthToken(await GetUserAuthTokens(creator));

        var firstRename = await client.PatchAsJsonAsync("clan/name", new EditClanNameRequest
        {
            Name = $"support_{Guid.NewGuid():N}"[..18]
        });
        Assert.Equal(HttpStatusCode.OK, firstRename.StatusCode);

        clan.NameChangedAt = DateTime.UtcNow.AddDays(-31);
        Database.DbContext.Clans.Update(clan);
        await Database.DbContext.SaveChangesAsync();

        var secondRename = await client.PatchAsJsonAsync("clan/name", new EditClanNameRequest
        {
            Name = $"support2_{Guid.NewGuid():N}"[..18]
        });

        Assert.Equal(HttpStatusCode.OK, secondRename.StatusCode);
    }

    [Fact]
    public async Task TestNonCreatorCannotEditClan()
    {
        var client = App.CreateClient().UseClient("api");

        var creator = await CreateTestUser();
        var clan = await CreateClan(creator);

        var member = await CreateTestUser();
        var joinResult = await Database.Clans.JoinClan(clan.Id, member.Id);
        Assert.Equal(Sunrise.Shared.Database.Repositories.ClanRepository.JoinClanResult.Success, joinResult);

        client.UseUserAuthToken(await GetUserAuthTokens(member));

        var response = await client.PatchAsJsonAsync("clan/description", new EditClanDescriptionRequest
        {
            Description = "attempt"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Clan> CreateClan(User creator)
    {
        var createResult = await Database.Clans.CreateClan($"clan_{Guid.NewGuid():N}", null, creator);
        Assert.True(createResult.IsSuccess, createResult.Error);

        return createResult.Value;
    }
}
