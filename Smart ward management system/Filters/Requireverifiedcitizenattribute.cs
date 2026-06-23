using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smart_ward_management_system.Data;

namespace Smart_ward_management_system.Filters
{
    /// <summary>
    /// Action filter that blocks unverified citizens from performing write actions.
    /// Apply [RequireVerifiedCitizen] to any endpoint that citizens must be verified to use.
    /// Staff, Admin, Officer roles pass through automatically.
    /// </summary>
    public class RequireVerifiedCitizenAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;

            // Not authenticated — let [Authorize] handle it
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                await next();
                return;
            }

            var role = user.FindFirst("Role")?.Value;

            // Staff, Admin, Officer roles bypass this check entirely
            if (role != null && role.ToLower() != "citizen")
            {
                await next();
                return;
            }

            // For citizens, check IsVerified claim
            var isVerifiedClaim = user.FindFirst("IsVerified")?.Value;
            if (isVerifiedClaim == "True")
            {
                await next();
                return;
            }

            // Citizen is not verified — also double-check DB in case token is stale
            var userIdClaim = user.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out Guid userId))
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var dbUser = await db.Users.FindAsync(userId);

                if (dbUser != null && dbUser.IsVerified)
                {
                    // DB says verified but token is stale — let them through
                    // They will get a fresh token on next login
                    await next();
                    return;
                }
            }

            // Block the request
            context.Result = new ObjectResult(new
            {
                message = "Your account has not been verified yet. Please wait for ward staff to verify your account before performing this action.",
                code = "ACCOUNT_NOT_VERIFIED"
            })
            {
                StatusCode = 403
            };
        }
    }
}