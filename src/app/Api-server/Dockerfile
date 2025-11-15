FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["app/Api-server/Api-server.csproj", "app/Api-server/"]
RUN dotnet restore "app/Api-server/Api-server.csproj"
COPY app/Api-server/ app/Api-server/
WORKDIR "/src/app/Api-server"
RUN dotnet build "Api-server.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR "/src/app/Api-server"
RUN dotnet publish "Api-server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api-server.dll"]

