using System.Net;
using System.Net.Http.Json;
using Sunrise.API.Serializable.Request;
using Sunrise.Shared.Database.Models.Users;
using Sunrise.Shared.Enums.Users;
using Sunrise.Tests.Abstracts;
using Sunrise.Tests.Services.Mock;
using Sunrise.Tests.Utils;

namespace Sunrise.Server.Tests.API.UserController;

[Collection("Integration tests collection")]
public class ApiModeratorAuthorizationTests(IntegrationDatabaseFixture fixture) : ApiTest(fixture)
{
    private readonly MockService _mocker = new();

    private async Task<User> CreatePrivilegedUser(UserPrivilege privilege)
    {
        var user = _mocker.User.GetRandomUser();
        user.Privilege = privilege;
        await CreateTestUser(user);
        return user;
    }

    [Fact]
    public async Task ModeratorCanSearchSensitiveUsersList()
    {
        var moderator = await CreatePrivilegedUser(UserPrivilege.Moderator);
        var target = await CreateTestUser();

        var client = App.CreateClient().UseClient("api");
        client.UseUserAuthToken(await GetUserAuthTokens(moderator));

        var response = await client.GetAsync($"user/search/list?query={target.Username}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ModeratorCanGetUserSensitive()
    {
        var moderator = await CreatePrivilegedUser(UserPrivilege.Moderator);
        var target = await CreateTestUser();

        var client = App.CreateClient().UseClient("api");
        client.UseUserAuthToken(await GetUserAuthTokens(moderator));

        var response = await client.GetAsync($"user/{target.Id}/sensitive");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ModeratorCanRestrictAndUnrestrictRegularUser()
    {
        var moderator = await CreatePrivilegedUser(UserPrivilege.Moderator);
        var target = await CreateTestUser();

        var client = App.CreateClient().UseClient("api");
        client.UseUserAuthToken(await GetUserAuthTokens(moderator));

        var restrictResponse = await client.PostAsJsonAsync($"user/{target.Id}/edit/restriction", new EditUserRestrictionRequest
        {
            IsRestrict = true,
            RestrictionReason = "moderation test"
        });

        Assert.Equal(HttpStatusCode.OK, restrictResponse.StatusCode);

        var unrestrictResponse = await client.PostAsJsonAsync($"user/{target.Id}/edit/restriction", new EditUserRestrictionRequest
        {
            IsRestrict = false
        });

        Assert.Equal(HttpStatusCode.OK, unrestrictResponse.StatusCode);
    }

    [Fact]
    public async Task ModeratorCannotEditPrivilege()
    {
        var moderator = await CreatePrivilegedUser(UserPrivilege.Moderator);
        var target = await CreateTestUser();

        var client = App.CreateClient().UseClient("api");
        client.UseUserAuthToken(await GetUserAuthTokens(moderator));

        var response = await client.PostAsJsonAsync($"user/{target.Id}/edit/privilege", new EditUserPrivilegeRequest
        {
            Privilege = new[] { UserPrivilege.Supporter }
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ModeratorCannotChangeUsername()
    {
        var moderator = await CreatePrivilegedUser(UserPrivilege.Moderator);
        var target = await CreateTestUser();

        var client = App.CreateClient().UseClient("api");
        client.UseUserAuthToken(await GetUserAuthTokens(moderator));

        var response = await client.PostAsJsonAsync($"user/{target.Id}/username/change", new UsernameChangeRequest
        {
            NewUsername = _mocker.User.GetRandomUsername(10)
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ModeratorCannotChangePassword()
    {
        var moderator = await CreatePrivilegedUser(UserPrivilege.Moderator);
        var target = await CreateTestUser();

        var client = App.CreateClient().UseClient("api");
        client.UseUserAuthToken(await GetUserAuthTokens(moderator));

        var response = await client.PostAsJsonAsync($"user/{target.Id}/password/change", new ResetPasswordRequest
        {
            NewPassword = _mocker.User.GetRandomPassword()
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ModeratorCannotRestrictAdminOrHigher()
    {
        var moderator = await CreatePrivilegedUser(UserPrivilege.Moderator);
        var admin = await CreatePrivilegedUser(UserPrivilege.Admin);

        var client = App.CreateClient().UseClient("api");
        client.UseUserAuthToken(await GetUserAuthTokens(moderator));

        var response = await client.PostAsJsonAsync($"user/{admin.Id}/edit/restriction", new EditUserRestrictionRequest
        {
            IsRestrict = true,
            RestrictionReason = "should be denied"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminStillCanAccessAdminAndModeratorEndpoints()
    {
        var admin = await CreatePrivilegedUser(UserPrivilege.Admin);
        var target = await CreateTestUser();

        var client = App.CreateClient().UseClient("api");
        client.UseUserAuthToken(await GetUserAuthTokens(admin));

        var searchResponse = await client.GetAsync($"user/search/list?query={target.Username}");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

        var sensitiveResponse = await client.GetAsync($"user/{target.Id}/sensitive");
        Assert.Equal(HttpStatusCode.OK, sensitiveResponse.StatusCode);

        var restrictResponse = await client.PostAsJsonAsync($"user/{target.Id}/edit/restriction", new EditUserRestrictionRequest
        {
            IsRestrict = true,
            RestrictionReason = "admin restriction"
        });
        Assert.Equal(HttpStatusCode.OK, restrictResponse.StatusCode);

        var privilegeResponse = await client.PostAsJsonAsync($"user/{target.Id}/edit/privilege", new EditUserPrivilegeRequest
        {
            Privilege = new[] { UserPrivilege.Supporter }
        });
        Assert.Equal(HttpStatusCode.OK, privilegeResponse.StatusCode);

        var usernameResponse = await client.PostAsJsonAsync($"user/{target.Id}/username/change", new UsernameChangeRequest
        {
            NewUsername = _mocker.User.GetRandomUsername(10)
        });
        Assert.Equal(HttpStatusCode.OK, usernameResponse.StatusCode);

        var passwordResponse = await client.PostAsJsonAsync($"user/{target.Id}/password/change", new ResetPasswordRequest
        {
            NewPassword = _mocker.User.GetRandomPassword()
        });
        Assert.Equal(HttpStatusCode.OK, passwordResponse.StatusCode);
    }
}
