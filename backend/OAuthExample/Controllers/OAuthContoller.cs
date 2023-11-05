using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuthService.Model.Dto;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using OAuthExample.Configuration;

namespace OAuthExample.Controllers
{
    [ApiController]
    [Route("api/oauth")]
    public class OAuthController : ControllerBase
    {
        private HashSet<string> _allowedReturnUrls;

        public OAuthController(IOptions<OAuthExampleOptions> config)
        {
            _allowedReturnUrls = config.Value.AllowedReturnUrls.ToHashSet();
        }

        /// <summary>
        /// Login using OAuth
        /// </summary>
        /// <remarks>
        /// This is simply an endpoint that redirects.
        /// But because it has the <c>Authorize</c> attribute, the OAuth middleware will
        /// check whether the caller is logged in. If not, it will start a login flow with the OAuth server first.
        /// </remarks>
        /// <param name="returnUrl">The url to redirect to after login. Must be whitelisted in configuration.</param>
        [HttpGet("login")]
        [Authorize]
        public IActionResult Login(string returnUrl = "/")
        {
            return Redirect(CheckReturnUrl(returnUrl));
        }

        /// <summary>
        /// Logout by removing the local session cookie.
        /// But the cookie set by the OAuth server persists, so it will
        /// automatically log you in again without prompting for credentials.
        /// </summary>
        /// <param name="returnUrl">The url to redirect to after logout. Must be whitelisted in configuration.</param>
        [HttpGet("logout")]
        public IActionResult Logout(string returnUrl = "/")
        {
            return SignOut(
                new AuthenticationProperties { RedirectUri = CheckReturnUrl(returnUrl) }
            );
        }

        /// <summary>
        /// Get user information from the Bearer token.
        /// The browser already has the JWT token, but it is stored in a httpOnly cookie - so
        /// JavaScript should not be able to read it. That is a measure against cross site scripting.
        /// </summary>
        [HttpGet("userinfo")]
        [Authorize]
        public UserInfoDto? UserInfo()
        {
            return new UserInfoDto(GetClaim(ClaimTypes.NameIdentifier), GetClaim(ClaimTypes.Name), GetClaim(ClaimTypes.Uri));
        }

        /// <summary>
        /// Test endpoint for verifying connection. Note that this endpoint does not require login.
        /// </summary>
        /// <returns>The string "pong"</returns>
        [HttpGet("ping")]
        [AllowAnonymous]
        public string Ping() => "pong";

        private string GetClaim(string claimType)
        {
            var claims = this.User?.Claims;
            if (claims is null)
            {
                return "";
            }

            return claims.FirstOrDefault(c => c.Type == claimType)?.Value ?? "";
        }

        private string CheckReturnUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url) || !_allowedReturnUrls.Contains(url))
            {
                return "/";
            }

            return url;
        }
    }
}
