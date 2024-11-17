FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 7000
EXPOSE 7001

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Jewelry.Api/Jewelry.Api.csproj", "Jewelry.Api/"]
RUN dotnet restore "Jewelry.Api/Jewelry.Api.csproj"
COPY . .
WORKDIR "/src/Jewelry.Api"
RUN dotnet build "Jewelry.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Jewelry.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Jewelry.Api.dll"]