using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Services;

namespace NationalAirspace.UnitTests.Application;

public class IconTests
{
    private static ServiceInput Service(string? icon) => new() { TitleAr = "خدمة", TitleEn = "Service", Icon = icon };

    [Theory]
    [InlineData("ph-airplane-tilt")]
    [InlineData(null)]
    public void Phosphor_name_or_empty_is_accepted(string? icon) =>
        Assert.DoesNotContain(InputValidator.Validate(Service(icon)), e => e.Field == nameof(ServiceInput.Icon));

    [Theory]
    [InlineData("bi-airplane")]
    [InlineData("ph-airplane\" onmouseover=\"x")]
    public void Legacy_or_unsafe_name_is_rejected(string icon) =>
        Assert.Contains(InputValidator.Validate(Service(icon)), e => e.Field == nameof(ServiceInput.Icon) && e.MessageKey == "Val.Icon");

    [Fact]
    public void Catalog_names_are_valid_and_unique()
    {
        Assert.All(IconCatalog.All, i => Assert.Matches(IconCatalog.Pattern, i.Name));
        Assert.Equal(IconCatalog.All.Count, IconCatalog.All.Select(i => i.Name).Distinct().Count());
    }
}
