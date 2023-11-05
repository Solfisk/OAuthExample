namespace OAuthExample.Configuration;

public class OAuth2Options
{
    public const string SectionName = "OAuth";

    /// <summary>
    /// Client ID as registered at https://www.openstreetmap.org/oauth2/applications/new
    /// Must be set in appsettings.json, enironment variable, or command line
    /// </summary>
    public string ClientId { get; set; } = "You must set client ID in configuration";

    /// <summary>
    /// Client secret that you got when you registered
    /// Must be set in appsettings.json, enironment variable, or command line
    /// </summary>    
    public string ClientSecret { get; set; } = "You must set client secret in configuration";

    /// <summary>
    /// OAuth Authorization endpont. This is the default for OpenStreetMap, so no need to change it in config.
    /// <summary>
    public string AuthorizationEndpoint { get; set; } = "https://www.openstreetmap.org/oauth2/authorize";

    /// <summary>
    /// OAuth Token endpont. This is the default for OpenStreetMap, so no need to change it in config.
    /// <summary>
    public string TokenEndpoint { get; set; } = "https://www.openstreetmap.org/oauth2/token";

    /// <summary>
    /// User information endpont. This is the default for OpenStreetMap, so no need to change it in config.
    /// <summary>
    public string UserInformationEndpoint { get; set; } = "https://api.openstreetmap.org/api/0.6/user/details.json";

    
    /// <summary>
    /// Just an internal identifier, in case you have multiple authorisation providers
    /// <summary>
    public string ChallengeSchemaName { get; set; } = "OpenStreetMap";
}