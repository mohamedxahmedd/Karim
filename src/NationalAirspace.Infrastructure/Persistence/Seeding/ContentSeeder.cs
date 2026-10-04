using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Features.Pages;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the baseline structure and editable placeholder content. Every block is idempotent: it only runs
/// when its target table/keys are empty, so administrator edits are never overwritten.
/// Placeholder texts are explicitly marked and must be replaced with approved official content.
/// </summary>
internal sealed class ContentSeeder(ApplicationDbContext db)
{
    private const string PendingAr = "(نص مبدئي — يُستبدل بالنص الرسمي المعتمد من المركز)";
    private const string PendingEn = "(Provisional text — to be replaced with the Center's approved official wording)";

    private const string DescriptionAr =
        "نعمل على تطوير وإدارة المجال الجوي وتعزيز سلامة وانسيابية الحركة الجوية من خلال التخطيط والتصميم وإدارة تدفق الحركة الجوية وتطوير الكوادر والخدمات المتخصصة.";
    private const string DescriptionEn =
        "We develop and manage the airspace and enhance the safety and fluidity of air traffic through planning and design, air traffic flow management, capacity building and specialized services.";

    public async Task SeedAsync(CancellationToken ct)
    {
        await SeedSettingsAsync(ct);
        await SeedPagesAsync(ct);
        await SeedMenusAsync(ct);
        await SeedHomeAsync(ct);
        await SeedServicesAsync(ct);
        await SeedCategoriesAsync(ct);
        await SeedOrganizationAsync(ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedSettingsAsync(CancellationToken ct)
    {
        if (await db.SiteSettings.AnyAsync(ct)) return;
        db.SiteSettings.Add(new SiteSettings
        {
            Id = SiteSettings.SingletonId,
            SiteNameAr = "المركز القومي لإدارة المجال الجوي",
            SiteNameEn = "National Airspace Management Center",
            TaglineAr = "نحو مجال جوي أكثر أمانًا وكفاءة واستدامة",
            TaglineEn = "Towards a safer, more efficient and sustainable airspace",
            AddressAr = "القاهرة، جمهورية مصر العربية (العنوان التفصيلي يُحدَّث من لوحة التحكم)",
            AddressEn = "Cairo, Arab Republic of Egypt (full address to be updated from the dashboard)",
            Phone = "+20 2 0000 0000",
            Email = "info@namc.example",
            WorkingHoursAr = "الأحد – الخميس: 9:00 ص – 3:00 م (مبدئي)",
            WorkingHoursEn = "Sunday – Thursday: 9:00 AM – 3:00 PM (provisional)",
            FooterTextAr = DescriptionAr,
            FooterTextEn = DescriptionEn,
            DefaultMetaDescriptionAr = "الموقع الرسمي للمركز القومي لإدارة المجال الجوي — " + DescriptionAr,
            DefaultMetaDescriptionEn = "Official website of the National Airspace Management Center — " + DescriptionEn,
        });
    }

    // ------------------------------------------------------------------ pages

    private async Task SeedPagesAsync(CancellationToken ct)
    {
        var existing = await db.ContentPages.Select(p => p.SystemKey).Where(k => k != null).ToListAsync(ct);

        if (!existing.Contains(SystemPages.About))
        {
            db.ContentPages.Add(Page(SystemPages.About, "نبذة عن المركز", "About the Center", "نبذة-عن-المركز", "about-the-center",
                DescriptionAr, DescriptionEn,
                Section("establishment", PageSectionLayout.Text, "النشأة", "Establishment", "bi-building",
                    $"<p>يستند المركز القومي لإدارة المجال الجوي في إطاره القانوني إلى <strong>القانون رقم ٢٥ لسنة ٢٠٢٢</strong>. {PendingAr}</p>",
                    $"<p>The National Airspace Management Center's legal framework references <strong>Law No. 25 of 2022</strong>. {PendingEn}</p>"),
                Section("legal-basis", PageSectionLayout.Highlight, "الأساس القانوني", "Legal Basis", "bi-journal-text",
                    $"<p>المرجع القانوني: القانون رقم ٢٥ لسنة ٢٠٢٢. تُضاف هنا المواد ذات الصلة وملخصها بعد اعتمادها. {PendingAr}</p>",
                    $"<p>Legal reference: Law No. 25 of 2022. The relevant articles and their summary will be added here once approved. {PendingEn}</p>"),
                Section("organizational-role", PageSectionLayout.Text, "الدور المؤسسي", "Organizational Role", "bi-diagram-3",
                    $"<p>{DescriptionAr}</p><p>{PendingAr}</p>",
                    $"<p>{DescriptionEn}</p><p>{PendingEn}</p>")));
        }

        if (!existing.Contains(SystemPages.VisionMission))
        {
            db.ContentPages.Add(Page(SystemPages.VisionMission, "الرؤية والرسالة", "Vision & Mission", "الرؤية-والرسالة", "vision-mission",
                "توجهات المركز الاستراتيجية.", "The Center's strategic direction.",
                Section(SectionKeys.Vision, PageSectionLayout.Card, "الرؤية", "Vision", "bi-eye",
                    $"<p>مجال جوي أكثر أمانًا وكفاءة واستدامة. {PendingAr}</p>",
                    $"<p>A safer, more efficient and sustainable airspace. {PendingEn}</p>"),
                Section(SectionKeys.Mission, PageSectionLayout.Card, "الرسالة", "Mission", "bi-bullseye",
                    $"<p>{DescriptionAr} {PendingAr}</p>",
                    $"<p>{DescriptionEn} {PendingEn}</p>"),
                Section("strategic-objectives", PageSectionLayout.Text, "الأهداف الاستراتيجية", "Strategic Objectives", "bi-flag",
                    $"<ul><li>تعزيز سلامة الحركة الجوية.</li><li>رفع كفاءة استخدام المجال الجوي وسعته.</li><li>تحقيق انسيابية تدفق الحركة الجوية.</li><li>تطوير الكوادر الوطنية المتخصصة.</li><li>دعم الاستدامة البيئية للعمليات الجوية.</li></ul><p>{PendingAr}</p>",
                    $"<ul><li>Enhance air traffic safety.</li><li>Increase airspace efficiency and capacity.</li><li>Ensure fluid air traffic flow.</li><li>Develop specialized national expertise.</li><li>Support the environmental sustainability of air operations.</li></ul><p>{PendingEn}</p>")));
        }

        if (!existing.Contains(SystemPages.Responsibilities))
        {
            var cards = new (string Ar, string En, string Icon)[]
            {
                ("تخطيط وتصميم المجال الجوي", "Airspace planning and design", "bi-bounding-box"),
                ("إدارة تدفق الحركة الجوية", "Air traffic flow management", "bi-arrow-left-right"),
                ("التنسيق مع الجهات المعنية", "Coordination with stakeholders", "bi-people"),
                ("الدراسات الفنية والتحليل", "Technical studies and analysis", "bi-graph-up"),
                ("تطوير الكوادر وبناء القدرات", "Capacity building and training", "bi-mortarboard"),
                ("الخدمات المتخصصة", "Specialized services", "bi-gear"),
            };
            db.ContentPages.Add(Page(SystemPages.Responsibilities, "اختصاصات المركز", "Center Responsibilities", "اختصاصات-المركز", "responsibilities",
                "تُعرض المجالات التالية بصورة مبدئية استنادًا إلى التعريف العام للمركز، ويُستكمل النص الرسمي للاختصاصات وفق القانون رقم ٢٥ لسنة ٢٠٢٢.",
                "The following areas are shown provisionally, based on the Center's general definition. The official text of the responsibilities will be completed according to Law No. 25 of 2022.",
                cards.Select(c => Section(null, PageSectionLayout.Card, c.Ar, c.En, c.Icon,
                    $"<p>{PendingAr}</p>", $"<p>{PendingEn}</p>")).ToArray()));
        }

        if (!existing.Contains(SystemPages.AirspaceManagement))
        {
            db.ContentPages.Add(Page(SystemPages.AirspaceManagement, "إدارة المجال الجوي", "Airspace Management", "إدارة-المجال-الجوي", "airspace-management",
                "إدارة المجال الجوي هي عملية تخطيط وتنظيم استخدام المجال الجوي لتلبية احتياجات جميع المستخدمين بأعلى درجات السلامة والكفاءة.",
                "Airspace management is the planning and organization of airspace use to meet the needs of all users with the highest levels of safety and efficiency.",
                Section(null, PageSectionLayout.Card, "تخطيط المجال الجوي", "Airspace Planning", "bi-map",
                    "<p>دراسة الاحتياجات الحالية والمستقبلية لمستخدمي المجال الجوي ووضع الخطط اللازمة لتلبيتها.</p>",
                    "<p>Studying the current and future needs of airspace users and developing the plans needed to meet them.</p>"),
                Section(null, PageSectionLayout.Card, "تصميم المجال الجوي", "Airspace Design", "bi-bounding-box-circles",
                    "<p>تصميم الهياكل والمسارات والقطاعات بما يدعم السلامة ويرفع السعة ويقلل التعقيد.</p>",
                    "<p>Designing structures, routes and sectors that support safety, increase capacity and reduce complexity.</p>"),
                Section(null, PageSectionLayout.Card, "الاستخدام المرن للمجال الجوي", "Flexible Use of Airspace", "bi-sliders",
                    "<p>مفهوم دولي يقوم على تخصيص المجال الجوي وفق الحاجة الفعلية وبالتنسيق بين الجهات المعنية.</p>",
                    "<p>An international concept based on allocating airspace according to actual need, in coordination with all stakeholders.</p>"),
                Section(null, PageSectionLayout.Card, "السلامة والكفاءة والاستدامة", "Safety, Efficiency & Sustainability", "bi-shield-check",
                    "<p>تحقيق التوازن بين متطلبات السلامة وكفاءة العمليات وتقليل الأثر البيئي.</p>",
                    "<p>Balancing safety requirements, operational efficiency and reduced environmental impact.</p>")));
        }

        if (!existing.Contains(SystemPages.Atfm))
        {
            db.ContentPages.Add(Page(SystemPages.Atfm, "إدارة تدفق الحركة الجوية (ATFM)", "Air Traffic Flow Management (ATFM)",
                "إدارة-تدفق-الحركة-الجوية", "air-traffic-flow-management",
                "محتوى تعريفي بمفهوم إدارة تدفق الحركة الجوية. لا تتضمن هذه الصفحة أي وصول إلى أنظمة تشغيلية.",
                "Informational content about the concept of air traffic flow management. This page does not provide access to any operational system.",
                Section("what-is-atfm", PageSectionLayout.Text, "ما هي إدارة تدفق الحركة الجوية؟", "What is ATFM?", "bi-question-circle",
                    "<p>إدارة تدفق الحركة الجوية خدمة تهدف إلى تحقيق تدفق آمن ومنظم وسريع للحركة الجوية، من خلال ضمان الاستخدام الأمثل لسعة المجال الجوي والمطارات بما يتوافق مع حجم الطلب.</p>",
                    "<p>Air traffic flow management is a service that aims to achieve a safe, orderly and expeditious flow of air traffic by ensuring that airspace and aerodrome capacity is used to the maximum extent possible and that traffic volume is compatible with the declared capacity.</p>"),
                Section("objectives", PageSectionLayout.Card, "الأهداف", "Objectives", "bi-bullseye",
                    "<p>الموازنة بين الطلب والسعة، وتقليل التأخيرات، ودعم سلامة العمليات.</p>",
                    "<p>Balancing demand and capacity, reducing delays and supporting operational safety.</p>"),
                Section("benefits", PageSectionLayout.Card, "الفوائد", "Benefits", "bi-award",
                    "<p>تحسين القدرة على التنبؤ، وخفض استهلاك الوقود، وتقليل الانبعاثات، ورفع كفاءة التشغيل.</p>",
                    "<p>Better predictability, lower fuel burn, reduced emissions and higher operational efficiency.</p>"),
                Section("demand-capacity", PageSectionLayout.Card, "إدارة الطلب والسعة", "Demand & Capacity Management", "bi-bar-chart",
                    "<p>تحليل الطلب المتوقع ومقارنته بالسعة المعلنة لاتخاذ إجراءات استباقية.</p>",
                    "<p>Analysing forecast demand against declared capacity to take proactive measures.</p>"),
                Section("flow-optimization", PageSectionLayout.Card, "تحسين تدفق الحركة", "Traffic Flow Optimization", "bi-shuffle",
                    "<p>تطبيق حلول مثل إعادة التوجيه وتنظيم أوقات الإقلاع لتحقيق تدفق متوازن.</p>",
                    "<p>Applying solutions such as re-routing and departure-time management to achieve a balanced flow.</p>"),
                Section("coordination", PageSectionLayout.Card, "التنسيق", "Coordination", "bi-people",
                    "<p>تنسيق مستمر بين وحدات الخدمات الجوية والمطارات وشركات الطيران والجهات المعنية.</p>",
                    "<p>Continuous coordination between air navigation units, airports, airlines and other stakeholders.</p>"),
                Section("airspace-capacity", PageSectionLayout.Card, "سعة المجال الجوي", "Airspace Capacity", "bi-speedometer2",
                    "<p>تقدير السعة الفعلية للقطاعات والمطارات وتحسينها بصورة مستدامة.</p>",
                    "<p>Assessing and sustainably improving the real capacity of sectors and aerodromes.</p>"),
                Section("operational-efficiency", PageSectionLayout.Card, "الكفاءة التشغيلية", "Operational Efficiency", "bi-lightning-charge",
                    "<p>تحقيق أفضل استخدام للموارد المتاحة مع الحفاظ على أعلى مستويات السلامة.</p>",
                    "<p>Making the best use of available resources while maintaining the highest safety levels.</p>"),
                Section("step-demand", PageSectionLayout.ProcessStep, "الطلب على الحركة الجوية", "Traffic Demand", "bi-airplane",
                    "<p>رصد وتقدير حجم الحركة المتوقعة.</p>", "<p>Monitoring and forecasting expected traffic.</p>"),
                Section("step-capacity", PageSectionLayout.ProcessStep, "تحليل السعة", "Capacity Analysis", "bi-clipboard-data",
                    "<p>مقارنة الطلب بالسعة المتاحة.</p>", "<p>Comparing demand with available capacity.</p>"),
                Section("step-flow", PageSectionLayout.ProcessStep, "إدارة التدفق", "Flow Management", "bi-sliders",
                    "<p>اختيار الإجراءات المناسبة لموازنة التدفق.</p>", "<p>Selecting suitable measures to balance the flow.</p>"),
                Section("step-coordination", PageSectionLayout.ProcessStep, "التنسيق", "Coordination", "bi-people",
                    "<p>التنسيق مع جميع الأطراف المعنية.</p>", "<p>Coordinating with all stakeholders.</p>"),
                Section("step-optimized", PageSectionLayout.ProcessStep, "تدفق حركة محسَّن", "Optimized Traffic Flow", "bi-check2-circle",
                    "<p>حركة جوية آمنة ومنتظمة وفعّالة.</p>", "<p>Safe, orderly and efficient air traffic.</p>")));
        }
    }

    // ------------------------------------------------------------------ menus

    private async Task SeedMenusAsync(CancellationToken ct)
    {
        if (await db.MenuItems.AnyAsync(ct)) return;
        var order = 0;

        MenuItem Add(MenuLocation location, string ar, string en, string url, MenuItem? parent = null)
        {
            var item = new MenuItem
            {
                Location = location, TitleAr = ar, TitleEn = en, Url = url, ParentId = parent?.Id, DisplayOrder = ++order, IsVisible = true,
            };
            db.MenuItems.Add(item);
            return item;
        }

        Add(MenuLocation.Header, "الرئيسية", "Home", "/");
        var about = Add(MenuLocation.Header, "عن المركز", "About the Center", "/about");
        Add(MenuLocation.Header, "نبذة عن المركز", "Overview", "/about", about);
        Add(MenuLocation.Header, "الرؤية والرسالة", "Vision & Mission", "/about/vision-mission", about);
        Add(MenuLocation.Header, "اختصاصات المركز", "Responsibilities", "/about/responsibilities", about);
        Add(MenuLocation.Header, "الهيكل التنظيمي", "Organizational Structure", "/about/structure", about);
        Add(MenuLocation.Header, "قيادات المركز", "Leadership", "/about/leadership", about);
        Add(MenuLocation.Header, "التشريعات واللوائح", "Legislation & Regulations", "/about/legislation", about);
        Add(MenuLocation.Header, "خدماتنا", "Services", "/services");
        var airspace = Add(MenuLocation.Header, "إدارة المجال الجوي", "Airspace Management", "/airspace-management");
        Add(MenuLocation.Header, "نظرة عامة", "Overview", "/airspace-management", airspace);
        Add(MenuLocation.Header, "لوحة العرض التوضيحية", "Demo Dashboard", "/airspace-demo", airspace);
        Add(MenuLocation.Header, "إدارة تدفق الحركة الجوية", "ATFM", "/atfm");
        var news = Add(MenuLocation.Header, "الأخبار والإعلانات", "News & Announcements", "/news");
        Add(MenuLocation.Header, "الأخبار", "News", "/news", news);
        Add(MenuLocation.Header, "الإعلانات", "Announcements", "/announcements", news);
        Add(MenuLocation.Header, "الدراسات والتقارير", "Studies & Reports", "/reports");
        Add(MenuLocation.Header, "التدريب والتأهيل", "Training", "/training");
        var media = Add(MenuLocation.Header, "المركز الإعلامي", "Media Center", "/media");
        Add(MenuLocation.Header, "معرض الصور", "Photo Gallery", "/media/photos", media);
        Add(MenuLocation.Header, "معرض الفيديو", "Video Gallery", "/media/videos", media);
        Add(MenuLocation.Header, "تواصل معنا", "Contact Us", "/contact");

        Add(MenuLocation.Footer, "عن المركز", "About the Center", "/about");
        Add(MenuLocation.Footer, "خدماتنا", "Services", "/services");
        Add(MenuLocation.Footer, "إدارة تدفق الحركة الجوية", "ATFM", "/atfm");
        Add(MenuLocation.Footer, "الأخبار", "News", "/news");
        Add(MenuLocation.Footer, "الدراسات والتقارير", "Studies & Reports", "/reports");
        Add(MenuLocation.Footer, "التشريعات واللوائح", "Legislation", "/about/legislation");
        Add(MenuLocation.Footer, "التدريب والتأهيل", "Training", "/training");
        Add(MenuLocation.Footer, "تواصل معنا", "Contact Us", "/contact");
    }

    // ------------------------------------------------------------------ home

    private async Task SeedHomeAsync(CancellationToken ct)
    {
        if (!await db.HeroSlides.AnyAsync(ct))
        {
            db.HeroSlides.Add(new HeroSlide
            {
                TitleAr = "نحو مجال جوي أكثر أمانًا وكفاءة واستدامة",
                TitleEn = "Towards a safer, more efficient and sustainable airspace",
                SubtitleAr = "المركز القومي لإدارة المجال الجوي",
                SubtitleEn = "National Airspace Management Center",
                DescriptionAr = DescriptionAr,
                DescriptionEn = DescriptionEn,
                PrimaryButtonTextAr = "تعرف على المركز", PrimaryButtonTextEn = "About the Center", PrimaryButtonUrl = "/about",
                SecondaryButtonTextAr = "خدمات المركز", SecondaryButtonTextEn = "Our Services", SecondaryButtonUrl = "/services",
                DisplayOrder = 1, IsActive = true,
            });
            db.HeroSlides.Add(new HeroSlide
            {
                TitleAr = "إدارة تدفق الحركة الجوية",
                TitleEn = "Air Traffic Flow Management",
                SubtitleAr = "موازنة الطلب والسعة",
                SubtitleEn = "Balancing demand and capacity",
                DescriptionAr = "تخطيط استباقي وتنسيق مستمر لتحقيق تدفق آمن ومنتظم وفعّال للحركة الجوية.",
                DescriptionEn = "Proactive planning and continuous coordination for a safe, orderly and efficient flow of air traffic.",
                PrimaryButtonTextAr = "اعرف المزيد", PrimaryButtonTextEn = "Learn more", PrimaryButtonUrl = "/atfm",
                SecondaryButtonTextAr = "لوحة العرض التوضيحية", SecondaryButtonTextEn = "Demo dashboard", SecondaryButtonUrl = "/airspace-demo",
                DisplayOrder = 2, IsActive = true,
            });
        }

        var keys = await db.HomeSections.Select(s => s.Key).ToListAsync(ct);
        var order = keys.Count;
        void Add(HomeSectionKey key, string ar, string en, string? subAr = null, string? subEn = null, string? descAr = null, string? descEn = null,
                 string? btnAr = null, string? btnEn = null, string? url = null, int count = 3)
        {
            if (keys.Contains(key)) return;
            db.HomeSections.Add(new HomeSection
            {
                Key = key, TitleAr = ar, TitleEn = en, SubtitleAr = subAr, SubtitleEn = subEn, DescriptionAr = descAr, DescriptionEn = descEn,
                ButtonTextAr = btnAr, ButtonTextEn = btnEn, ButtonUrl = url, ItemCount = count, IsVisible = true, DisplayOrder = ++order,
            });
        }

        Add(HomeSectionKey.About, "عن المركز", "About the Center", "المركز القومي لإدارة المجال الجوي", "National Airspace Management Center",
            DescriptionAr, DescriptionEn, "تعرف على المركز", "Learn more", "/about");
        Add(HomeSectionKey.VisionMission, "الرؤية والرسالة", "Vision & Mission", "توجهاتنا", "Our direction",
            btnAr: "المزيد", btnEn: "More", url: "/about/vision-mission");
        Add(HomeSectionKey.Responsibilities, "اختصاصات المركز", "Center Responsibilities", "مجالات العمل", "Areas of work",
            btnAr: "جميع الاختصاصات", btnEn: "All responsibilities", url: "/about/responsibilities", count: 6);
        Add(HomeSectionKey.Services, "خدماتنا", "Our Services", "خدمات متخصصة", "Specialized services",
            "مجموعة من الخدمات المتخصصة في مجالات المجال الجوي والحركة الجوية.", "A range of specialized services in airspace and air traffic.",
            "جميع الخدمات", "All services", "/services", 8);
        Add(HomeSectionKey.Atfm, "إدارة تدفق الحركة الجوية (ATFM)", "Air Traffic Flow Management (ATFM)", "من الطلب إلى تدفق محسَّن", "From demand to optimized flow",
            "منهجية متكاملة لموازنة الطلب على الحركة الجوية مع السعة المتاحة.", "An integrated approach to balancing air traffic demand with available capacity.",
            "اعرف المزيد", "Learn more", "/atfm");
        Add(HomeSectionKey.LatestNews, "أحدث الأخبار", "Latest News", "المركز الإعلامي", "Media center",
            btnAr: "جميع الأخبار", btnEn: "All news", url: "/news", count: 3);
        Add(HomeSectionKey.LatestReports, "الدراسات والتقارير", "Studies & Reports", "إصدارات المركز", "Publications",
            btnAr: "جميع الإصدارات", btnEn: "All publications", url: "/reports", count: 3);
        Add(HomeSectionKey.Training, "التدريب والتأهيل", "Training & Qualification", "بناء القدرات", "Capacity building",
            btnAr: "جميع البرامج", btnEn: "All programs", url: "/training", count: 3);
        Add(HomeSectionKey.Statistics, "المركز في أرقام", "The Center in Numbers", "مؤشرات (قيم مبدئية قابلة للتعديل)", "Highlights (editable provisional values)");
        Add(HomeSectionKey.ImportantLinks, "روابط مهمة", "Important Links", "جهات ذات صلة", "Related organizations");
        Add(HomeSectionKey.Contact, "تواصل معنا", "Contact Us", "يسعدنا تواصلكم", "We are glad to hear from you",
            "لأي استفسارات أو مقترحات يمكنكم التواصل معنا عبر النموذج الإلكتروني أو بيانات الاتصال.",
            "For enquiries or suggestions, contact us through the online form or our contact details.",
            "راسلنا", "Send a message", "/contact");

        if (!await db.Statistics.AnyAsync(ct))
        {
            var stats = new (string Ar, string En, string Value, string Icon)[]
            {
                ("خدمات متخصصة", "Specialized services", "8", "bi-grid"),
                ("مجالات عمل رئيسية", "Main areas of work", "6", "bi-diagram-3"),
                ("مراحل دورة إدارة التدفق", "ATFM process stages", "5", "bi-arrow-repeat"),
                ("سنة الإطار القانوني (القانون ٢٥)", "Legal framework year (Law 25)", "2022", "bi-journal-text"),
            };
            var i = 0;
            foreach (var s in stats)
                db.Statistics.Add(new Statistic { LabelAr = s.Ar, LabelEn = s.En, Value = s.Value, Icon = s.Icon, DisplayOrder = ++i, IsActive = true });
        }

        if (!await db.ImportantLinks.AnyAsync(ct))
        {
            var links = new (string Ar, string En, string Url, string Icon)[]
            {
                ("وزارة الطيران المدني", "Ministry of Civil Aviation", "https://www.civilaviation.gov.eg", "bi-bank"),
                ("منظمة الطيران المدني الدولي (إيكاو)", "International Civil Aviation Organization (ICAO)", "https://www.icao.int", "bi-globe"),
                ("المنظمة الأوروبية لسلامة الملاحة الجوية", "EUROCONTROL", "https://www.eurocontrol.int", "bi-globe2"),
                ("بوابة مصر الرقمية", "Digital Egypt portal", "https://digital.gov.eg", "bi-laptop"),
            };
            var i = 0;
            foreach (var l in links)
                db.ImportantLinks.Add(new ImportantLink { TitleAr = l.Ar, TitleEn = l.En, Url = l.Url, Icon = l.Icon, DisplayOrder = ++i, IsActive = true });
        }
    }

    // ------------------------------------------------------------------ services

    private async Task SeedServicesAsync(CancellationToken ct)
    {
        if (await db.CenterServices.AnyAsync(ct)) return;
        var services = new (string Ar, string En, string SlugAr, string SlugEn, string Icon, string ShortAr, string ShortEn)[]
        {
            ("تخطيط وتصميم المجال الجوي", "Airspace Planning & Design", "تخطيط-وتصميم-المجال-الجوي", "airspace-planning-design", "bi-bounding-box",
                "تخطيط هياكل المجال الجوي وتصميم المسارات والقطاعات بما يعزز السلامة والسعة.", "Planning airspace structures and designing routes and sectors to enhance safety and capacity."),
            ("إدارة تدفق الحركة الجوية — ATFM", "Air Traffic Flow Management — ATFM", "إدارة-تدفق-الحركة-الجوية-atfm", "air-traffic-flow-management-atfm", "bi-arrow-left-right",
                "موازنة الطلب على الحركة الجوية مع السعة المتاحة لتقليل التأخير ورفع الكفاءة.", "Balancing air traffic demand with available capacity to reduce delays and increase efficiency."),
            ("دعم معلومات الطيران", "Aeronautical Information Support", "دعم-معلومات-الطيران", "aeronautical-information-support", "bi-info-circle",
                "دعم إعداد وتنظيم المعلومات ذات الصلة بالمجال الجوي.", "Supporting the preparation and organization of airspace-related information."),
            ("الدراسات الفنية", "Technical Studies", "الدراسات-الفنية", "technical-studies", "bi-clipboard-data",
                "إعداد الدراسات الفنية والتحليلية في مجالات المجال الجوي والحركة الجوية.", "Preparing technical and analytical studies in airspace and air traffic."),
            ("الاستشارات في مجال الطيران والمجال الجوي", "Aviation & Airspace Consulting", "الاستشارات-في-مجال-الطيران", "aviation-airspace-consulting", "bi-chat-square-text",
                "تقديم الرأي الفني والاستشاري للجهات ذات الصلة.", "Providing technical and advisory opinions to relevant entities."),
            ("التدريب والتأهيل", "Training & Qualification", "التدريب-والتأهيل", "training-qualification", "bi-mortarboard",
                "برامج تدريبية متخصصة لبناء القدرات الوطنية.", "Specialized training programs to build national capabilities."),
            ("تطوير المجال الجوي", "Airspace Development", "تطوير-المجال-الجوي", "airspace-development", "bi-graph-up-arrow",
                "مبادرات ومشروعات لتطوير المجال الجوي وفق أفضل الممارسات الدولية.", "Initiatives and projects to develop the airspace according to international best practice."),
            ("خدمات بيانات ومعلومات الطيران", "Aviation Data & Information Services", "خدمات-بيانات-ومعلومات-الطيران", "aviation-data-information-services", "bi-database",
                "جمع وتحليل البيانات لدعم اتخاذ القرار.", "Collecting and analysing data to support decision-making."),
        };
        var order = 0;
        foreach (var s in services)
        {
            db.CenterServices.Add(new CenterService
            {
                TitleAr = s.Ar, TitleEn = s.En, SlugAr = s.SlugAr, SlugEn = s.SlugEn, Icon = s.Icon,
                ShortDescriptionAr = s.ShortAr, ShortDescriptionEn = s.ShortEn,
                ContentAr = $"<p>{s.ShortAr}</p><p>{PendingAr}</p>",
                ContentEn = $"<p>{s.ShortEn}</p><p>{PendingEn}</p>",
                DisplayOrder = ++order, Status = ContentStatus.Published,
            });
        }
    }

    private async Task SeedCategoriesAsync(CancellationToken ct)
    {
        if (!await db.NewsCategories.AnyAsync(ct))
        {
            db.NewsCategories.Add(new NewsCategory { NameAr = "أخبار المركز", NameEn = "Center News", Slug = "center-news", DisplayOrder = 1 });
            db.NewsCategories.Add(new NewsCategory { NameAr = "فعاليات", NameEn = "Events", Slug = "events", DisplayOrder = 2 });
            db.NewsCategories.Add(new NewsCategory { NameAr = "التدريب", NameEn = "Training", Slug = "training", DisplayOrder = 3 });
        }
        if (!await db.ReportCategories.AnyAsync(ct))
        {
            db.ReportCategories.Add(new ReportCategory { NameAr = "دراسات فنية", NameEn = "Technical Studies", Slug = "technical-studies", DisplayOrder = 1 });
            db.ReportCategories.Add(new ReportCategory { NameAr = "تقارير دورية", NameEn = "Periodic Reports", Slug = "periodic-reports", DisplayOrder = 2 });
            db.ReportCategories.Add(new ReportCategory { NameAr = "أوراق بحثية", NameEn = "Research Papers", Slug = "research-papers", DisplayOrder = 3 });
        }
    }

    // ------------------------------------------------------------------ organization (explicit placeholders only)

    private async Task SeedOrganizationAsync(CancellationToken ct)
    {
        if (!await db.Leaders.AnyAsync(ct))
        {
            for (var i = 1; i <= 3; i++)
            {
                db.Leaders.Add(new Leader
                {
                    NameAr = $"اسم القيادة ({i}) — يُضاف لاحقًا", NameEn = $"Leader name ({i}) — to be added",
                    PositionAr = "المنصب — يُضاف لاحقًا", PositionEn = "Position — to be added",
                    BiographyAr = $"<p>{PendingAr}</p>", BiographyEn = $"<p>{PendingEn}</p>",
                    DisplayOrder = i, IsActive = true,
                });
            }
        }

        if (!await db.Departments.AnyAsync(ct))
        {
            var root = new Department
            {
                NameAr = "رئاسة المركز", NameEn = "Center Presidency",
                DescriptionAr = PendingAr, DescriptionEn = PendingEn, DisplayOrder = 1, Status = ContentStatus.Published,
            };
            db.Departments.Add(root);
            for (var i = 1; i <= 4; i++)
            {
                var unit = new Department
                {
                    ParentDepartmentId = root.Id,
                    NameAr = $"وحدة تنظيمية ({i}) — مسمى مبدئي", NameEn = $"Organizational unit ({i}) — provisional name",
                    DescriptionAr = PendingAr, DescriptionEn = PendingEn, DisplayOrder = i, Status = ContentStatus.Published,
                };
                db.Departments.Add(unit);
                if (i <= 2)
                {
                    for (var j = 1; j <= 2; j++)
                    {
                        db.Departments.Add(new Department
                        {
                            ParentDepartmentId = unit.Id,
                            NameAr = $"قسم ({i}.{j}) — مسمى مبدئي", NameEn = $"Section ({i}.{j}) — provisional name",
                            DisplayOrder = j, Status = ContentStatus.Published,
                        });
                    }
                }
            }
        }

        if (!await db.LegalDocuments.AnyAsync(ct))
        {
            // Kept as Draft: the official text, file and publication date must be attached before publishing.
            db.LegalDocuments.Add(new LegalDocument
            {
                TitleAr = "القانون رقم ٢٥ لسنة ٢٠٢٢", TitleEn = "Law No. 25 of 2022",
                DescriptionAr = "المرجع القانوني للمركز. يُرفق النص الرسمي ويُراجع تاريخ النشر قبل النشر.",
                DescriptionEn = "The Center's legal reference. Attach the official text and verify the publication date before publishing.",
                Category = LegalDocumentCategory.Law, DocumentNumber = "25/2022",
                PublicationDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Status = ContentStatus.Draft,
            });
        }
    }

    // ------------------------------------------------------------------ helpers

    private static ContentPage Page(string key, string titleAr, string titleEn, string slugAr, string slugEn,
        string summaryAr, string summaryEn, params PageSection[] sections)
    {
        var page = new ContentPage
        {
            SystemKey = key, TitleAr = titleAr, TitleEn = titleEn, SlugAr = slugAr, SlugEn = slugEn,
            SummaryAr = summaryAr, SummaryEn = summaryEn, Status = ContentStatus.Published,
        };
        var order = 0;
        foreach (var s in sections)
        {
            s.DisplayOrder = ++order;
            page.Sections.Add(s);
        }
        return page;
    }

    private static PageSection Section(string? key, PageSectionLayout layout, string titleAr, string titleEn, string? icon, string contentAr, string contentEn) =>
        new()
        {
            Key = key, Layout = layout, TitleAr = titleAr, TitleEn = titleEn, Icon = icon,
            ContentAr = contentAr, ContentEn = contentEn, IsVisible = true,
        };
}
