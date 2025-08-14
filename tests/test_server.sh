#!/bin/bash

echo "Testing Unity MCP Server..."
echo ""

# Test if server is responding
echo "1. Testing HTTP connection to localhost:9876..."
curl -X POST http://localhost:9876 \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"initialize","params":{},"id":1}' \
  -w "\nHTTP Status: %{http_code}\n" \
  2>/dev/null

echo ""
echo "2. Testing tools/list method..."
curl -X POST http://localhost:9876 \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/list","params":{},"id":2}' \
  2>/dev/null | python3 -m json.tool 2>/dev/null || echo "Failed to get tools list"

echo ""
echo "3. Checking if port 9876 is open..."
lsof -i :9876 | grep LISTEN || echo "Port 9876 is not listening"

echo ""
echo "4. Testing with telnet..."
echo "quit" | telnet localhost 9876 2>/dev/null | head -5 || echo "Telnet connection failed"