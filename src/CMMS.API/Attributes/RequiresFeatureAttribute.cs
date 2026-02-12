using CMMS.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CMMS.API.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresFeatureAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _feature;

    public RequiresFeatureAttribute(string feature)
    {
        _feature = feature;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var licenseService = context.HttpContext.RequestServices.GetRequiredService<ILicenseService>();

        if (!licenseService.IsFeatureEnabled(_feature))
        {
            var tier = licenseService.GetCurrentTier();
            context.Result = new ObjectResult(new
            {
                success = false,
                error = $"This feature requires a higher license tier. Current tier: {tier}. Required feature: {_feature}.",
                requiredFeature = _feature,
                currentTier = tier.ToString(),
            })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
            return;
        }

        await next();
    }
}
