# Unity MCP Server

A Unity Editor plugin that implements a Model Context Protocol (MCP) server, enabling direct communication between Claude Code and Unity Editor. The system runs a pure C# HTTP server within Unity Editor itself, eliminating the need for external dependencies.

![Unity MCP Demo](https://img.shields.io/badge/Unity-2022.3+-blue) ![MCP](https://img.shields.io/badge/MCP-2024--11--05-green) ![C#](https://img.shields.io/badge/C%23-Editor-purple)

## 🚀 Features

- **Direct HTTP Communication**: No bridge scripts or external processes needed
- **Dynamic Tool System**: JSON-defined tools with hot-reloading capability
- **C# Code Execution**: Execute arbitrary C# code directly in Unity Editor
- **Real-time Unity Control**: Play/pause, scene manipulation, log access
- **Compilation Safety**: Detects Unity compilation state and responds appropriately
- **Auto-start**: Server automatically starts when Unity loads
- **Visual Status Monitor**: Built-in GUI for server management and monitoring

## 🏗️ Architecture

The system consists of three main layers:

1. **HTTP MCP Server** (`MCPServer.cs`) - Handles MCP JSON-RPC requests on port 9876
2. **Dynamic Tool System** (`DynamicToolManager.cs`) - Manages and executes Unity tools defined in JSON files
3. **C# Code Execution** (`UnityDynamicCompiler.cs`) - Compiles and executes C# code queries dynamically

```
Claude Code ←→ HTTP (Port 9876) ←→ Unity MCP Server ←→ Unity Editor API
```

## 📦 Installation

### Prerequisites
- Unity 2022.3 or higher
- [Claude Code CLI](https://docs.anthropic.com/en/docs/claude-code) installed

### Method 1: Unity Package Manager (Recommended)

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click the `+` button and select `Add package from git URL`
3. Enter: `https://github.com/yourusername/unity-mcp.git?path=UnityPackage/com.mcp.unity`
4. Click `Add`

### Method 2: Local Installation

1. Clone this repository:
   ```bash
   git clone https://github.com/yourusername/unity-mcp.git
   cd unity-mcp
   ```

2. Copy the Unity package to your project:
   ```bash
   cp -r UnityPackage/com.mcp.unity /path/to/your/unity/project/Packages/
   ```

3. Unity will automatically import and compile the package

## ⚙️ Configuration

### 1. Unity Setup
The MCP server starts automatically when Unity loads. You can manage it through:
- **Menu**: `Tools > Unity MCP > Server Status`
- **Status Window**: Monitor connections, request counts, and server state

### 2. Claude Code Configuration
Configure Claude Code to connect to the Unity MCP server:

```bash
claude mcp add --transport http unity http://localhost:9876
```

### 3. Verify Connection
Test the connection:

```bash
# Check if server is responding
curl -X POST -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"server/status","id":1}' \
  http://localhost:9876
```

## 🛠️ Usage

### Basic C# Code Execution

In Claude Code, use the `execute_query` tool to run C# code in Unity:

```csharp
// Create a cube
var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
cube.name = "MyCube";
cube.transform.position = new Vector3(0, 1, 0);
return $"Created cube at position {cube.transform.position}";
```

### Built-in Tools

The system includes several pre-built tools:

- **`get_logs`** - Retrieve Unity console logs (errors, warnings, info)
- **`clear_logs`** - Clear Unity console
- **`list_scene_objects`** - List GameObjects in current scene
- **`list_scenes`** - List all scenes in project
- **`create_gameobject`** - Create GameObjects with primitives
- **`focus_unity`** - Bring Unity window to foreground
- **`recompile`** - Trigger script recompilation
- **`play_mode`** - Control Unity play/pause/stop

### Example Workflows

#### Scene Analysis
```
Ask Claude: "What objects are in my current scene?"
```

#### Error Investigation
```
Ask Claude: "Show me the recent errors from Unity console"
```

#### Quick GameObject Creation
```
Ask Claude: "Create 5 cubes in a line along the X axis"
```

#### Play Mode Testing
```
Ask Claude: "Start play mode and create a sphere at the player position"
```

## 🔧 Advanced Usage

### Manual Tool Extension

Developers can extend the system by manually creating JSON tool files in the `Tools/Custom/` directory. See the [Tool Development](#-tool-development) section for details on the JSON format and hot-reloading capabilities.

### API Endpoints

The server exposes several MCP endpoints:

- `initialize` - Server initialization
- `tools/list` - Get available tools
- `tools/call` - Execute a tool
- `server/status` - Get server status
- `resources/list` - List available resources
- `resources/read` - Read resource content

## 🧪 Development & Testing

### Test Scripts

Several test scripts are included for development:

```bash
# Test server connectivity
./tests/test_server.sh

# Test C# compilation features
./tests/test_compilation.sh

# Test dynamic tool creation
./tests/test_dynamic_tools.sh

# Run comprehensive diagnostics
./tests/diagnose.sh
```

### Debugging

1. **Server Status**: Use `Tools > Unity MCP > Server Status` to monitor the server
2. **Console Logs**: Check Unity console for detailed error messages
3. **Port Conflicts**: Server runs on port 9876 (configurable in MCPServer.cs)
4. **Compilation Detection**: Server pauses during Unity recompilation

### Assembly Configuration

The package uses a custom assembly definition (`UnityMCP.Editor.asmdef`) with:
- `autoReferenced: false` - Reduces recompilation triggers
- Editor-only scope
- Dependencies on `Newtonsoft.Json`

## 📝 Tool Development

### Tool Structure
Tools are JSON files with this structure:

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

### Template System
- Use `{{parameterName}}` placeholders in queryTemplate
- Supports parameter substitution with type conversion
- Default values from schema are used for missing parameters
- Full Unity API access in templates

### Hot Reloading
Tools are automatically reloaded when JSON files change, enabling rapid development without Unity restarts.

## 🛡️ Security & Safety

### Code Execution Safety
- Tools are validated before creation
- Dangerous namespaces (`System.IO.File`, `System.Diagnostics.Process`) are blocked
- All code runs within Unity's compilation context
- No external file system access by default

### Compilation Safety
- Server detects Unity compilation state
- Returns appropriate errors during recompilation
- Prevents crashes during domain reloads

## 🐛 Troubleshooting

### Common Issues

**Server won't start**
- Check if port 9876 is available
- Look for Unity console errors
- Try restarting Unity

**Connection timeout**
- Verify server is running: `Tools > Unity MCP > Server Status`
- Check firewall settings
- Test with curl command

**Tools not loading**
- Check JSON syntax in tool files
- Verify file paths and permissions
- Look for validation errors in Unity console

**Compilation errors**
- Server automatically pauses during recompilation
- Wait for compilation to complete
- Check for syntax errors in custom tools

### Performance Notes
- Server runs on Unity's main thread for API safety
- HTTP requests are queued and processed during Update cycle
- Large result sets may impact performance

## 🤝 Contributing

We welcome contributions! Please:

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Submit a pull request

### Development Setup
1. Clone the repository
2. Open in Unity 2022.3+
3. Install dependencies via Package Manager
4. Run test scripts to verify setup

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built for [Claude Code](https://docs.anthropic.com/en/docs/claude-code) integration
- Uses [Model Context Protocol](https://modelcontextprotocol.io/) specification
- Unity Editor APIs and reflection system
- Community feedback and contributions

## 📚 Additional Resources

- [MCP Specification](https://modelcontextprotocol.io/)
- [Claude Code Documentation](https://docs.anthropic.com/en/docs/claude-code)
- [Unity Editor Scripting](https://docs.unity3d.com/ScriptReference/Editor.html)
- [Unity Package Development](https://docs.unity3d.com/Manual/CustomPackages.html)

---

**Made with ❤️ for Unity developers using Claude Code**