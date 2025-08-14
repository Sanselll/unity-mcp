# Unity MCP

Enable Claude Code to interact directly with Unity Editor through the Model Context Protocol (MCP).

## Architecture

Unity MCP runs a pure C# HTTP server (port 9876) within Unity Editor itself, implementing the MCP JSON-RPC protocol. No external dependencies required.

**System Components:**
- **MCPServer.cs** - HTTP MCP server running on port 9876
- **DynamicToolManager.cs** - Manages tools defined in JSON files
- **UnityDynamicCompiler.cs** - Compiles and executes C# code queries

## Installation

1. Copy the `com.mcp.unity` package to your project's `Packages/` folder
2. Unity will automatically import the package
3. The MCP server starts automatically when Unity loads

## Configuration

Configure Claude Code to connect:
```bash
claude mcp add --transport http unity http://localhost:9876
```

## Available Tools

### Core Tools
- `execute_query` - Execute arbitrary C# code in Unity
- `get_logs` - Retrieve Unity console logs (errors, warnings, logs)
- `clear_logs` - Clear Unity console
- `create_gameobject` - Create GameObjects with primitive types
- `list_scene_objects` - List GameObjects in current scene
- `list_scenes` - List all scenes in project
- `find_object` - Find GameObjects by name pattern
- `focus_unity` - Bring Unity Editor to foreground
- `recompile` - Trigger script compilation
- `play_mode` - Control play/pause/stop

### Tool System

Tools are defined as JSON files in:
- `Editor/Tools/Default/` - Built-in tools
- `Editor/Tools/Custom/` - Custom tool definitions

JSON tool structure:
```json
{
  "name": "tool_name",
  "description": "Tool description", 
  "queryTemplate": "C# code with {{parameter}} placeholders",
  "inputSchema": {
    "type": "object",
    "properties": { /* parameter definitions */ }
  }
}
```

## Unity Menu

Access via **Tools > Unity MCP**:
- Start/Stop/Restart Server
- View Server Status

## Requirements

- Unity 2020.3 or later
- Newtonsoft.Json package (auto-installed)
- Claude Code desktop app

## Server Status

The server provides compilation-aware responses:
- Returns error code -32000 when Unity is compiling
- Includes `/status` endpoint for health checks

## License

Copyright © 2024 Volodymyr Bouland
MIT License