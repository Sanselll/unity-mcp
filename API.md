# Unity MCP Server API Documentation

This document provides comprehensive API documentation for the Unity MCP Server, including all endpoints, tools, and usage examples.

## Table of Contents

- [Server Endpoints](#server-endpoints)
- [Built-in Tools](#built-in-tools)
- [Custom Tool Development](#custom-tool-development)
- [Error Handling](#error-handling)
- [Examples](#examples)

## Server Endpoints

The Unity MCP Server runs on `http://localhost:9876` and implements the MCP JSON-RPC protocol.

### Base Request Format

All requests follow the JSON-RPC 2.0 specification:

```json
{
  "jsonrpc": "2.0",
  "method": "method_name",
  "params": {},
  "id": 1
}
```

### initialize

Initializes the MCP server connection.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "capabilities": {},
    "clientInfo": {
      "name": "claude-code",
      "version": "1.0.0"
    }
  },
  "id": 1
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2024-11-05",
    "capabilities": {
      "tools": {},
      "resources": {}
    },
    "serverInfo": {
      "name": "unity-mcp-server",
      "version": "1.0.0",
      "description": "Unity MCP Server with dynamic tools for Unity Editor automation and scene manipulation."
    }
  }
}
```

### tools/list

Returns a list of all available tools.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "params": {},
  "id": 2
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "tools": [
      {
        "name": "execute_query",
        "description": "Execute C# code in Unity Editor. Full access to Unity Editor and Runtime APIs for scene manipulation and automation.",
        "inputSchema": {
          "type": "object",
          "properties": {
            "queryCode": {
              "type": "string",
              "description": "C# code to execute. Should return a value or perform Unity operations. Full access to Unity Editor and Runtime APIs."
            },
            "parameters": {
              "type": "object",
              "description": "Optional parameters to pass to the query"
            }
          },
          "required": ["queryCode"]
        }
      },
      // ... other tools
    ]
  }
}
```

### tools/call

Executes a specific tool with provided parameters.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "tool_name",
    "arguments": {
      "parameter1": "value1",
      "parameter2": "value2"
    }
  },
  "id": 3
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Tool execution result"
      }
    ]
  }
}
```

### server/status

Returns current server status and Unity state information.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "server/status",
  "params": {},
  "id": 4
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "result": {
    "running": true,
    "compiling": false,
    "updating": false,
    "playing": false,
    "paused": false,
    "requestCount": 42,
    "lastRequestTime": "2024-08-14 09:57:57",
    "lastMethod": "tools/call"
  }
}
```

### resources/list

Lists available MCP resources.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "resources/list",
  "params": {},
  "id": 5
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "result": {
    "resources": [
      {
        "uri": "unity://console",
        "name": "Unity Console Logs",
        "mimeType": "text/plain",
        "description": "Real-time Unity console output"
      },
      {
        "uri": "unity://project",
        "name": "Unity Project Info",
        "mimeType": "application/json",
        "description": "Current Unity project information"
      }
    ]
  }
}
```

### resources/read

Reads a specific resource.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "resources/read",
  "params": {
    "uri": "unity://project"
  },
  "id": 6
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "result": {
    "contents": [
      {
        "uri": "unity://project",
        "mimeType": "application/json",
        "text": "{\"projectName\":\"MyProject\",\"unityVersion\":\"2022.3.12f1\",\"platform\":\"OSXEditor\",\"isPlaying\":false,\"isPaused\":false}"
      }
    ]
  }
}
```

## Built-in Tools

### execute_query

Execute arbitrary C# code in Unity Editor.

**Parameters:**
- `queryCode` (string, required): C# code to execute
- `parameters` (object, optional): Parameters to pass to the code

**Example:**
```json
{
  "name": "execute_query",
  "arguments": {
    "queryCode": "var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = \"TestCube\"; return $\"Created {cube.name}\";",
    "parameters": {}
  }
}
```

### get_logs

Retrieve Unity console log entries with filtering options.

**Parameters:**
- `count` (number, default: 10): Number of log entries to retrieve
- `includeErrors` (boolean, default: true): Include error messages
- `includeWarnings` (boolean, default: true): Include warning messages  
- `includeLogs` (boolean, default: true): Include regular log messages

**Example:**
```json
{
  "name": "get_logs",
  "arguments": {
    "count": 5,
    "includeErrors": true,
    "includeWarnings": false,
    "includeLogs": false
  }
}
```

**Response:**
```
[ERROR] Assets/Scripts/MyScript.cs(25,15): error CS0103: The name 'undefinedVariable' does not exist in the current context
[ERROR] Compilation failed: 1 error(s)
```

### clear_logs

Clear all Unity console log entries.

**Parameters:** None

**Example:**
```json
{
  "name": "clear_logs",
  "arguments": {}
}
```

### create_gameobject

Create a GameObject with specified properties.

**Parameters:**
- `name` (string, required): Name of the GameObject
- `x` (number, default: 0): X position
- `y` (number, default: 0): Y position
- `z` (number, default: 0): Z position
- `primitiveType` (string, default: "None"): Primitive type (Cube, Sphere, Cylinder, Capsule, Plane, Quad)

**Example:**
```json
{
  "name": "create_gameobject",
  "arguments": {
    "name": "MySpere",
    "x": 2.5,
    "y": 1.0,
    "z": -1.0,
    "primitiveType": "Sphere"
  }
}
```

### list_scene_objects

List all GameObjects in the current scene.

**Parameters:**
- `includeDetails` (boolean, default: false): Include position and component details

