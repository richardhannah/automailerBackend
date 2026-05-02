using AutoMailerBackend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class TokenAuthAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var authHeader = context.HttpContext.Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var tokenString = authHeader["Bearer ".Length..];
        if (!Guid.TryParse(tokenString, out var token))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var login = await db.Logins.Include(l => l.User).FirstOrDefaultAsync(l => l.Token == token);

        if (login == null || login.Token == Guid.Empty)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Items["Login"] = login;
        context.HttpContext.Items["User"] = login.User;
    }
}
