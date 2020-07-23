FROM mcr.microsoft.com/dotnet/framework/sdk:4.7.2 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .
COPY *.csproj ./
COPY *.config ./
RUN nuget restore

# copy everything else and build app
COPY . ./
# WORKDIR /app
RUN msbuild /p:Configuration=Release

# 14.2GB
# FROM mcr.microsoft.com/dotnet/framework/aspnet:4.7.2 AS runtime
# 7.32GB
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.7.2-windowsservercore-ltsc2019 AS runtime
# this one doesn't work - 500 - internal server error (200723)
# FROM mcr.microsoft.com/windows/servercore/iis:windowsservercore-ltsc2019 AS runtime 
WORKDIR /inetpub/wwwroot
COPY --from=build /app/. ./ 
RUN dir
