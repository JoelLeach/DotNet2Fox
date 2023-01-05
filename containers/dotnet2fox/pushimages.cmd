@echo off

@echo ================================================
@echo Pushing Windows Server 2022 image...
@echo ================================================
REM docker tag dotnet2fox:4.8-ltsc2022 joelleach/dotnet2fox:4.8-ltsc2022
docker push joelleach/dotnet2fox:4.8-ltsc2022

@echo ================================================
@echo Pushing Windows Server 2019 20H2 image...
@echo ================================================
REM docker tag dotnet2fox:4.8-20H2 joelleach/dotnet2fox:4.8-20H2
docker push joelleach/dotnet2fox:4.8-20H2

@echo ================================================
@echo Pushing Windows Server 2019 1809 (LTSC) image...
@echo ================================================
REM docker tag dotnet2fox:4.8-1809 joelleach/dotnet2fox:4.8-1809
docker push joelleach/dotnet2fox:4.8-1809