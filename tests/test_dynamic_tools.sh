#!/bin/bash

echo "Testing Unity MCP Dynamic Tools"
echo "================================"
echo ""

PORT=9876
BASE_URL="http://localhost:$PORT"

# Test 1: List tools (should include default tools)
echo "1. Listing all tools..."
curl -s -X POST $BASE_URL \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/list",
    "params": {},
    "id": 1
  }' | python3 -m json.tool | head -50

echo ""
echo "2. Creating a custom tool 'get_scene_info'..."
curl -s -X POST $BASE_URL \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "create_tool",
      "arguments": {
        "name": "get_scene_info",
        "description": "Get information about the current Unity scene",
        "queryTemplate": "var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();\nvar info = $\"Scene: {scene.name}\\nPath: {scene.path}\\nIs Loaded: {scene.isLoaded}\\nObject Count: {scene.rootCount}\\nInclude details: {{includeDetails}}\";\nif ({{includeDetails}})\n{\n    var objects = scene.GetRootGameObjects();\n    info += \"\\n\\nRoot GameObjects:\\n\";\n    foreach(var obj in objects)\n    {\n        info += $\"  - {obj.name}\\n\";\n    }\n}\nreturn info;",
        "inputSchema": {
          "type": "object",
          "properties": {
            "includeDetails": {
              "type": "boolean",
              "description": "Include list of root GameObjects",
              "default": false
            }
          }
        }
      }
    },
    "id": 2
  }' | python3 -m json.tool

echo ""
echo "3. Waiting 2 seconds for hot reload..."
sleep 2

echo ""
echo "4. Testing the new 'get_scene_info' tool..."
curl -s -X POST $BASE_URL \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "get_scene_info",
      "arguments": {
        "includeDetails": true
      }
    },
    "id": 3
  }' | python3 -m json.tool

echo ""
echo "5. List tools again (should now include 'get_scene_info')..."
curl -s -X POST $BASE_URL \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/list",
    "params": {},
    "id": 4
  }' | python3 -m json.tool | grep -A5 "get_scene_info"

echo ""
echo "Test complete!"