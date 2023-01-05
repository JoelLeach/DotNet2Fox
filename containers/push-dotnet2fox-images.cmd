@echo off

@echo ------------------------------------------------
@echo Pushing dotnet2fox images...
@echo ------------------------------------------------
cd dotnet2fox
call pushimages.cmd

@echo ------------------------------------------------
@echo Pushing dotnet2fox-core images...
@echo ------------------------------------------------
cd ..\dotnet2fox-core
call pushimages.cmd

@echo ------------------------------------------------
@echo Pushing dotnet2fox-iis images...
@echo ------------------------------------------------
cd ..\dotnet2fox-iis
call pushimages.cmd

@echo ------------------------------------------------
@echo Pushing dotnet2fox-aspnet images...
@echo ------------------------------------------------
cd ..\dotnet2fox-aspnet
call pushimages.cmd

cd ..
@echo ------------------------------------------------
@echo Pushing DotNet2Fox images complete.
@echo ------------------------------------------------