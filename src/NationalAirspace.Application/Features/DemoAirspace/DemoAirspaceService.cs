using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;

namespace NationalAirspace.Application.Features.DemoAirspace;

public enum DemoLoadLevel { Normal, Elevated, High }

public sealed record DemoSector(string Code, string Name, int Column, int Row, int Demand, int Capacity, int LoadPercent, DemoLoadLevel Level);
public sealed record DemoAirport(string Code, string Name, int Arrivals, int Departures, int Capacity, int AverageDelayMinutes, DemoLoadLevel Level);
public sealed record DemoFlow(string From, string To, int Volume);
public sealed record DemoHourPoint(string Hour, int Demand, int Capacity);
public sealed record DemoNotification(string Severity, string Message);

public sealed record DemoSnapshot(
    bool IsDemo,
    DateTime GeneratedAtUtc,
    int SimulatedFlights,
    int AverageLoadPercent,
    int ActiveMeasures,
    int OnTimePercent,
    IReadOnlyList<DemoSector> Sectors,
    IReadOnlyList<DemoAirport> Airports,
    IReadOnlyList<DemoFlow> Flows,
    IReadOnlyList<DemoHourPoint> Hourly,
    IReadOnlyList<DemoNotification> Notifications);

/// <summary>
/// Produces SIMULATED, NON-OPERATIONAL data for the public demo dashboard. All sector and airport names
/// are fictional, values are pseudo-random and deterministic per 30-second window. No real, live,
/// restricted or military information is used.
/// </summary>
public sealed class DemoAirspaceService(IClock clock)
{
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(30);

    private static readonly (string Code, string Ar, string En, int Col, int Row, int Capacity)[] SectorDefs =
    [
        ("DMO-N1", "قطاع تجريبي شمال ١", "Demo North 1", 1, 0, 42),
        ("DMO-N2", "قطاع تجريبي شمال ٢", "Demo North 2", 2, 0, 38),
        ("DMO-W1", "قطاع تجريبي غرب ١", "Demo West 1", 0, 1, 34),
        ("DMO-C1", "قطاع تجريبي وسط ١", "Demo Central 1", 1, 1, 48),
        ("DMO-C2", "قطاع تجريبي وسط ٢", "Demo Central 2", 2, 1, 46),
        ("DMO-E1", "قطاع تجريبي شرق ١", "Demo East 1", 3, 1, 36),
        ("DMO-S1", "قطاع تجريبي جنوب ١", "Demo South 1", 1, 2, 32),
        ("DMO-S2", "قطاع تجريبي جنوب ٢", "Demo South 2", 2, 2, 30),
    ];

    private static readonly (string Code, string Ar, string En, int Capacity)[] AirportDefs =
    [
        ("DMA", "مطار تجريبي (أ)", "Demo Airport A", 40),
        ("DMB", "مطار تجريبي (ب)", "Demo Airport B", 28),
        ("DMC", "مطار تجريبي (ج)", "Demo Airport C", 22),
        ("DMD", "مطار تجريبي (د)", "Demo Airport D", 16),
    ];

    private static readonly (string A, string B)[] FlowDefs =
        [("DMO-N1", "DMO-C1"), ("DMO-N2", "DMO-C2"), ("DMO-W1", "DMO-C1"), ("DMO-C1", "DMO-S1"), ("DMO-C2", "DMO-E1"), ("DMO-C2", "DMO-S2"), ("DMO-C1", "DMO-C2")];