**Example:**
```json
{
  "name": "list_scene_objects",
  "arguments": {
    "includeDetails": true
  }
}
```

**Response:**
```
Scene: SampleScene
Total GameObjects: 5

Detailed GameObject List:

• Main Camera (Active)
  Position: (0.00, 1.00, -10.00)
  Components: Camera, AudioListener

• Directional Light (Active)
  Position: (0.00, 3.00, 0.00)
  Components: Light

• TestCube (Active)
  Position: (0.00, 0.00, 0.00)
  Components: MeshRenderer, BoxCollider, MeshFilter
```

### list_scenes

List all scenes in the project.

**Parameters:** None

**Example:**
```json
{
  "name": "list_scenes",
  "arguments": {}
}
```

### focus_unity

Bring Unity Editor window to foreground.

**Parameters:** None

### recompile

Trigger Unity script recompilation.

**Parameters:** None

### play_mode

Control Unity Editor play mode.

**Parameters:**
- `action` (string, required): "play", "pause", or "stop"

**Example:**
```json
{
  "name": "play_mode",
  "arguments": {
    "action": "play"
  }
}
```

### find_object

Find GameObjects in the scene by name pattern.

**Parameters:**
- `objectName` (string, required): Name or partial name of objects to find
- `includeInactive` (boolean, default: false): Include inactive GameObjects

**Example:**
```json
{
  "name": "find_object",
  "arguments": {
    "objectName": "Player",
    "includeInactive": true
  }
}
```

### remove_tool

Remove a custom tool (cannot remove default tools).

**Parameters:**
- `name` (string, required): Name of the tool to remove

**Example:**
```json
{
  "name": "remove_tool",
  "arguments": {
    "name": "my_custom_tool"
  }
}
```

## Tool Development (Manual)

### Tool Definition Format

Developers can manually create custom tools as JSON files in the `Tools/Custom/` directory:

```json
{
  "name": "tool_name",
  "description": "Tool description",
  "queryTemplate": "C# code with {{parameter}} placeholders",
  "inputSchema": {
    "type": "object",
    "properties": {
      "parameter": {
        "type": "string|number|boolean",
        "description": "Parameter description",
        "default": "default_value"
      }
    },
    "required": ["required_param"]
  }
}
```

### Parameter Substitution

Parameters are substituted in the `queryTemplate` using double braces:

```csharp
// Template with parameters
var name = "{{objectName}}";
var count = {{count}};
var active = {{isActive}};

for (int i = 0; i < count; i++) {
    var obj = new GameObject($"{name}_{i}");
    obj.SetActive(active);
}
```

### Type Handling

- **Strings**: Automatically quoted if not already in quotes
- **Numbers**: Used as-is
- **Booleans**: Converted to lowercase (`true`/`false`)
- **Missing Parameters**: Replaced with default values from schema or `null`

### Validation Rules

Custom tools are validated before creation:

1. **Name**: Must be lowercase with underscores only
2. **Template**: Must contain at least one parameter placeholder
3. **Schema**: All placeholders must have corresponding schema definitions
4. **Security**: Cannot use dangerous namespaces (`System.IO.File`, `System.Diagnostics.Process`, `System.Net`)

### Hot Reloading

Tools are automatically reloaded when JSON files change. This enables rapid development without Unity restarts.

## Error Handling

### Compilation Errors

When Unity is compiling scripts, the server returns:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32000,
    "message": "Unity is currently compiling scripts. Please try again in a moment.",
    "data": {
      "status": "compiling"
    }
  }
}
```

### Tool Execution Errors

When tool execution fails:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32603,
    "message": "Tool execution failed: Compilation errors\n\nHINT: The code has compilation errors. Check:\n1. Syntax errors and missing semicolons\n2. Proper namespace imports (using statements)\n3. Correct Unity API usage and type names"
  }
}
```

### Common Error Codes

- `-32700`: Parse error
- `-32600`: Invalid request
- `-32601`: Method not found
- `-32602`: Invalid params
- `-32603`: Internal error
- `-32000`: Server error (Unity compiling)

## Examples

### Create Multiple GameObjects

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "execute_query",
    "arguments": {
      "queryCode": "for (int i = 0; i < 5; i++) { var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.transform.position = new Vector3(i * 2, 0, 0); cube.name = $\"Cube_{i}\"; } return \"Created 5 cubes\";"
    }
  },
  "id": 1
}
```

### Query Scene Information

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "execute_query",
    "arguments": {
      "queryCode": "var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene(); var objects = UnityEngine.Object.FindObjectsByType<GameObject>(UnityEngine.FindObjectsSortMode.None); return $\"Scene: {scene.name}, Objects: {objects.Length}\";"
    }
  },
  "id": 1
}
```

### Custom Component Creation

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "execute_query",
    "arguments": {
      "queryCode": "var obj = new GameObject(\"Rotator\"); var rotator = obj.AddComponent<Rigidbody>(); rotator.angularVelocity = Vector3.up * 5; return $\"Created rotating object: {obj.name}\";"
    }
  },
  "id": 1
}
```

### Batch Operations

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "execute_query",
    "arguments": {
      "queryCode": "var cubes = GameObject.FindGameObjectsWithTag(\"Cube\"); foreach(var cube in cubes) { cube.transform.Rotate(0, 45, 0); } return $\"Rotated {cubes.Length} cubes\";"
    }
  },
  "id": 1
}
```

---

For more information, see the [main README](README.md) or visit the [MCP Specification](https://modelcontextprotocol.io/).