#!/bin/bash

# Test Deployment Script for Independent Layered Architecture
# This script validates the containerized deployment

echo "========================================"
echo "Testing Independent Layered Architecture"
echo "========================================"
echo ""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Function to test endpoint
test_endpoint() {
    local name=$1
    local url=$2
    local expected_status=${3:-200}
    
    echo -n "Testing $name..."
    status_code=$(curl -s -o /dev/null -w "%{http_code}" "$url" 2>/dev/null)
    
    if [ "$status_code" -eq "$expected_status" ]; then
        echo -e " ${GREEN}✓ OK${NC}"
        return 0
    else
        echo -e " ${RED}✗ FAIL (Status: $status_code)${NC}"
        return 1
    fi
}

# Function to check if container is running
test_container() {
    local name=$1
    
    echo -n "Checking container: $name..."
    if docker ps --filter "name=$name" --format "{{.Names}}" | grep -q "^$name$"; then
        echo -e " ${GREEN}✓ Running${NC}"
        return 0
    else
        echo -e " ${RED}✗ Not Running${NC}"
        return 1
    fi
}

echo -e "${YELLOW}1. Checking Docker Containers${NC}"
echo "------------------------------"
containers=(
    "nifi-metadata-arangodb"
    "nifi-metadata-opensearch"
    "nifi-metadata-redis"
    "nifi-metadata-api"
    "nifi-metadata-ingestion"
    "nifi-metadata-frontend"
)

all_containers_running=true
for container in "${containers[@]}"; do
    if ! test_container "$container"; then
        all_containers_running=false
    fi
done
echo ""

echo -e "${YELLOW}2. Testing Storage Layer${NC}"
echo "-------------------------"
storage_ok=true
test_endpoint "ArangoDB" "http://localhost:8529/_api/version" || storage_ok=false
test_endpoint "OpenSearch" "http://localhost:9200/_cluster/health" || storage_ok=false
echo ""

echo -e "${YELLOW}3. Testing API Layer${NC}"
echo "--------------------"
api_ok=true
test_endpoint "API Health" "http://localhost:5000/health" || api_ok=false
echo ""

echo -e "${YELLOW}4. Testing UI Layer${NC}"
echo "-------------------"
ui_ok=true
test_endpoint "Frontend" "http://localhost:5173" || ui_ok=false
echo ""

echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}Test Summary${NC}"
echo -e "${CYAN}========================================${NC}"
echo -n "Containers:    "
if [ "$all_containers_running" = true ]; then
    echo -e "${GREEN}✓ All Running${NC}"
else
    echo -e "${RED}✗ Some Failed${NC}"
fi

echo -n "Storage Layer: "
if [ "$storage_ok" = true ]; then
    echo -e "${GREEN}✓ Healthy${NC}"
else
    echo -e "${RED}✗ Issues Detected${NC}"
fi

echo -n "API Layer:     "
if [ "$api_ok" = true ]; then
    echo -e "${GREEN}✓ Healthy${NC}"
else
    echo -e "${RED}✗ Issues Detected${NC}"
fi

echo -n "UI Layer:      "
if [ "$ui_ok" = true ]; then
    echo -e "${GREEN}✓ Accessible${NC}"
else
    echo -e "${RED}✗ Not Accessible${NC}"
fi
echo ""

if [ "$all_containers_running" = true ] && [ "$storage_ok" = true ] && [ "$api_ok" = true ] && [ "$ui_ok" = true ]; then
    echo -e "${GREEN}✓ All tests passed! Architecture is working correctly.${NC}"
    echo ""
    echo -e "${CYAN}Access Points:${NC}"
    echo "  - UI:          http://localhost:5173"
    echo "  - API:         http://localhost:5000"
    echo "  - ArangoDB:    http://localhost:8529"
    echo "  - OpenSearch:  http://localhost:9200"
    exit 0
else
    echo -e "${RED}✗ Some tests failed. Please check the logs.${NC}"
    echo ""
    echo -e "${YELLOW}To view logs, run:${NC}"
    echo "  docker-compose -f docker/docker-compose.yml logs -f"
    exit 1
fi
