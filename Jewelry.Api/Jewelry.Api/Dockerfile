# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
WORKDIR /app

# สร้างโฟลเดอร์สำหรับเก็บรูปภาพและกำหนดสิทธิ์
RUN mkdir -p /app/Images/Mold && \
    mkdir -p /app/Images/Stock/Product && \
    chmod -R 755 /app/Images

# แก้ไข port exposure - ใช้ standard ports
EXPOSE 8080

# เปลี่ยนกลับเป็น app user
USER app

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files - adjusted for repository structure
# Repository root is jewelry-api, projects are in Jewelry.Api folder
COPY ["Jewelry.Api/Jewelry.Api/Jewelry.Api.csproj", "Jewelry.Api/"]
COPY ["Jewelry.Api/DynamicLinqCore/DynamicLinqCore.csproj", "DynamicLinqCore/"]
COPY ["Jewelry.Api/jewelry.Model/jewelry.Model.csproj", "jewelry.Model/"]
COPY ["Jewelry.Api/Jewelry.Service/Jewelry.Service.csproj", "Jewelry.Service/"]
COPY ["Jewelry.Api/Jewelry.Data/Jewelry.Data.csproj", "Jewelry.Data/"]

# Restore dependencies
RUN dotnet restore "Jewelry.Api/Jewelry.Api.csproj"

# Copy all source code
COPY ["Jewelry.Api/", "."]

# Build project
WORKDIR "/src/Jewelry.Api"
RUN dotnet build "Jewelry.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Jewelry.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
USER root
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# สร้างและตั้งค่าสิทธิ์โฟลเดอร์ Images อีกครั้ง
RUN mkdir -p /app/Images/Mold && \
    mkdir -p /app/Images/Stock/Product && \
    chmod -R 755 /app/Images && \
    chown -R app:app /app/Images

# เปลี่ยนกลับเป็น app user
USER app

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Jewelry.Api.dll"]