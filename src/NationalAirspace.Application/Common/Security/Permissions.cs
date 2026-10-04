namespace NationalAirspace.Application.Common.Security;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Administrator = "Administrator";
    public const string ContentEditor = "ContentEditor";
    public const string CommunicationsOfficer = "CommunicationsOfficer";

    public static readonly IReadOnlyList<string> All = [SuperAdmin, Administrator, ContentEditor, CommunicationsOfficer];
}

/// <summary>
/// Permission names double as authorization policy names. Each admin controller is protected by one.
/// </summary>
public static class Permissions
{
    public const string Dashboard = "perm:dashboard";
    public const string News = "perm:news";
    public const string Announcements = "perm:announcements";
    public const string Services = "perm:services";
    public const string Reports = "perm:reports";
    public const string Training = "perm:training";
    public const string Media = "perm:media";
    public const string Leadership = "perm:leadership";
    public const string Departments = "perm:departments";
    public const string Legislation = "perm:legislation";
    public const string Pages = "perm:pages";
    public const string Homepage = "perm:homepage";
    public const string Menus = "perm:menus";
    public const string Messages = "perm:messages";
    public const string Settings = "perm:settings";
    public const string Users = "perm:users";
    public const string AuditLog = "perm:audit";

    private static readonly string[] Content =
        [Dashboard, News, Announcements, Services, Reports, Training, Media, Leadership, Departments, Legislation, Pages, Homepage];

    public static readonly IReadOnlyList<string> All =
        [.. Content, Menus, Messages, Settings, Users, AuditLog];

    /// <summary>Static role → permission matrix.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ByRole = new Dictionary<string, IReadOnlyList<string>>
    {
        [Roles.SuperAdmin] = All,
        [Roles.Administrator] = All.Where(p => p != Users).ToArray(),
        [Roles.ContentEditor] = Content,
        [Roles.CommunicationsOfficer] = [Dashboard, Messages, News, Announcements, Media],
    };

    public static IEnumerable<string> RolesGranting(string permission) =>
        ByRole.Where(kv => kv.Value.Contains(permission)).Select(kv => kv.Key);
}
