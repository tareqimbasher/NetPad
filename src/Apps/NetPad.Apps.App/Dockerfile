FROM node:20-alpine3.19 AS frontend-dependencies
WORKDIR /src
COPY ["Apps/NetPad.Apps.App/App/package.json", "Apps/NetPad.Apps.App/App/"]
COPY ["Apps/NetPad.Apps.App/App/package-lock.json", "Apps/NetPad.Apps.App/App/"]
COPY ["Apps/NetPad.Apps.App/App/.npmrc", "Apps/NetPad.Apps.App/App/"]
WORKDIR /src/Apps/NetPad.Apps.App/App/
RUN npm install

FROM frontend-dependencies AS frontend-resources
WORKDIR /src
COPY [".", "."]
WORKDIR /src/Apps/NetPad.Apps.App/App/
RUN npm run build --env target=web

FROM alpine:3.20 AS base
RUN apk add dotnet8-sdk dotnet6-sdk
RUN dotnet tool install --global dotnet-ef
WORKDIR /app
EXPOSE 57930

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src
COPY ["Apps/NetPad.Apps.App/NetPad.Apps.App.csproj", "Apps/NetPad.Apps.App/"]
COPY ["Core/NetPad.Runtime/NetPad.Runtime.csproj", "Core/NetPad.Runtime/"]
COPY ["External/O2Html/O2Html.csproj", "External/O2Html/"]
COPY ["Apps/NetPad.Apps.Shells.Electron/NetPad.Apps.Shells.Electron.csproj", "Apps/NetPad.Apps.Shells.Electron/"]
COPY ["Apps/NetPad.Apps.Common/NetPad.Apps.Common.csproj", "Apps/NetPad.Apps.Common/"]
COPY ["Apps/NetPad.Apps.Shells.Web/NetPad.Apps.Shells.Web.csproj", "Apps/NetPad.Apps.Shells.Web/"]
COPY ["Plugins/NetPad.Plugins.OmniSharp/NetPad.Plugins.OmniSharp.csproj", "Plugins/NetPad.Plugins.OmniSharp/"]
COPY ["External/OmniSharp.NET/OmniSharp.NET.csproj", "External/OmniSharp.NET/"]
COPY ["Directory.Build.props", "."]
RUN dotnet restore "Apps/NetPad.Apps.App/NetPad.Apps.App.csproj"
COPY . .
WORKDIR "/src/Apps/NetPad.Apps.App"
RUN dotnet build "NetPad.Apps.App.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG TARGET=web
ENV WebBuild=false
ENV BuildServerSideRenderer=false
COPY --from=frontend-resources "/src/Apps/NetPad.Apps.App/App/dist" "Apps/NetPad.Apps.App/App/"
RUN dotnet publish "NetPad.Apps.App.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NetPad.Apps.App.dll"]
