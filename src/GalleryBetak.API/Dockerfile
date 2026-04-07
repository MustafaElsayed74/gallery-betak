# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/GalleryBetak.API/GalleryBetak.API.csproj", "src/GalleryBetak.API/"]
COPY ["src/GalleryBetak.Application/GalleryBetak.Application.csproj", "src/GalleryBetak.Application/"]
COPY ["src/GalleryBetak.Domain/GalleryBetak.Domain.csproj", "src/GalleryBetak.Domain/"]
COPY ["src/GalleryBetak.Infrastructure/GalleryBetak.Infrastructure.csproj", "src/GalleryBetak.Infrastructure/"]
RUN dotnet restore "./src/GalleryBetak.API/GalleryBetak.API.csproj"
COPY . .
WORKDIR "/src/src/GalleryBetak.API"
RUN dotnet build "./GalleryBetak.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GalleryBetak.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GalleryBetak.API.dll"]

