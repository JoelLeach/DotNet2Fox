@echo off

@echo Removing existing images...
REM May fail if there are any child images. Can prune them later.
powershell -Command docker rmi $(docker images 'joelleach/dotnet2fox:7.0*' -a -q)
powershell -Command docker rmi $(docker images 'joelleach/dotnet2fox:6.0*' -a -q)
powershell -Command docker rmi $(docker images 'joelleach/dotnet2fox:5.0*' -a -q)

rem Server 2022 requires Windows 11 or Server 2022 to build image
@echo ================================================
@echo Building .NET 7.0 Windows Server 2022 image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=4.8-ltsc2022  --build-arg NetVer=7.0.1 --tag joelleach/dotnet2fox:7.0-ltsc2022 .

@echo ================================================
@echo Building .NET 7.0 Windows Server 2019 20H2 image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=4.8-20H2 --build-arg NetVer=7.0.1 --tag joelleach/dotnet2fox:7.0-20H2 .

@echo ================================================
@echo Building .NET 7.0 Windows Server 2019 1809 (LTSC) image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=4.8-1809 --build-arg NetVer=7.0.1 --tag joelleach/dotnet2fox:7.0-1809 .

rem Server 2022 requires Windows 11 or Server 2022 to build image
@echo ================================================
@echo Building .NET 6.0 Windows Server 2022 image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=4.8-ltsc2022  --build-arg NetVer=6.0.12 --tag joelleach/dotnet2fox:6.0-ltsc2022 .

@echo ================================================
@echo Building .NET 6.0 Windows Server 2019 20H2 image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=4.8-20H2 --build-arg NetVer=6.0.12 --tag joelleach/dotnet2fox:6.0-20H2 .

@echo ================================================
@echo Building .NET 6.0 Windows Server 2019 1809 (LTSC) image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=4.8-1809 --build-arg NetVer=6.0.12 --tag joelleach/dotnet2fox:6.0-1809 .

rem Server 2022 requires Windows 11 or Server 2022 to build image
@echo ================================================
@echo Building .NET 5.0 Windows Server 2022 image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=4.8-ltsc2022  --build-arg NetVer=5.0.17 --tag joelleach/dotnet2fox:5.0-ltsc2022 .

@echo ================================================
@echo Building .NET 5.0 Windows Server 2019 20H2 image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=4.8-20H2 --build-arg NetVer=5.0.17 --tag joelleach/dotnet2fox:5.0-20H2 .

@echo ================================================
@echo Building .NET 5.0 Windows Server 2019 1809 (LTSC) image...
@echo ================================================
docker image build --build-arg DotNet2FoxTag=4.8-1809 --build-arg NetVer=5.0.17 --tag joelleach/dotnet2fox:5.0-1809 .