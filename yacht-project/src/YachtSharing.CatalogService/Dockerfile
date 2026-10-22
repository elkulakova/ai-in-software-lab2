FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/YachtSharing.CatalogService/YachtSharing.CatalogService.csproj", "src/YachtSharing.CatalogService/"]
COPY ["Protos/catalog.proto", "Protos/"]

RUN dotnet restore "src/YachtSharing.CatalogService/YachtSharing.CatalogService.csproj"

COPY . .
WORKDIR "/src/src/YachtSharing.CatalogService"
RUN dotnet publish "YachtSharing.CatalogService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
EXPOSE 8082

ENTRYPOINT ["dotnet", "YachtSharing.CatalogService.dll"]
