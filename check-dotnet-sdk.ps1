# .NET SDK and MSBuild Check Script

Write-Host "=== .NET SDK Environment Check ===" -ForegroundColor Cyan

# Check .NET SDK
Write-Host ""
Write-Host "[1] Checking .NET SDK" -ForegroundColor Yellow
$dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
if ($dotnetCmd) {
    $version = dotnet --version 2>&1
    Write-Host "OK: .NET SDK is installed: $version" -ForegroundColor Green
} else {
    Write-Host "ERROR: .NET SDK not found" -ForegroundColor Red
    Write-Host "  Install from: https://dotnet.microsoft.com/download" -ForegroundColor Gray
}

# Check dotnet msbuild
Write-Host ""
Write-Host "[2] Checking dotnet msbuild" -ForegroundColor Yellow
if ($dotnetCmd) {
    $msbuildCheck = dotnet msbuild -version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "OK: dotnet msbuild is available" -ForegroundColor Green
    } else {
        Write-Host "ERROR: dotnet msbuild is not available" -ForegroundColor Red
    }
} else {
    Write-Host "SKIP: .NET SDK not found" -ForegroundColor Yellow
}

# Check .NET Framework Developer Pack
Write-Host ""
Write-Host "[3] Checking .NET Framework Developer Pack" -ForegroundColor Yellow
$frameworkPaths = @(
    "${env:ProgramFiles(x86)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5",
    "${env:ProgramFiles}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5",
    "${env:ProgramFiles(x86)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
)

$found = $false
foreach ($path in $frameworkPaths) {
    if (Test-Path $path) {
        Write-Host "OK: .NET Framework reference assemblies found: $path" -ForegroundColor Green
        $found = $true
        break
    }
}

if (-not $found) {
    Write-Host "ERROR: .NET Framework Developer Pack not found" -ForegroundColor Red
    Write-Host "  Install from: https://dotnet.microsoft.com/download/dotnet-framework/net48" -ForegroundColor Gray
    Write-Host "  (.NET Framework 4.8 Developer Pack includes 4.5)" -ForegroundColor Gray
}

# Build test
Write-Host ""
Write-Host "[4] Build Test" -ForegroundColor Yellow
if (Test-Path "DesktopMinidamWorking.sln") {
    Write-Host "Project file found. Attempting build..." -ForegroundColor Gray
    if ($dotnetCmd) {
        dotnet msbuild DesktopMinidamWorking.sln /p:Configuration=Debug /t:Build /v:minimal
        if ($LASTEXITCODE -eq 0) {
            Write-Host "OK: Build succeeded!" -ForegroundColor Green
        } else {
            Write-Host "ERROR: Build failed" -ForegroundColor Red
        }
    } else {
        Write-Host "SKIP: .NET SDK not available" -ForegroundColor Yellow
    }
} else {
    Write-Host "Project file not found" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Check Complete ===" -ForegroundColor Cyan
