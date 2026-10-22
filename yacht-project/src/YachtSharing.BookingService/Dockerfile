FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY src/YachtSharing.BookingService/YachtSharing.BookingService.csproj src/YachtSharing.BookingService/

RUN dotnet restore src/YachtSharing.BookingService/YachtSharing.BookingService.csproj

COPY Protos/ Protos/

COPY src/YachtSharing.BookingService/ src/YachtSharing.BookingService/

WORKDIR /app/src/YachtSharing.BookingService
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

EXPOSE 8080

COPY --from=build /out ./

ENTRYPOINT ["dotnet", "YachtSharing.BookingService.dll"]
