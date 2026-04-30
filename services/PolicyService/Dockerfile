FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5145

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["PolicyService.API/PolicyService.API.csproj", "PolicyService.API/"]
COPY ["PolicyService.Application/PolicyService.Application.csproj", "PolicyService.Application/"]
COPY ["PolicyService.Domain/PolicyService.Domain.csproj", "PolicyService.Domain/"]
COPY ["PolicyService.Infrastructure/PolicyService.Infrastructure.csproj", "PolicyService.Infrastructure/"]
RUN dotnet restore "PolicyService.API/PolicyService.API.csproj"
COPY . .
WORKDIR "/src/PolicyService.API"
RUN dotnet build "PolicyService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PolicyService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PolicyService.API.dll"]
