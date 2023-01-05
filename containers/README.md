# DotNet2Fox Containers
Pre-built container images for apps that use DotNet2Fox. COM registration and DCOM configuration has already been performed inside the containers. Containers can be built on top of these images, or the Dockerfiles can be used as examples for creating images from scratch.

## Images
- [dotnet2fox](dotnet2fox): The base DotNet2Fox container with support for .NET Framework 4.8.
- [dotnet2fox-core](dotnet2fox-core): Includes support for .NET 5.0 and later. These are tagged as "dotnet2fox" so they'll show up on the same page as .NET Framework 4.8 images on DockerHub.
- [dotnet2fox-aspnet](dotnet2fox-aspnet): For web apps that use ASP.NET Core 5.0 or later with the Kestrel web server.
- [dotnet2fox-iis](dotnet2fox-iis): For web apps that use ASP.NET 4.8 with IIS. These are tagged as "dotnet2fox-aspnet" so they'll show up on the same page as ASP.NET Core images on DockerHub.

## Usage
Generally speaking, you can use a Dockerfile that Visual Studio generates and replace the base image with an applicable DotNet2Fox image. This is demonstrated in the video below.

## Scripts
Batch files and Dockerfile arguments are used to build each container for multiple operating systems and versions of .NET.

## Docker Hub
Images are hosted on [Docker Hub](https://hub.docker.com/u/joelleach).

## Video
A DotNet2Fox container was demonstrated at Virtual Fox Fest 2022 (October):

[DevOps with Visual FoxPro](https://youtu.be/FNFk1CpBzjE)

## FoxConsole
FoxConsole is included with all DotNet2Fox containers. Enter **foxc** at the command prompt inside a container to start.