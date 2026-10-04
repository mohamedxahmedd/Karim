namespace NationalAirspace.Web.ViewModels;

public sealed record BarPoint(string Label, int Value, string Tip);

/// <summary>Single-series bar chart (SVG) with an optional labelled reference line.</summary>
public sealed record BarChartModel(string Id, string AriaLabel, IReadOnlyList<BarPoint> Points, int? Reference = null, string? ReferenceLabel = null, string? ValueHeader = null);
