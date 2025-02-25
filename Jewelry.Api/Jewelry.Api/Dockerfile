# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# เปลี่ยนเป็น root user ชั่วคราวเพื่อสร้างโฟลเดอร์และตั้งค่าสิทธิ์
USER root
# สร้างโฟลเดอร์สำหรับเก็บรูปภาพและกำหนดสิทธิ์
#RUN mkdir -p /app/Images/Mold && \
    #chown -R app:app /app/Images && \
    #chmod -R 755 /app/Images

USER root

RUN mkdir -p /app/Images/Mold && \
    mkdir -p /app/Images/Stock/Product && \
    chmod -R 777 /app/Images && \
    chown -R app:app /app/Images

# กลับไปใช้ app user
USER app
WORKDIR /app
EXPOSE 7000
EXPOSE 7000

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Jewelry.Api/Jewelry.Api.csproj", "Jewelry.Api/"]
COPY ["DynamicLinqCore/DynamicLinqCore.csproj", "DynamicLinqCore/"]
COPY ["jewelry.Model/jewelry.Model.csproj", "jewelry.Model/"]
COPY ["Jewelry.Service/Jewelry.Service.csproj", "Jewelry.Service/"]
COPY ["Jewelry.Data/Jewelry.Data.csproj", "Jewelry.Data/"]
RUN dotnet restore "./Jewelry.Api/Jewelry.Api.csproj"
COPY . .
WORKDIR "/src/Jewelry.Api"
RUN dotnet build "./Jewelry.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Jewelry.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# เพิ่มขั้นตอนตรวจสอบและรักษาสิทธิ์โฟลเดอร์
USER root
RUN mkdir -p /app/Images/Mold && \
    mkdir -p /app/Images/Stock/Product && \  
    chmod -R 777  /app/Images && \
    chown -R app:app /app/Images


USER app
ENTRYPOINT ["dotnet", "Jewelry.Api.dll"]