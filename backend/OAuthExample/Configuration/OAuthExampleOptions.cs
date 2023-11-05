namespace OAuthExample.Configuration;

public class OAuthExampleOptions
{
    public const string SectionName = "OAuthExample";

    /// <summary>
    /// List of return URLs accepted
    /// </summary>
    public IEnumerable<string> AllowedReturnUrls { get; set; } = Array.Empty<string>();
}