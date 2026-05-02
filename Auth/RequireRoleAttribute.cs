using AutoMailerBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AutoMailerBackend.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly UserRole _requiredRole;

    public RequireRoleAttribute(UserRole requiredRole)
    {
        _requiredRole = requiredRole;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.Items["User"] as User;

        if (user == null || user.Role < _requiredRole)
        {
            context.Result = new JsonResult(new { error = "Forbidden" }) { StatusCode = 403 };
        }
    }
}
