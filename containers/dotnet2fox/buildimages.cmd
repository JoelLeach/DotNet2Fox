@echo off

@echo Removing existing images...
REM May fail if there are any child images. Can prune them later.
powershell -Command docker rmi -f $(docker images 'joelleach/dotnet2fox' -a -q)

rem Server 2022 requires Windows 11 or Server 2022 to build image
@echo ================================================
@echo Building Windows Server 2022 image...
@echo ================================================
docker image build --build-arg VFPTag=ltsc2022 --tag joelleach/dotnet2fox:4.8-ltsc2022 .

@echo ================================================
@echo Building Windows Server 2019 20H2 image...
@echo ================================================
docker image build --build-arg VFPTag=20H2 --tag joelleach/dotnet2fox:4.8-20H2 .

@echo ================================================
@echo Building Windows Server 2019 1809 (LTSC) image...
@echo ================================================
docker image build --build-arg VFPTag=1809 --tag joelleach/dotnet2fox:4.8-1809 .