using System.Security.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.Options;
using Sunrise.API.Extensions;
using Sunrise.Shared.Enums.Users;
using Sunrise.Shared.Extensions.Users;

namespace Sunrise.Server.Middlewares;

public class UserPrivilegeRequirement(UserPrivilege privilege) : IAuthorizationRequirement
{
    public UserPrivilege Privilege { get; } = privilege;
}


public class PrivilegeAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await base.GetPolicyAsync(policyName);

        if (policy != null)
            return policy;

        if (!TryGetPrivilegeByPolicyName(policyName, out var privilege))
            return null;

        return new AuthorizationPolicyBuilder()
            .AddRequirements(new UserPrivilegeRequirement(privilege))
            .Build();
    }

    private static bool TryGetPrivilegeByPolicyName(string policyName, out UserPrivilege privilege)
    {
        privilege = policyName switch
        {
            "RequireSuperUser" => UserPrivilege.SuperUser,
            "RequireAdmin" => UserPrivilege.Admin,
            "RequireModerator" => UserPrivilege.Moderator,
            "RequireBat" => UserPrivilege.Bat,
            _ => UserPrivilege.User
        };

        return privilege != UserPrivilege.User;
    }
}

public class DatabaseAuthorizationHandler : IAuthorizationHandler
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (context.Resource is not HttpContext httpContext)
            return Task.CompletedTask;

        var user = httpContext.GetCurrentUser();

        if (user == null)
            return Task.CompletedTask;

        foreach (var requirement in context.PendingRequirements)
        {
            if (requirement is UserPrivilegeRequirement privilegeRequirement)
            {
                var requiredPrivilege = privilegeRequirement.Privilege;

                if (user.Privilege.HasFlag(requiredPrivilege) || user.Privilege.GetHighestPrivilege() >= requiredPrivilege.GetHighestPrivilege())
                {
                    context.Succeed(requirement);
                }
            }
        }

        return Task.CompletedTask;
    }
}

public class CustomAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            throw new AuthenticationException("You can't access this resource.");
        }

        if (!authorizeResult.Succeeded)
        {
            throw new UnauthorizedAccessException("Please authorize to access this resource.");
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
