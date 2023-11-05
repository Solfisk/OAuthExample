# OAuthExample

This project is a complete example (frontend, backend, Docker) of a web application that authenticate users by OAuth2.

* Backend: C#, .NET 7
* Frontend: HTML / vanilla JavaScript
* OAuth IDP in this example: OpenStreetMap
* Hosting: Docker 

As it is an example only, this project is not production ready. But it should provide you with a starting point.

## Prerequisites

The description assumes that you are fluent in C#, and have a basic understanding of javascript and Docker.

Also, this example uses OpenStreetMap as identity provider. It assumes that you already have an OpenStreetMap user. Otherwise, you can create one, or use another identity provider of your choice.

You should have `git` and `docker` installed on your system, and a suitable editor.

## How to run 

### SSL Certificate
Before first run, you must create a directory with the SSL certificate to use for you app. For development, you can use the default .NET developer certificate. Export it like this:

```bash
mkdir certs
dotnet dev-certs https -ep cert/localhost.pfx -p ez2guess
```

### OAuth configuration

You also need to register your application at the OAuth server. For OpenStreetMap, you can do that at [https://www.openstreetmap.org/oauth2/applications/new](https://www.openstreetmap.org/oauth2/applications/new)

Give your application a name. You also need to add an allowed callback URL. This is where your application runs. For this example, use `https://localhost:7074/api/oauth/callback`

Check the "Confidential application?" checkbox. and the "Read user preferences" permission.

When you click "Register", you will see a new page with a client ID and a client secret. You need to copy both of these at this stage - the secret will not be visible later on.

Create a new file called `.env` with your own client ID and secret, e.g.
```bash
OAuth__ClientId=wk8LAlseN32ZsTQrLVz7IAymJDX3R7CF9ypGp8EaU4a
OAuth__ClientSecret=k3W2pgs454a4t9ck8rcdez0iXyIlQN2zmIACLWMWMw8 
```


### First run

You can now build and start your application in docker:
```bash
docker-compose build
docker-compose up
```

Try it out at https://localhost:7074/

## Basic Principle

When a user first visits the example page, he has no cookie set.

So he is redirected to the OSM authorisation server.
There, he enters his credentials, and the OSM server issues an *access token* with a set of *claims*, such as user ID. The token can also be used to gain access to certain services on behalf of the user.

The user is now redirected back to this project's server.

Our backend then makes a request to the user info endpoint of OSM's API using the token it just got. The response from OSM's API contains further details, e.g. user name and picture URL.

The access token and the user information is then combined in an authentication token that is saved in a cookie.

That cookie is used on subsequent requests to the backend.


## Details

### Frontend

The frontend in this example is made as simple as possible.

The `index.html` simply assumes that you are not logged in, and displays a login button. The login button *redirects* to the backend's `login` endpoint.

After login, the user is again *redirected* to `loggedin.html`. At that point, the authentication cookie should be set with all the information we need. But as a measure agains XSS, it is set as a HTTP-only cookie. So we cannot read it from JavaScript. Instead, we call the backend's `userinfo` endpoint to get the relevant data as JSON.  


### Backend

The backend utilises several frameworks that comes with .NET Core, as well as a few other frameworks

* `Microsoft.AspNetCore.Authentication.Cookies` and `Microsoft.AspNetCore.Authentication.OAuth` for the authentication parts.
* `Swashbuckle.AspNetCore` for API documentation. Not necessary for the example, but always nice to have.
* `Microsoft.CodeAnalysis.NetAnalyzers` for code analysis. Again, not necessary for the example, but you should always use code analysis. It makes you a better programmer.

#### Program.cs

This is where all the magic happens, as this is where all the frameworks are configured and activated.

First of all, we read the configuration in using the [options pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/options).
Then, we set up controllers and Swagger.

We now set up Authentication to authenticate using cookies. We also set a default challenge name. This can be any name we choose - in this example we use "`OpenStreetMap`".

After that, we add an OAuth handler for that name. We configure it with our client id, secret and other information (see configuration below). We adds the *scope* `read_prefs` which is the permission we need to reed user info from OSM. Other identity providers use other scope names.

The OAuth handler is also configured to retrieve user info from OSM upon login. But we need to tell the framework how to add infomration from the user information as claims. That is done by configuring `ClaimActions`.
Please note that in this example, we abuse the standard `Uri` claim to hold the user image from OSM.

Finally, we configure an event so that after unsuccessful login, we use the user's token to call the user information endpoint.



#### OAuthController.cs
This is a simple controller that just has an endpoint with an `[Authorize]` decorator. Because we have setup OAuth authentication, any endpoint with the `[Authorize]` decorator will check whether you have a valid bearer token in a cookie. If not, it will commence the login flow by redirecting to the OAuth authorisation server.

In this case, the `login` and `logout` endpoints redirects to a return URL that they get as an argument. In order to avoid cross site scriptng issues, we make sure never to redirect to a URL that we do not trust.

The `logout` endpoint removes the auth cookie for your local site. So your app no longer has access or refresh tokens.

But the cookie for the OAuth server persists. So if the user tries to access your app again, he will be redirected to the OAuth server. The OAuth server will look at the cookie, that it has set itself. And if that is still valid, it will accept that the user is still logged in and redirect back with a new access token.

In order to log the user out of the authorisation server as well, you need to user a protocol that supports single logout, e.g. SAML or OpenConnectId.

### Docker

#### Dockerfile

Docker is configured in `Dockerfile`. It consists of two parts.

First, one image is used to build the app. This image is based on dotnet/sdk which offers a full .NET SDK.

Then, another image is created based on dotnet/aspnet which only offers the .NET runtime.

A non-root user is created in that image, and the files build by the build images is copied to the runtime image.

The app is run as the non-root user.

#### docker-compose.yml

Because the app is run with HTTPS, it need a trusted SSL certificate. We do not want to bake that into the image itself, so we need to pass it to the image by mounting a volume.

We use a docker-compose file to make it easy to spin up a container with everything in place. You could also run the container with plain `docker`, but that would need a decent amount of command line arguments.