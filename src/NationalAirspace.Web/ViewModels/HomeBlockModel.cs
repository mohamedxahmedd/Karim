using NationalAirspace.Application.Features.Home;

namespace NationalAirspace.Web.ViewModels;

/// <summary>A homepage block together with the page data it may render.</summary>
public sealed record HomeBlockModel(HomeBlockDto Block, HomePageDto Page);
