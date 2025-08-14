#!/bin/bash

echo "Testing Unity MCP Compilation Fix"
echo "================================="
echo ""

# Test with using statements
echo "Test 1: Code with using statements..."
curl -s -X POST http://localhost:9876 \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "execute_query",
      "arguments": {
        "queryCode": "using System.IO;\nusing System.Text;\n\nvar message = \"Test successful!\";\nreturn message;"
      }
    },
    "id": 1
  }' | python3 -m json.tool

echo ""
echo "Test 2: Simple return statement..."
curl -s -X POST http://localhost:9876 \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "execute_query",
      "arguments": {
        "queryCode": "return \"Hello from Unity!\";"
      }
    },
    "id": 2
  }' | python3 -m json.tool

echo ""
echo "Test 3: Get current scene..."
curl -s -X POST http://localhost:9876 \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "execute_query",
      "arguments": {
        "queryCode": "var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();\nreturn $\"Current scene: {scene.name}\";"
      }
    },
    "id": 3
  }' | python3 -m json.tool