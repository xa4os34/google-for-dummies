FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY indexer/app ./
COPY share /share

RUN dotnet restore
RUN dotnet publish -o out
RUN rm -rf src

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/out/ ./

ENTRYPOINT ["dotnet", "indexer.dll"]
