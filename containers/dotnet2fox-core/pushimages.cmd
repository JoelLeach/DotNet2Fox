@echo off

rem For publishing, use dotnet2fox tag, so they'll show up on the 
rem same page as 4.8 on Docker Hub.

@echo ================================================
@echo Pushing .NET 5.0 Windows Server 2022 image...
@echo ================================================
REM docker tag dotnet2fox-core:5.0-ltsc2022 joelleach/dotnet2fox:5.0-ltsc2022
docker push joelleach/dotnet2fox:5.0-ltsc2022

@echo ================================================
@echo Pushing .NET 5.0 Windows Server 2019 20H2 image...
@echo ================================================
REM docker tag dotnet2fox-core:5.0-20H2 joelleach/dotnet2fox:5.0-20H2
docker push joelleach/dotnet2fox:5.0-20H2

@echo ================================================
@echo Pushing .NET 5.0 Windows Server 2019 1809 (LTSC) image...
@echo ================================================
REM docker tag dotnet2fox-core:5.0-1809 joelleach/dotnet2fox:5.0-1809
docker push joelleach/dotnet2fox:5.0-1809

@echo ================================================
@echo Pushing .NET 6.0 Windows Server 2022 image...
@echo ================================================
REM docker tag dotnet2fox-core:6.0-ltsc2022 joelleach/dotnet2fox:6.0-ltsc2022
docker push joelleach/dotnet2fox:6.0-ltsc2022

@echo ================================================
@echo Pushing .NET 6.0 Windows Server 2019 20H2 image...
@echo ================================================
REM docker tag dotnet2fox-core:6.0-20H2 joelleach/dotnet2fox:6.0-20H2
docker push joelleach/dotnet2fox:6.0-20H2

@echo ================================================
@echo Pushing .NET 6.0 Windows Server 2019 1809 (LTSC) image...
@echo ================================================
REM docker tag dotnet2fox-core:6.0-1809 joelleach/dotnet2fox:6.0-1809
docker push joelleach/dotnet2fox:6.0-1809

@echo ================================================
@echo Pushing .NET 7.0 Windows Server 2022 image...
@echo ================================================
docker push joelleach/dotnet2fox:7.0-ltsc2022

@echo ================================================
@echo Pushing .NET 7.0 Windows Server 2019 20H2 image...
@echo ================================================
docker push joelleach/dotnet2fox:7.0-20H2

@echo ================================================
@echo Pushing .NET 7.0 Windows Server 2019 1809 (LTSC) image...
@echo ================================================
docker push joelleach/dotnet2fox:7.0-1809

