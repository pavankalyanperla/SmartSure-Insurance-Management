FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5073

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["AdminService.API/AdminService.API.csproj", "AdminService.API/"]
COPY ["AdminService.Application/AdminService.Application.csproj", "AdminService.Application/"]
COPY ["AdminService.Domain/AdminService.Domain.csproj", "AdminService.Domain/"]
COPY ["AdminService.Infrastructure/AdminService.Infrastructure.csproj", "AdminService.Infrastructure/"]
RUN dotnet restore "AdminService.API/AdminService.API.csproj"
COPY . .
WORKDIR "/src/AdminService.API"
RUN dotnet build "AdminService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AdminService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AdminService.API.dll"]
