using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Sunrise.Server.Middlewares;

public sealed class SafeAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    private static readonly AuthorizationPolicy DenyAccessPolicy = new AuthorizationPolicyBuilder()
        .RequireAssertion(_ => false)
        .Build();

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await base.GetPolicyAsync(policyName);
        return policy ?? DenyAccessPolicy;
    }
}
