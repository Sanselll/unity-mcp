# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity MCP is a Unity Editor plugin that implements an MCP (Model Context Protocol) server, enabling direct communication between Claude Code and Unity Editor. The system runs a pure C# HTTP server on port 9876 within Unity Editor itself, eliminating the need for external dependencies.

## Architecture

The system consists of three main layers:
1. **HTTP MCP Server** (`MCPServer.cs`) - Handles MCP JSON-RPC requests on port 9876
2. **Dynamic Tool System** (`DynamicToolManager.cs`) - Manages and executes Unity tools defined in JSON files
3. **C# Code Execution** (`DynamicCompiler.cs`) - Compiles and executes C# code queries dynamically

Tools are stored as JSON files in:
- `UnityPackage/com.mcp.unity/Editor/Tools/Default/` - Built-in tools (create_gameobject, get_logs, etc.)
- `UnityPackage/com.mcp.unity/Editor/Tools/Custom/` - Runtime-created custom tools

## Development Commands

### Server Testing
```bash
# Test if MCP server is responding
./test_server.sh

# Run comprehensive diagnostics
./diagnose.sh

# Test dynamic tool creation and execution
./test_dynamic_tools.sh

# Test C# code compilation features
./test_compilation.sh
```

### Unity Package Structure
The Unity package follows standard Unity package layout:
- `package.json` - Unity package manifest with Newtonsoft.Json dependency
- `Editor/` - All C# scripts (Editor-only assembly)
- `Editor/Tools/` - JSON tool definitions loaded at runtime

### MCP Configuration
Server runs on `http://localhost:9876` (changed from 5001 to avoid conflicts). Configure Claude Code with:
```bash
claude mcp add --transport http unity http://localhost:9876
```

## Key Components

### MCPServer.cs
- HTTP listener on port 9876
- Auto-starts when Unity loads via `[InitializeOnLoad]`
- Processes MCP JSON-RPC requests on background thread
- Queues responses for main thread execution
- Menu items: Tools > Unity MCP > Start/Stop/Restart Server

### DynamicToolManager.cs
- Loads tool definitions from JSON files
- File system watchers for hot-reloading tools
- Thread-safe tool registry with locking
- Supports both default and custom tool directories

### DynamicCompiler.cs
- Compiles C# code snippets using Roslyn
- Template substitution system for parameterized queries
- Comprehensive using statements included automatically
- Error handling with detailed compilation diagnostics

## Tool Development

Tools are JSON files with this structure:
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

The `queryTemplate` supports:
- Parameter substitution with `{{parameterName}}`
- Full Unity API access
- Automatic using statement inclusion
- Return value handling

## Core Available Tools

- `execute_query` - Execute arbitrary C# code in Unity
- `create_gameobject` - Create GameObjects with primitive types
- `get_logs` - Retrieve Unity console logs
- `list_scene_objects` - List GameObjects in current scene
- `list_scenes` - List all scenes in project
- `remove_tool` - Remove custom tools at runtime

## Testing Strategy

Use the provided shell scripts to verify functionality:
1. `diagnose.sh` - Check Unity running, port availability, HTTP connectivity
2. `test_server.sh` - Test basic MCP protocol communication
3. `test_dynamic_tools.sh` - Test tool creation and hot-reloading
4. `test_compilation.sh` - Test C# code execution with various patterns

## Important Implementation Details

- Server runs on main Unity thread for API access safety
- Background HTTP listener queues requests for main thread processing
- File system watchers enable hot-reloading of tool definitions
- All C# execution happens within Unity's compilation context
- Tool registry uses locks for thread safety
- Session tracking prevents tool persistence between Unity restarts

## Configuration Files

- `unity-mcp-config.json` - Claude Code MCP server configuration
- `UnityPackage/com.mcp.unity/package.json` - Unity package manifest
- Tool JSON files define available MCP tools and their C# implementations