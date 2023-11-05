using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using OAuthExample.Configuration;

namespace OAuthService
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            // Read configuration
            var oauthConfig = builder.Configuration.GetSection(OAuth2Options.SectionName).Get<OAuth2Options>() ?? new OAuth2Options();
            builder.Services.Configure<OAuthExampleOptions>(builder.Configuration.GetSection(OAuthExampleOptions.SectionName));

            // Configure Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Setup authentication - first cookie, then OAuth
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = oauthConfig.ChallengeSchemaName;
            })
            .AddCookie()
            .AddOAuth(oauthConfig.ChallengeSchemaName, options =>
            {
                options.ClientId = oauthConfig.ClientId;
                options.ClientSecret = oauthConfig.ClientSecret;
                options.SaveTokens = true;
                options.CallbackPath = new PathString("/api/oauth/callback"); // Could be anything
                options.AuthorizationEndpoint = oauthConfig.AuthorizationEndpoint;
                options.TokenEndpoint = oauthConfig.TokenEndpoint;
                options.Scope.Add("read_prefs"); // The OSM permission we need to read user information
                options.UserInformationEndpoint = oauthConfig.UserInformationEndpoint;

                // Setup map of OSM user info JSON to claims
                options.ClaimActions.MapJsonSubKey(ClaimTypes.NameIdentifier, "user", "id");
                options.ClaimActions.MapJsonSubKey(ClaimTypes.Name, "user", "display_name");
                options.ClaimActions.MapCustomJson(ClaimTypes.Uri, j => {
                    try {
                        return  j.GetProperty("user").GetProperty("img").GetProperty("href").GetRawText();
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                });

                // On login, automatically make a request (with the user's token) to OpenStreetMap's
                // user information endpoint. Then, add the relevant user info to the claims
                options.Events = new OAuthEvents
                   {
                       OnCreatingTicket = async context =>
                       {
                           var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                           request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                           request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                           var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                           response.EnsureSuccessStatusCode();
                           var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                           context.RunClaimActions(json.RootElement);
                       }
                   };
            });

            // Add controllers _after_ auth has been set up.
            builder.Services.AddControllers();



            
            // Dependency Injection (services) has been set up. Now build the app
            var app = builder.Build();


            // Set exception handler.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler();
            }


            // Enable Swagger
            app.UseSwagger();
            app.UseSwaggerUI();

            // Serve the frontend as static pages.
            // We need to use FileServer because the files are located
            // outside the backend's home directory
            var fileServerOptions = new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(
                   Path.Combine(builder.Environment.ContentRootPath, "../../frontend")),
                   RequestPath = "",
                   RedirectToAppendTrailingSlash = true,                
            };
            fileServerOptions.DefaultFilesOptions.DefaultFileNames.Clear();
            fileServerOptions.DefaultFilesOptions.DefaultFileNames.Add("index.html");
            app.UseFileServer(fileServerOptions);




            app.UseRouting();

            app.UseHttpsRedirection();
            app.UseHsts();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}