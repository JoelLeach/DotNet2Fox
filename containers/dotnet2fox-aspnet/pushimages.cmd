@echo off

@echo ================================================
@echo Pushing ASP.NET 5.0 Windows Server 2022 image...
@echo ================================================
REM docker tag dotnet2fox-aspnet:5.0-ltsc2022 joelleach/dotnet2fox-aspnet:5.0-ltsc2022
docker push joelleach/dotnet2fox-aspnet:5.0-ltsc2022

@echo ================================================
@echo Pushing ASP.NET 5.0 Windows Server 2019 20H2 image...
@echo ================================================
REM docker tag dotnet2fox-aspnet:5.0-20H2 joelleach/dotnet2fox-aspnet:5.0-20H2
docker push joelleach/dotnet2fox-aspnet:5.0-20H2

@echo ================================================
@echo Pushing ASP.NET 5.0 Windows Server 2019 1809 (LTSC) image...
@echo ================================================
REM docker tag dotnet2fox-aspnet:5.0-1809 joelleach/dotnet2fox-aspnet:5.0-1809
docker push joelleach/dotnet2fox-aspnet:5.0-1809

@echo ================================================
@echo Pushing ASP.NET 6.0 Windows Server 2022 image...
@echo ================================================
docker push joelleach/dotnet2fox-aspnet:6.0-ltsc2022

@echo ================================================
@echo Pushing ASP.NET 6.0 Windows Server 2019 20H2 image...
@echo ================================================
REM docker tag dotnet2fox-aspnet:6.0-20H2 joelleach/dotnet2fox-aspnet:6.0-20H2
docker push joelleach/dotnet2fox-aspnet:6.0-20H2

@echo ================================================
@echo Pushing ASP.NET 6.0 Windows Server 2019 1809 (LTSC) image...
@echo ================================================
REM docker tag dotnet2fox-aspnet:6.0-1809 joelleach/dotnet2fox-aspnet:6.0-1809
docker push joelleach/dotnet2fox-aspnet:6.0-1809

@echo ================================================
@echo Pushing ASP.NET 7.0 Windows Server 2022 image...
@echo ================================================
docker push joelleach/dotnet2fox-aspnet:7.0-ltsc2022

@echo ================================================
@echo Pushing ASP.NET 7.0 Windows Server 2019 20H2 image...
@echo ================================================
docker push joelleach/dotnet2fox-aspnet:7.0-20H2

@echo ================================================
@echo Pushing ASP.NET 7.0 Windows Server 2019 1809 (LTSC) image...
@echo ================================================
docker push joelleach/dotnet2fox-aspnet:7.0-1809