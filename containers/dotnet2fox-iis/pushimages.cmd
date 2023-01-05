@echo off

rem Publishing with dotnet2fox-aspnet tag, so they'll show up on same page
rem as ASP.NET Core in Docker Hub.

@echo ================================================
@echo Pushing ASP.NET 4.8 Windows Server 2022 image...
@echo ================================================
REM docker tag dotnet2fox-iis:4.8-ltsc2022 joelleach/dotnet2fox-aspnet:4.8-ltsc2022
docker push joelleach/dotnet2fox-aspnet:4.8-ltsc2022

@echo ================================================
@echo Pushing ASP.NET 4.8 Windows Server 2019 20H2 image...
@echo ================================================
REM docker tag dotnet2fox-iis:4.8-20H2 joelleach/dotnet2fox-aspnet:4.8-20H2
docker push joelleach/dotnet2fox-aspnet:4.8-20H2

@echo ================================================
@echo Pushing ASP.NET 4.8 Windows Server 2019 1809 (LTSC) image...
@echo ================================================
REM docker tag dotnet2fox-iis:4.8-1809 joelleach/dotnet2fox-aspnet:4.8-1809
docker push joelleach/dotnet2fox-aspnet:4.8-1809