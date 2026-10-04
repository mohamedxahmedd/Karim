using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Localization;

namespace NationalAirspace.Web.Localization;

/// <summary>
/// Gives every model property a localized display name from the "Field.{PropertyName}" resource key,
/// so labels and validation messages are translated without [Display] attributes on application DTOs.
/// </summary>
public sealed class LocalizedDisplayMetadataProvider(IStringLocalizerFactory factory) : IDisplayMetadataProvider
{
    private readonly IStringLocalizer _localizer = factory.Create(typeof(SharedResource));

    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        if (context.Key.MetadataKind != ModelMetadataKind.Property || context.Key.Name is null) return;
        if (context.DisplayMetadata.DisplayName is not null) return;
        var key = "Field." + context.Key.Name;
        context.DisplayMetadata.DisplayName = () =>
        {
            var value = _localizer[key];
            return value.ResourceNotFound ? context.Key.Name : value.Value;
        };
    }
}
