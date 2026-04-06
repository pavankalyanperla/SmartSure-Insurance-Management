param(
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$services = @(
    @{
        Name = "IdentityService"
        Project = "services\IdentityService\IdentityService.API\IdentityService.API.csproj"
        Url = "http://localhost:5265"
    },
    @{
        Name = "PolicyService"
        Project = "services\PolicyService\PolicyService.API\PolicyService.API.csproj"
        Url = "http://localhost:5145"
    },
    @{
        Name = "ClaimsService"
        Project = "services\ClaimsService\ClaimsService.API\ClaimsService.API.csproj"
        Url = "http://localhost:5084"
    },
    @{
        Name = "AdminService"
        Project = "services\AdminService\AdminService.API\AdminService.API.csproj"
        Url = "http://localhost:5073"
    },
    @{
        Name = "ApiGateway"
        Project = "gateway\ApiGateway\ApiGateway.csproj"
        Url = "http://localhost:5000"
    }
)

foreach ($service in $services) {
    $projectPath = Join-Path $root $service.Project

    if (-not (Test-Path $projectPath)) {
        throw "Project not found: $projectPath"
    }

    $runArgs = @(
        "run"
        "--project"
        "`"$projectPath`""
        "--urls"
        $service.Url
        "--environment"
        "Development"
    )

    if ($NoBuild) {
        $runArgs += "--no-build"
    }

    $command = "dotnet " + ($runArgs -join " ")

    Start-Process -FilePath "powershell" -ArgumentList "-NoExit", "-Command", $command -WorkingDirectory $root | Out-Null

    Write-Host "[STARTED] $($service.Name) -> $($service.Url)"
}

Write-Host "All services are starting in separate terminals."
Write-Host "Use Ctrl+C in each terminal window to stop a service."
