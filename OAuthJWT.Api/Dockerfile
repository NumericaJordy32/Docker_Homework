FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["OAuthJWT.Api.csproj", "./"]
RUN dotnet restore "OAuthJWT.Api.csproj"
COPY . .
RUN dotnet publish "OAuthJWT.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "OAuthJWT.Api.dll"]
