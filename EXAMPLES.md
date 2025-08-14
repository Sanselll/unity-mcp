# Unity MCP Server - Usage Examples

This document provides practical examples of using the Unity MCP Server with Claude Code.

## Table of Contents

- [Basic Setup Verification](#basic-setup-verification)
- [Scene Manipulation](#scene-manipulation)
- [GameObject Operations](#gameobject-operations)
- [Console and Logging](#console-and-logging)
- [Play Mode Control](#play-mode-control)
- [Project Management](#project-management)
- [Development Workflows](#development-workflows)

## Basic Setup Verification

### Check Server Status
```
Ask Claude: "Is the Unity MCP server running?"
```
Claude will use the `server/status` endpoint to check connectivity and Unity state.

### Test Basic Connectivity
```
Ask Claude: "Execute some simple C# code in Unity"
```
Example response from Claude:
```csharp
// Claude will use execute_query tool
var message = "Hello from Claude Code!";
Debug.Log(message);
return $"Successfully executed: {message}";
```

## Scene Manipulation

### Scene Analysis
```
Ask Claude: "What's currently in my Unity scene?"
```
Claude will use `list_scene_objects` to show all GameObjects, their positions, and components.

### Create Scene Layout
```
Ask Claude: "Create a basic 3D scene with a ground plane, some cubes, and a light"
```
Claude might respond:
```csharp
// Create ground plane
var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
ground.name = "Ground";
ground.transform.localScale = new Vector3(10, 1, 10);

// Create some cubes
for (int i = 0; i < 5; i++) {
    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    cube.name = $"Cube_{i}";
    cube.transform.position = new Vector3(i * 2 - 4, 0.5f, 0);
}

// Add a light
var lightObj = new GameObject("Scene Light");
var light = lightObj.AddComponent<Light>();
light.type = LightType.Directional;
lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

return "Created basic 3D scene with ground, 5 cubes, and directional light";
```

### Scene Cleanup
```
Ask Claude: "Remove all GameObjects tagged as 'temporary'"
```

## GameObject Operations

### Batch GameObject Creation
```
Ask Claude: "Create a circle of 8 spheres around the origin"
```
Claude will calculate positions and create spheres in a circular pattern.

### GameObject Search and Modification
```
Ask Claude: "Find all cubes in the scene and make them red"
```
Example:
```csharp
var cubes = GameObject.FindGameObjectsWithTag("Cube") 
    .Concat(GameObject.FindObjectsOfType<GameObject>()
    .Where(go => go.name.Contains("Cube")))
    .Distinct();

foreach (var cube in cubes) {
    var renderer = cube.GetComponent<Renderer>();
    if (renderer != null) {
        renderer.material.color = Color.red;
    }
}

return $"Made {cubes.Count()} cubes red";
```

### Transform Operations
```
Ask Claude: "Move all objects up by 2 units"
```
```
Ask Claude: "Rotate the main camera to look at the center cube"
```

## Console and Logging

### Error Investigation
```
Ask Claude: "Show me the recent errors from the Unity console"
```
Claude will use `get_logs` with error filtering enabled.

### Log Analysis
```
Ask Claude: "Check if there are any compilation errors and explain what they mean"
```
Claude will retrieve error logs and provide explanations.

### Console Management
```
Ask Claude: "Clear the Unity console and run a test that logs some information"
```

## Play Mode Control

### Testing Workflows
```
Ask Claude: "Start play mode, create a test object, wait 2 seconds, then stop play mode"
```

### Automated Testing
```
Ask Claude: "Enter play mode and check if the player moves correctly when I press keys"
```
Note: Claude can enter play mode but cannot directly simulate key presses.

### Performance Monitoring
```
Ask Claude: "Start play mode and tell me the current frame rate"
```

## Project Management

### Asset Discovery
```
Ask Claude: "List all scenes in my project"
```

### Project Information
```
Ask Claude: "What Unity version is this project using and what's the current scene?"
```

### Script Analysis
```
Ask Claude: "Find all MonoBehaviour scripts in the project"
```

## Development Workflows

### Rapid Prototyping
```
Ask Claude: "Create a simple physics simulation with 10 bouncing balls"
```

### Component Testing
```
Ask Claude: "Create a test object with a Rigidbody and show me its physics properties"
```

### Shader and Material Testing
```
Ask Claude: "Create spheres with different materials to test lighting"
```

### Animation Setup
```
Ask Claude: "Create a cube that rotates continuously and moves in a figure-8 pattern"
```

### Debugging Helpers
```
Ask Claude: "Create debug visualization gizmos for all transform positions in the scene"
```

### Performance Testing
```
Ask Claude: "Create 100 cubes and measure the performance impact"
```

## Advanced Examples

### Custom Editor Tools
```
Ask Claude: "Create a tool that arranges selected objects in a grid pattern"
```

### Data Visualization
```
Ask Claude: "Create a 3D bar chart using cubes to represent some sample data"
```

### Procedural Generation
```
Ask Claude: "Generate a simple maze using cubes"
```

### Physics Experiments
```
Ask Claude: "Set up a domino effect with 20 cubes in a line"
```

### Camera Management
```
Ask Claude: "Create multiple camera views showing the scene from different angles"
```

## Integration with Unity Features

### Addressable Assets
```
Ask Claude: "Load an addressable prefab if the addressable system is set up"
```

### ScriptableObjects
```
Ask Claude: "Create a ScriptableObject to store game settings"
```

### Unity Events
```
Ask Claude: "Set up a UnityEvent system for object interactions"
```

## Error Handling Examples

### Graceful Failures
When Claude encounters errors, it will:
1. Show the error message
2. Suggest possible fixes
3. Offer alternative approaches

Example conversation:
```
User: "Create a cube with an invalid component"
Claude: "I tried to create the cube but encountered an error: 'InvalidComponent' does not exist. Would you like me to create a cube with a valid component like Rigidbody or Collider instead?"
```

### Compilation Safety
If Unity is compiling when Claude tries to execute code:
```
Claude: "Unity is currently compiling scripts. I'll wait for compilation to complete before executing your request."
```

## Best Practices

### Effective Prompts
- Be specific about what you want to achieve
- Mention if you want explanations or just results
- Specify coordinate systems or scales when relevant

### Performance Considerations
- Claude can create many objects quickly, but be mindful of Unity's performance
- Ask Claude to batch operations when possible
- Request performance feedback for large operations

### Safety
- Claude will warn about potentially destructive operations
- Always test in a separate scene or project first
- Claude can help create backups before major changes

---

For more information, see the [main README](README.md) and [API documentation](API.md).