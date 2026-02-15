using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Sunrise.Server.Middlewares;

namespace Sunrise.Server.Tests.Services;

public class SafeAuthorizationPolicyProviderTests
{
    [Fact]
    public async Task GetPolicyAsync_ShouldReturnConfiguredPolicy_WhenPolicyExists()
    {
        var options = new AuthorizationOptions();
        options.AddPolicy("RequireAdmin", policy => policy.RequireAuthenticatedUser());

        var provider = new SafeAuthorizationPolicyProvider(Options.Create(options));

        var policy = await provider.GetPolicyAsync("RequireAdmin");

        policy.Should().NotBeNull();
        policy!.Requirements.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPolicyAsync_ShouldReturnDenyPolicy_WhenPolicyDoesNotExist()
    {
        var provider = new SafeAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));

        var policy = await provider.GetPolicyAsync("RequireModerator");

        policy.Should().NotBeNull();
        policy!.Requirements.Should().ContainSingle(r => r.GetType().Name == "AssertionRequirement");
    }
}
