FROM mcr.microsoft.com/dotnet/framework/sdk:4.7.2 AS build
WORKDIR /app

COPY *.sln .
COPY *.csproj ./
COPY *.config ./
RUN nuget restore

COPY . ./
RUN msbuild /p:Configuration=Release

# 14.2GB
# FROM mcr.microsoft.com/dotnet/framework/aspnet:4.7.2 AS runtime
# 7.32GB
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.7.2-windowsservercore-ltsc2019 AS runtime
# this one doesn't work - 500 - internal server error (200723)
# FROM mcr.microsoft.com/windows/servercore/iis:windowsservercore-ltsc2019 AS runtime 
WORKDIR /inetpub/wwwroot
COPY --from=build /app/. ./ 
# ref: https://www.saotn.org/install-web-websockets-feature-iis-using-powershell/ (200731)
# RUN powershell -Command Add-WindowsFeature Web-WebSockets
RUN powershell -Command Install-WindowsFeature -Name Web-WebSockets
# SHELL ["powershell", "-Command", "Add-WindowsFeature", "Web-WebSockets"]

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

RUN $newCert=New-SelfSignedCertificate -DnsName 'localhost' -CertStoreLocation cert:\LocalMachine\My; \
  New-WebBinding -Name 'Default Web Site' -Protocol 'https'; \
  $binding=Get-WebBinding -Name 'Default Web Site' -Protocol 'https'; \
  $binding.AddSslCertificate($newCert.GetCertHashString(),'my')
