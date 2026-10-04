using Microsoft.AspNetCore.Localization;
using NationalAirspace.Application.Common;

namespace NationalAirspace.Web.Localization;

/// <summary>Route constraint for the {culture} segment (ar|en).</summary>
public sealed class CultureRouteConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection) =>
        values.TryGetValue(routeKey, out var value) && LangExtensions.IsSupported(value?.ToString());
}

/// <summary>Reads the culture from the {culture} route value and maps it to the formatting culture.</summary>
public sealed class RouteCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var code = httpContext.GetRouteValue(CultureHelper.RouteKey)?.ToString();
        if (!LangExtensions.IsSupported(code)) return NullProviderCultureResult;
        var name = CultureHelper.CultureNameFor(code);
        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(name, name));
    }
}
