FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /

COPY . ./

WORKDIR /backend/OAuthExample

RUN dotnet restore --use-current-runtime

RUN dotnet publish -c Release --use-current-runtime --self-contained false --no-restore  -o out


FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /backend/OAuthExample


# Create non-root user  
RUN adduser --disabled-password --disabled-login --gecos "" oauthex


COPY --from=build /backend/OAuthExample/out .
COPY --from=build /frontend /frontend

USER oauthex

ENTRYPOINT ["dotnet", "OAuthExample.dll"]
