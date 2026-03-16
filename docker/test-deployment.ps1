# Test Deployment Script for Independent Layered Architecture
# This script validates the containerized deployment

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Testing Independent Layered Architecture" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Function to test endpoint
function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [int]$ExpectedStatus = 200
    )
    
    Write-Host "Testing $Name..." -NoNewline
    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
        if ($response.StatusCode -eq $ExpectedStatus) {
            Write-Host " ✓ OK" -ForegroundColor Green
            return $true
        } else {
            Write-Host " ✗ FAIL (Status: $($response.StatusCode))" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host " ✗ FAIL ($($_.Exception.Message))" -ForegroundColor Red
        return $false
    }
}

# Function to check if container is running
function Test-Container {
    param([string]$Name)
    
    Write-Host "Checking container: $Name..." -NoNewline
    $container = docker ps --filter "name=$Name" --format "{{.Names}}" 2>$null
    if ($container -eq $Name) {
        Write-Host " ✓ Running" -ForegroundColor Green
        return $true
    } else {
        Write-Host " ✗ Not Running" -ForegroundColor Red
        return $false
    }
}

Write-Host "1. Checking Docker Containers" -ForegroundColor Yellow
Write-Host "------------------------------"
$containers = @(
    "nifi-metadata-arangodb",
    "nifi-metadata-opensearch",
    "nifi-metadata-redis",
    "nifi-metadata-api",
    "nifi-metadata-ingestion",
    "nifi-metadata-frontend"
)

$allContainersRunning = $true
foreach ($container in $containers) {
    if (-not (Test-Container -Name $container)) {
        $allContainersRunning = $false
    }
}
Write-Host ""

Write-Host "2. Testing Storage Layer" -ForegroundColor Yellow
Write-Host "-------------------------"
$storageOk = $true
$storageOk = (Test-Endpoint -Name "ArangoDB" -Url "http://localhost:8529/_api/version") -and $storageOk
$storageOk = (Test-Endpoint -Name "OpenSearch" -Url "http://localhost:9200/_cluster/health") -and $storageOk
$storageOk = (Test-Endpoint -Name "Redis" -Url "http://localhost:6379") -and $storageOk
Write-Host ""

Write-Host "3. Testing API Layer" -ForegroundColor Yellow
Write-Host "--------------------"
$apiOk = Test-Endpoint -Name "API Health" -Url "http://localhost:5000/health"
Write-Host ""

Write-Host "4. Testing UI Layer" -ForegroundColor Yellow
Write-Host "-------------------"
$uiOk = Test-Endpoint -Name "Frontend" -Url "http://localhost:5173"
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Containers:    " -NoNewline
if ($allContainersRunning) { Write-Host "✓ All Running" -ForegroundColor Green } else { Write-Host "✗ Some Failed" -ForegroundColor Red }
Write-Host "Storage Layer: " -NoNewline
if ($storageOk) { Write-Host "✓ Healthy" -ForegroundColor Green } else { Write-Host "✗ Issues Detected" -ForegroundColor Red }
Write-Host "API Layer:     " -NoNewline
if ($apiOk) { Write-Host "✓ Healthy" -ForegroundColor Green } else { Write-Host "✗ Issues Detected" -ForegroundColor Red }
Write-Host "UI Layer:      " -NoNewline
if ($uiOk) { Write-Host "✓ Accessible" -ForegroundColor Green } else { Write-Host "✗ Not Accessible" -ForegroundColor Red }
Write-Host ""

if ($allContainersRunning -and $storageOk -and $apiOk -and $uiOk) {
    Write-Host "✓ All tests passed! Architecture is working correctly." -ForegroundColor Green
    Write-Host ""
    Write-Host "Access Points:" -ForegroundColor Cyan
    Write-Host "  - UI:          http://localhost:5173" -ForegroundColor White
    Write-Host "  - API:         http://localhost:5000" -ForegroundColor White
    Write-Host "  - ArangoDB:    http://localhost:8529" -ForegroundColor White
    Write-Host "  - OpenSearch:  http://localhost:9200" -ForegroundColor White
    exit 0
} else {
    Write-Host "✗ Some tests failed. Please check the logs." -ForegroundColor Red
    Write-Host ""
    Write-Host "To view logs, run:" -ForegroundColor Yellow
    Write-Host "  docker-compose -f docker/docker-compose.yml logs -f" -ForegroundColor White
    exit 1
}
