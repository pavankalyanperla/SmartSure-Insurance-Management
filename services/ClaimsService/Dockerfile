FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5084

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["ClaimsService.API/ClaimsService.API.csproj", "ClaimsService.API/"]
COPY ["ClaimsService.Application/ClaimsService.Application.csproj", "ClaimsService.Application/"]
COPY ["ClaimsService.Domain/ClaimsService.Domain.csproj", "ClaimsService.Domain/"]
COPY ["ClaimsService.Infrastructure/ClaimsService.Infrastructure.csproj", "ClaimsService.Infrastructure/"]
RUN dotnet restore "ClaimsService.API/ClaimsService.API.csproj"
COPY . .
WORKDIR "/src/ClaimsService.API"
RUN dotnet build "ClaimsService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ClaimsService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClaimsService.API.dll"]
