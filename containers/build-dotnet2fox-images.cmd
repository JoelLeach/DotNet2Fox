@echo off

@echo Removing existing images...
REM May fail if there are any child images. Can prune them later.
powershell -Command docker rmi $(docker images 'joelleach/dotnet2fox-aspnet' -a -q)
powershell -Command docker rmi $(docker images 'joelleach/dotnet2fox-iis' -a -q)
powershell -Command docker rmi $(docker images 'joelleach/dotnet2fox-core' -a -q)
powershell -Command docker rmi -f $(docker images 'joelleach/dotnet2fox' -a -q)

@echo ------------------------------------------------
@echo Building dotnet2fox images...
@echo ------------------------------------------------
cd dotnet2fox
call buildimages.cmd

@echo ------------------------------------------------
@echo Building dotnet2fox-core images...
@echo ------------------------------------------------
cd ..\dotnet2fox-core
call buildimages.cmd

@echo ------------------------------------------------
@echo Building dotnet2fox-aspnet images...
@echo ------------------------------------------------
cd ..\dotnet2fox-aspnet
call buildimages.cmd

@echo ------------------------------------------------
@echo Building dotnet2fox-iis images...
@echo ------------------------------------------------
cd ..\dotnet2fox-iis
call buildimages.cmd

cd ..
@echo ------------------------------------------------
@echo Building DotNet2Fox images complete.
@echo ------------------------------------------------