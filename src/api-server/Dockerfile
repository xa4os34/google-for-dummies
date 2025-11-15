FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["api-server/Api-server.csproj", "api-server/"]
COPY ["share/share.csproj", "share/"]
RUN dotnet restore "api-server/Api-server.csproj"
COPY api-server/ api-server/
COPY share/ share/
WORKDIR "/src/api-server"
RUN dotnet build "Api-server.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR "/src/api-server"
RUN dotnet publish "Api-server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api-server.dll"]

