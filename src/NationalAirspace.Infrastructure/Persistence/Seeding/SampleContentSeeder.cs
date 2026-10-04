using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Infrastructure.Persistence.Seeding;

/// <summary>
/// Development/demo-only sample items (enabled with <c>Seed:SampleContent=true</c>). Every item is clearly
/// labelled as sample content. Runs only when there is no news yet.
/// </summary>
internal sealed class SampleContentSeeder(ApplicationDbContext db, IFileStorage storage, IClock clock)
{
    public async Task SeedAsync(CancellationToken ct)
    {
        if (await db.NewsArticles.AnyAsync(ct)) return;
        var now = clock.UtcNow;

        var images = new List<MediaFile>();
        var palettes = new[]
        {
            ((byte)11, (byte)37, (byte)69), ((byte)29, (byte)95, (byte)168),
            ((byte)16, (byte)52, (byte)92), ((byte)46, (byte)125, (byte)150),
            ((byte)20, (byte)40, (byte)80), ((byte)150, (byte)120, (byte)60),
            ((byte)8, (byte)30, (byte)60), ((byte)70, (byte)110, (byte)170),
        };
        for (var i = 0; i < palettes.Length; i += 2)
            images.Add(await StoreAsync(SampleFiles.Png(960, 540, palettes[i], palettes[i + 1]), $"sample-{i / 2 + 1}.png", "image/png", MediaKind.Image, ct));

        var center = await db.NewsCategories.FirstOrDefaultAsync(c => c.Slug == "center-news", ct);
        var events = await db.NewsCategories.FirstOrDefaultAsync(c => c.Slug == "events", ct);
        var news = new (string Ar, string En, string SlugAr, string SlugEn, NewsCategory? Cat, int DaysAgo, bool Featured)[]
        {
            ("خبر تجريبي: إطلاق الموقع الإلكتروني الجديد للمركز", "Sample news: the Center launches its new website", "خبر-تجريبي-إطلاق-الموقع", "sample-new-website", center, 1, true),
            ("خبر تجريبي: ورشة عمل حول إدارة تدفق الحركة الجوية", "Sample news: workshop on air traffic flow management", "خبر-تجريبي-ورشة-عمل", "sample-atfm-workshop", events, 6, false),
            ("خبر تجريبي: برنامج تدريبي لتطوير الكوادر", "Sample news: capacity building training program", "خبر-تجريبي-برنامج-تدريبي", "sample-training-program", center, 12, false),
            ("خبر تجريبي: لقاء تنسيقي مع الجهات المعنية", "Sample news: coordination meeting with stakeholders", "خبر-تجريبي-لقاء-تنسيقي", "sample-coordination-meeting", events, 20, false),
        };
        var n = 0;
        foreach (var item in news)
        {
            db.NewsArticles.Add(new NewsArticle
            {
                TitleAr = item.Ar, TitleEn = item.En, SlugAr = item.SlugAr, SlugEn = item.SlugEn,
                SummaryAr = "هذا محتوى تجريبي لأغراض العرض فقط، ويُستبدل بالأخبار الرسمية للمركز.",
                SummaryEn = "This is sample content for demonstration only and will be replaced by the Center's official news.",
                ContentAr = "<p>هذا محتوى تجريبي لأغراض العرض فقط، ويُستبدل بالأخبار الرسمية للمركز.</p><p>يمكن تعديل هذا الخبر أو حذفه من لوحة التحكم.</p>",
                ContentEn = "<p>This is sample content for demonstration only and will be replaced by the Center's official news.</p><p>Edit or delete it from the admin dashboard.</p>",
                NewsCategoryId = item.Cat?.Id, FeaturedImageId = images[n % images.Count].Id, IsFeatured = item.Featured,
                PublishDateUtc = now.AddDays(-item.DaysAgo), Status = ContentStatus.Published,
            });
            n++;
        }

        db.Announcements.Add(new Announcement
        {
            TitleAr = "إعلان تجريبي مهم", TitleEn = "Important sample announcement",
            ContentAr = "<p>إعلان تجريبي لأغراض العرض فقط.</p>", ContentEn = "<p>Sample announcement for demonstration only.</p>",
            IsImportant = true, PublishDateUtc = now.AddDays(-2), ExpiresAtUtc = now.AddDays(30), Status = ContentStatus.Published,
        });
        db.Announcements.Add(new Announcement
        {
            TitleAr = "إعلان تجريبي منتهي الصلاحية", TitleEn = "Expired sample announcement",
            ContentAr = "<p>إعلان تجريبي انتهت مدته ويظهر في الأرشيف.</p>", ContentEn = "<p>An expired sample announcement shown in the archive.</p>",
            PublishDateUtc = now.AddDays(-40), ExpiresAtUtc = now.AddDays(-10), Status = ContentStatus.Archived,
        });

        var pdf = await StoreAsync(SampleFiles.Pdf("Sample report - demonstration only"), "sample-report.pdf", "application/pdf", MediaKind.Document, ct);
        var studies = await db.ReportCategories.FirstOrDefaultAsync(c => c.Slug == "technical-studies", ct);
        db.Reports.Add(new Report
        {
            TitleAr = "تقرير تجريبي: مفاهيم إدارة المجال الجوي", TitleEn = "Sample report: airspace management concepts",
            SlugAr = "تقرير-تجريبي-مفاهيم", SlugEn = "sample-report-concepts",
            DescriptionAr = "ملف تجريبي لأغراض العرض فقط.", DescriptionEn = "Sample file for demonstration only.",
            ReportCategoryId = studies?.Id, PublicationDate = now.Date.AddDays(-15), PdfFileId = pdf.Id, CoverImageId = images[1].Id,
            Status = ContentStatus.Published,
        });

        var courses = new (string Ar, string En, string SlugAr, string SlugEn, int StartIn, int Days, RegistrationStatus Reg)[]
        {
            ("برنامج تجريبي: أساسيات إدارة المجال الجوي", "Sample program: airspace management fundamentals", "برنامج-تجريبي-أساسيات", "sample-fundamentals", 14, 5, RegistrationStatus.Open),
            ("برنامج تجريبي: مقدمة في إدارة تدفق الحركة الجوية", "Sample program: introduction to ATFM", "برنامج-تجريبي-مقدمة-atfm", "sample-intro-atfm", 45, 3, RegistrationStatus.ComingSoon),
            ("برنامج تجريبي سابق: تحليل البيانات", "Past sample program: data analysis", "برنامج-تجريبي-تحليل-البيانات", "sample-data-analysis", -60, 4, RegistrationStatus.Closed),
        };
        var c = 0;
        foreach (var course in courses)
        {
            db.TrainingCourses.Add(new TrainingCourse
            {
                TitleAr = course.Ar, TitleEn = course.En, SlugAr = course.SlugAr, SlugEn = course.SlugEn,
                DescriptionAr = "<p>برنامج تدريبي تجريبي لأغراض العرض فقط.</p>", DescriptionEn = "<p>Sample training program for demonstration only.</p>",
                DurationAr = $"{course.Days} أيام", DurationEn = $"{course.Days} days",
                TargetAudienceAr = "المتخصصون في مجال الطيران (مثال)", TargetAudienceEn = "Aviation professionals (example)",
                LocationAr = "مقر المركز (مثال)", LocationEn = "Center premises (example)",
                StartDate = now.Date.AddDays(course.StartIn), EndDate = now.Date.AddDays(course.StartIn + course.Days - 1),
                RegistrationStatus = course.Reg, CourseImageId = images[(c + 2) % images.Count].Id, Status = ContentStatus.Published,
            });
            c++;
        }

        var album = new PhotoAlbum
        {
            TitleAr = "ألبوم تجريبي", TitleEn = "Sample album",
            DescriptionAr = "صور تجريدية لأغراض العرض فقط.", DescriptionEn = "Abstract images for demonstration only.",
            EventDate = now.Date.AddDays(-5), CoverMediaId = images[0].Id, DisplayOrder = 1, Status = ContentStatus.Published,
        };
        db.PhotoAlbums.Add(album);
        for (var i = 0; i < images.Count; i++)
            db.AlbumPhotos.Add(new AlbumPhoto { AlbumId = album.Id, MediaFileId = images[i].Id, DisplayOrder = i + 1, CaptionAr = $"صورة تجريبية {i + 1}", CaptionEn = $"Sample image {i + 1}" });

        await db.SaveChangesAsync(ct);
    }

    private async Task<MediaFile> StoreAsync(byte[] bytes, string name, string contentType, MediaKind kind, CancellationToken ct)
    {
        using var stream = new MemoryStream(bytes);
        var path = await storage.SaveAsync(stream, kind, Path.GetExtension(name), ct);
        var media = new MediaFile
        {
            OriginalFileName = name, StoragePath = path, ContentType = contentType, SizeBytes = bytes.Length, Kind = kind,
            AltTextAr = "صورة تجريبية", AltTextEn = "Sample image",
        };
        db.MediaFiles.Add(media);
        return media;
    }
}
