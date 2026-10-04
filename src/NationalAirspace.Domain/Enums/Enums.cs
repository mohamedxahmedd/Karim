namespace NationalAirspace.Domain.Enums;

public enum ContentStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2,
}

public enum MediaKind
{
    Image = 0,
    Document = 1,
}

public enum RegistrationStatus
{
    ComingSoon = 0,
    Open = 1,
    Closed = 2,
    Full = 3,
}

public enum LegalDocumentCategory
{
    Law = 0,
    Regulation = 1,
    Decision = 2,
    Policy = 3,
    OfficialDocument = 4,
}

public enum MenuLocation
{
    Header = 0,
    Footer = 1,
}

public enum PageSectionLayout
{
    /// <summary>Heading + rich text.</summary>
    Text = 0,
    /// <summary>Rendered as one card in a grid of cards.</summary>
    Card = 1,
    /// <summary>Rendered as one step of a horizontal/vertical process diagram.</summary>
    ProcessStep = 2,
    /// <summary>Emphasised call-out block.</summary>
    Highlight = 3,
}

public enum HomeSectionKey
{
    About = 0,
    VisionMission = 1,
    Responsibilities = 2,
    Services = 3,
    Atfm = 4,
    LatestNews = 5,
    LatestReports = 6,
    Training = 7,
    Statistics = 8,
    ImportantLinks = 9,
    Contact = 10,
}

public enum ContactMessageStatus
{
    New = 0,
    Read = 1,
    Replied = 2,
}

public enum VideoProvider
{
    YouTube = 0,
    Vimeo = 1,
    External = 2,
}
