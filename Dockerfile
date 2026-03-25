FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AgendaPositiva.Web/AgendaPositiva.Web.csproj AgendaPositiva.Web/
RUN dotnet restore AgendaPositiva.Web/AgendaPositiva.Web.csproj

COPY AgendaPositiva.Web/ AgendaPositiva.Web/
RUN dotnet publish AgendaPositiva.Web/AgendaPositiva.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

EXPOSE 8080
ENTRYPOINT ["dotnet", "AgendaPositiva.Web.dll"]
