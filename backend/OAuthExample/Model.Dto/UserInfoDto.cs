namespace OAuthService.Model.Dto;

public class UserInfoDto
{
    public UserInfoDto(string id, string userName, string? imageUrl = null)
    {
        Id = id;
        UserName = userName;
        ImageUrl = imageUrl;
    }

    public string Id { get; set; }
    
    public string UserName { get; set; }

    public string? ImageUrl { get; set; }
}