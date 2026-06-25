FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

COPY . .
WORKDIR /repo/src/Services/OrderService/OrderService.API
RUN dotnet restore OrderService.API.csproj
RUN dotnet publish OrderService.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderService.API.dll"]
