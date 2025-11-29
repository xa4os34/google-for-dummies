FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build 
ADD crawler /crawler
ADD share /share
RUN dotnet publish crawler -o out;

FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN mkdir -p /app;
COPY --from=build /out/. /app/
WORKDIR /app

ENTRYPOINT [ "dotnet", "crawler.dll" ]

