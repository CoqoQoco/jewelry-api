#!/bin/bash

# Test ConfirmStockItems API
# This script tests the API endpoints for Sale Order functionality

echo "=== Testing Sale Order API ==="

# Set base URL (adjust as needed when API is running)
BASE_URL="https://localhost:7001"
# BASE_URL="http://localhost:7000"

echo "1. Testing /SaleOrder/GenerateRunningNumber..."
curl -k -X POST \
  "${BASE_URL}/SaleOrder/GenerateRunningNumber" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  2>/dev/null | jq .

echo -e "\n2. Creating test Sale Order..."
SO_NUMBER=$(curl -k -X POST \
  "${BASE_URL}/SaleOrder/Upsert" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -d @test-create-so.json \
  2>/dev/null | jq -r .)

echo "Created SO Number: $SO_NUMBER"

# Update the confirm stock test with the generated SO number
jq --arg so "$SO_NUMBER" '.soNumber = $so' test-confirm-stock.json > test-confirm-stock-updated.json

echo -e "\n3. Testing ConfirmStockItems..."
curl -k -X POST \
  "${BASE_URL}/SaleOrder/ConfirmStockItems" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -d @test-confirm-stock-updated.json \
  2>/dev/null | jq .

echo -e "\n4. Getting Sale Order details..."
curl -k -X POST \
  "${BASE_URL}/SaleOrder/Get" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -d "{\"soNumber\": \"$SO_NUMBER\"}" \
  2>/dev/null | jq .

echo -e "\n=== API Testing Complete ==="
echo "Note: Replace YOUR_JWT_TOKEN_HERE with actual JWT token when testing"
echo "Run this script when the API server is running on ${BASE_URL}"