    public DemoSnapshot GetSnapshot(Lang lang)
    {
        var now = clock.UtcNow;
        var bucket = now.Ticks / Window.Ticks;
        var rng = new Random(unchecked((int)(bucket ^ (bucket >> 32))));
        var local = clock.ToSiteTime(now);

        var sectors = SectorDefs.Select(d =>
        {
            var demand = (int)Math.Round(d.Capacity * DailyFactor(local.Hour + local.Minute / 60.0) * (0.75 + rng.NextDouble() * 0.45));
            var load = (int)Math.Round(100.0 * demand / d.Capacity);
            return new DemoSector(d.Code, lang.Pick(d.Ar, d.En), d.Col, d.Row, demand, d.Capacity, load, Level(load));
        }).ToList();

        var airports = AirportDefs.Select(d =>
        {
            var total = (int)Math.Round(d.Capacity * DailyFactor(local.Hour) * (0.7 + rng.NextDouble() * 0.4));
            var arrivals = (int)Math.Round(total * (0.4 + rng.NextDouble() * 0.2));
            var load = (int)Math.Round(100.0 * total / d.Capacity);
            var delay = Math.Max(0, (int)Math.Round((load - 70) * 0.4 + rng.Next(0, 4)));
            return new DemoAirport(d.Code, lang.Pick(d.Ar, d.En), arrivals, total - arrivals, d.Capacity, delay, Level(load));
        }).ToList();

        var flows = FlowDefs.Select(f => new DemoFlow(f.A, f.B, rng.Next(4, 26))).ToList();

        var hourly = Enumerable.Range(0, 12).Select(i =>
        {
            var hour = local.AddHours(i - 11);
            var capacity = SectorDefs.Sum(s => s.Capacity);
            var hourRng = new Random(hour.Year * 100000 + hour.DayOfYear * 100 + hour.Hour);
            var demand = (int)Math.Round(capacity * DailyFactor(hour.Hour) * (0.8 + hourRng.NextDouble() * 0.3));
            return new DemoHourPoint(hour.ToString("HH:00", System.Globalization.CultureInfo.InvariantCulture), demand, capacity);
        }).ToList();

        var notifications = new List<DemoNotification>();
        foreach (var s in sectors.Where(s => s.Level == DemoLoadLevel.High).Take(2))
            notifications.Add(new DemoNotification("warning", lang == Lang.Ar
                ? $"ارتفاع الطلب في {s.Name} ({s.LoadPercent}%) — إجراء تنظيمي افتراضي للمحاكاة."
                : $"High demand in {s.Name} ({s.LoadPercent}%) — simulated regulation measure applied."));
        foreach (var a in airports.Where(a => a.AverageDelayMinutes >= 8).Take(1))
            notifications.Add(new DemoNotification("info", lang == Lang.Ar
                ? $"متوسط تأخير افتراضي {a.AverageDelayMinutes} دقيقة في {a.Name}."
                : $"Simulated average delay of {a.AverageDelayMinutes} min at {a.Name}."));
        notifications.Add(new DemoNotification("neutral", lang == Lang.Ar
            ? "جميع البيانات المعروضة افتراضية لأغراض العرض التوضيحي فقط."
            : "All values shown are simulated for demonstration purposes only."));

        return new DemoSnapshot(
            IsDemo: true,
            GeneratedAtUtc: new DateTime(bucket * Window.Ticks, DateTimeKind.Utc),
            SimulatedFlights: sectors.Sum(s => s.Demand),
            AverageLoadPercent: (int)Math.Round(sectors.Average(s => s.LoadPercent)),
            ActiveMeasures: sectors.Count(s => s.Level == DemoLoadLevel.High),
            OnTimePercent: Math.Clamp(96 - (int)Math.Round(airports.Average(a => a.AverageDelayMinutes)), 70, 99),
            sectors, airports, flows, hourly, notifications);
    }

    /// <summary>Smooth daily traffic curve in [0.35, 1.0] peaking late morning and evening.</summary>
    internal static double DailyFactor(double hour)
    {
        var morning = Math.Exp(-Math.Pow(hour - 10, 2) / 12);
        var evening = Math.Exp(-Math.Pow(hour - 19, 2) / 10);
        return 0.35 + 0.65 * Math.Max(morning, evening * 0.9);
    }

    private static DemoLoadLevel Level(int load) => load >= 90 ? DemoLoadLevel.High : load >= 70 ? DemoLoadLevel.Elevated : DemoLoadLevel.Normal;
}
