using Microsoft.Extensions.DependencyInjection;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Announcements;
using NationalAirspace.Application.Features.Audit;
using NationalAirspace.Application.Features.Contact;
using NationalAirspace.Application.Features.Dashboard;
using NationalAirspace.Application.Features.DemoAirspace;
using NationalAirspace.Application.Features.Departments;
using NationalAirspace.Application.Features.Home;
using NationalAirspace.Application.Features.Leadership;
using NationalAirspace.Application.Features.Legislation;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Application.Features.MediaCenter;
using NationalAirspace.Application.Features.Menus;
using NationalAirspace.Application.Features.News;
using NationalAirspace.Application.Features.Pages;
using NationalAirspace.Application.Features.Reports;
using NationalAirspace.Application.Features.Search;
using NationalAirspace.Application.Features.Services;
using NationalAirspace.Application.Features.Settings;
using NationalAirspace.Application.Features.Training;

namespace NationalAirspace.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<ContentCache>();
        services.AddScoped<AdminServiceDependencies>();

        // Admin (write) services
        services.AddScoped<MediaService>();
        services.AddScoped<NewsAdminService>();
        services.AddScoped<NewsCategoryAdminService>();
        services.AddScoped<AnnouncementAdminService>();
        services.AddScoped<ServiceAdminService>();
        services.AddScoped<ReportAdminService>();
        services.AddScoped<ReportCategoryAdminService>();
        services.AddScoped<TrainingAdminService>();
        services.AddScoped<LegislationAdminService>();
        services.AddScoped<AlbumAdminService>();
        services.AddScoped<VideoAdminService>();
        services.AddScoped<LeaderAdminService>();
        services.AddScoped<DepartmentAdminService>();
        services.AddScoped<PageAdminService>();
        services.AddScoped<HomeAdminService>();
        services.AddScoped<MenuService>();
        services.AddScoped<SettingsService>();
        services.AddScoped<ContactAdminService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<AuditQueryService>();

        // Public (read) services
        services.AddScoped<NewsPublicService>();
        services.AddScoped<AnnouncementPublicService>();
        services.AddScoped<ServicePublicService>();
        services.AddScoped<ReportPublicService>();
        services.AddScoped<TrainingPublicService>();
        services.AddScoped<LegislationPublicService>();
        services.AddScoped<AlbumPublicService>();
        services.AddScoped<VideoPublicService>();
        services.AddScoped<LeaderPublicService>();
        services.AddScoped<DepartmentPublicService>();
        services.AddScoped<PagePublicService>();
        services.AddScoped<HomePublicService>();
        services.AddScoped<ContactService>();
        services.AddScoped<SiteSearchService>();
        services.AddScoped<DemoAirspaceService>();
        return services;
    }
}
