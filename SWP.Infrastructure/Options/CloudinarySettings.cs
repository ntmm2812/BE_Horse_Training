namespace SWP.Infrastructure.Options;

public class CloudinarySettings
{
    public const string SectionName = "Cloudinary";

    public string CloudName { get; set; } = null!;

    public string ApiKey { get; set; } = null!;

    public string ApiSecret { get; set; } = null!;

    public string Folder { get; set; } = "swp-horses";
}
