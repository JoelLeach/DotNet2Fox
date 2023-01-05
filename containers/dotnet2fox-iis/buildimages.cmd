@echo off

@echo Removing existing images...
REM May fail if there are any child images. Can prune them later.
powershell -Command docker rmi $(docker images 'joelleach/dotnet2fox-aspnet:4.8*' -a -q)

rem Server 2022 requires Windows 11 or Server 2022 to build image
@echo ================================================
@echo Building ASP.NET 4.8 Windows Server 2022 image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=4.8-ltsc2022  --tag joelleach/dotnet2fox-aspnet:4.8-ltsc2022 .

@echo ================================================
@echo Building ASP.NET 4.8 Windows Server 2019 20H2 image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=4.8-20H2 --tag joelleach/dotnet2fox-aspnet:4.8-20H2 .

@echo ================================================
@echo Building ASP.NET 4.8 Windows Server 2019 1809 (LTSC) image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=4.8-1809 --tag joelleach/dotnet2fox-aspnet:4.8-1809 .