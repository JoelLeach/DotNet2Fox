@echo off

@echo Removing existing images...
REM May fail if there are any child images. Can prune them later.
powershell -Command docker rmi $(docker images 'joelleach/dotnet2fox-aspnet' -a -q)

rem Server 2022 requires Windows 11 or Server 2022 to build image
@echo ================================================
@echo Building ASP.NET 7.0 Windows Server 2022 image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=7.0-ltsc2022  --build-arg ASPNetVer=7.0.1 --tag joelleach/dotnet2fox-aspnet:7.0-ltsc2022 .

@echo ================================================
@echo Building ASP.NET 7.0 Windows Server 2019 20H2 image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=7.0-20H2 --build-arg ASPNetVer=7.0.1 --tag joelleach/dotnet2fox-aspnet:7.0-20H2 .

@echo ================================================
@echo Building ASP.NET 7.0 Windows Server 2019 1809 (LTSC) image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=7.0-1809 --build-arg ASPNetVer=7.0.1 --tag joelleach/dotnet2fox-aspnet:7.0-1809 .

rem Server 2022 requires Windows 11 or Server 2022 to build image
@echo ================================================
@echo Building ASP.NET 6.0 Windows Server 2022 image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=6.0-ltsc2022  --build-arg ASPNetVer=6.0.12 --tag joelleach/dotnet2fox-aspnet:6.0-ltsc2022 .

@echo ================================================
@echo Building ASP.NET 6.0 Windows Server 2019 20H2 image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=6.0-20H2 --build-arg ASPNetVer=6.0.12 --tag joelleach/dotnet2fox-aspnet:6.0-20H2 .

@echo ================================================
@echo Building ASP.NET 6.0 Windows Server 2019 1809 (LTSC) image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=6.0-1809 --build-arg ASPNetVer=6.0.12 --tag joelleach/dotnet2fox-aspnet:6.0-1809 .

rem Server 2022 requires Windows 11 or Server 2022 to build image
@echo ================================================
@echo Building ASP.NET 5.0 Windows Server 2022 image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=5.0-ltsc2022  --build-arg ASPNetVer=5.0.17 --tag joelleach/dotnet2fox-aspnet:5.0-ltsc2022 .

@echo ================================================
@echo Building ASP.NET 5.0 Windows Server 2019 20H2 image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=5.0-20H2 --build-arg ASPNetVer=5.0.17 --tag joelleach/dotnet2fox-aspnet:5.0-20H2 .

@echo ================================================
@echo Building ASP.NET 5.0 Windows Server 2019 1809 (LTSC) image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=5.0-1809 --build-arg ASPNetVer=5.0.17 --tag joelleach/dotnet2fox-aspnet:5.0-1809 .