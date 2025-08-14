# Unity MCP Bridge

Enable Claude Code to interact with Unity Editor through the Model Context Protocol (MCP).

## Features

- 🚀 **One-Click Setup** - Automated installation and configuration
- 🔄 **Dynamic Scripts** - Claude can write and register Unity tools on-the-fly
- 🎮 **Full Unity Control** - Play mode, scenes, GameObjects, and more
- 📦 **Self-Contained** - Everything included in the Unity package
- ⚡ **Hot Reload** - Scripts compile and register without restarts

## Quick Start

### Installation

#### Option 1: Unity Package Manager (Recommended)

1. Open Unity Package Manager (Window > Package Manager)
2. Click the "+" button and select "Add package from git URL"
3. Enter: `https://github.com/yourusername/unity-mcp.git#package`
4. Click "Add"

#### Option 2: Local Installation

1. Download the package
2. Copy `com.mcp.unity` folder to your project's `Packages` folder

### Setup

After installation, the Setup Wizard will open automatically. If not:

1. Go to **Tools > Unity MCP > Setup Wizard**
2. Click **Install MCP Server** (requires Node.js)
3. Click **Configure Claude Code**
4. Click **Start Unity Bridge**

That's it! Claude Code can now interact with your Unity project.

## Menu Options

- **Tools > Unity MCP > Setup Wizard** - Main configuration window
- **Tools > Unity MCP > Start/Stop MCP Server** - Control the bridge server
- **Tools > Unity MCP > Configure Claude Code** - Auto-configure Claude settings
- **Tools > Unity MCP > Open Scripts Folder** - View Claude-generated scripts
- **Tools > Unity MCP > Documentation** - Open online documentation

## How It Works

```
Claude Code <--> MCP Server <--> Unity Bridge <--> Unity Editor
                     |
                Dynamic Scripts
```

1. Unity Bridge runs a TCP server in the Editor
2. MCP Server connects to Unity and Claude Code
3. Claude can create scripts that become MCP tools instantly
4. All scripts are stored within the package for portability

## Available Tools

### Core Tools
- `create_unity_script` - Create new Unity scripts dynamically
- `unity_play_mode` - Control play/pause/stop
- `unity_scene` - Load/save/create scenes
- `unity_gameobject` - Create/modify GameObjects
- `unity_console` - Read Unity console logs
- `execute_unity_script` - Run any registered script

### Dynamic Scripts
Claude can create custom tools by writing C# scripts:

```csharp
[MCPTool("my_custom_tool")]
public class MyCustomTool : IUnityScript {
    public object Execute(Dictionary<string, object> parameters) {
        // Your Unity code here
        return "Result";
    }
}
```

## Requirements

- Unity 2020.3 or later
- Node.js 18+ (for MCP server)
- Claude Code desktop app

## Troubleshooting

### Bridge Not Connecting
1. Check Unity Console for "[MCP Bridge] Server started"
2. Ensure port 12345 is not blocked
3. Restart Unity Editor

### MCP Server Not Installing
1. Verify Node.js is installed: `node --version`
2. Check npm: `npm --version`
3. Run Setup Wizard again

### Scripts Not Compiling
1. Check Unity Console for errors
2. Ensure script implements `IUnityScript`
3. Verify using statements are included

## Settings

Settings are saved in Unity Editor Preferences:
- **Auto-start server** - Start bridge when Unity opens
- **Server port** - TCP port (default: 12345)
- **Verbose logging** - Enable detailed logs

## Support

- [Documentation](https://github.com/yourusername/unity-mcp)
- [Issues](https://github.com/yourusername/unity-mcp/issues)

## License

MIT