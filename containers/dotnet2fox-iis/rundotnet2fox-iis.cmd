@echo off 
rem Access on localhost:3000

docker run -it -p 3000:80 --name dotnet2fox-iis --hostname dotnet2fox-iis joelleach/dotnet2fox-aspnet:4.8-1809