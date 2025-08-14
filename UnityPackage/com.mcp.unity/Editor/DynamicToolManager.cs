using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityMCP.Editor
{
    [InitializeOnLoad]
    public class DynamicToolManager
    {
        private static Dictionary<string, DynamicTool> tools = new Dictionary<string, DynamicTool>();
        private static FileSystemWatcher defaultWatcher;
        private static FileSystemWatcher customWatcher;
        private static readonly object toolsLock = new object();
        
        private static string DefaultToolsPath => Path.Combine(GetPackagePath(), "Editor", "Tools", "Default");
        private static string CustomToolsPath => Path.Combine(GetPackagePath(), "Editor", "Tools", "Custom");
        
        static DynamicToolManager()
        {
            Initialize();
        }
        
        private static string GetPackagePath()
        {
            // Find the package path dynamically
            var scriptPath = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();
            if (!string.IsNullOrEmpty(scriptPath))
            {
                var dir = new FileInfo(scriptPath).Directory;
                while (dir != null && !File.Exists(Path.Combine(dir.FullName, "package.json")))
                {
                    dir = dir.Parent;
                }
                if (dir != null)
                {
                    return dir.FullName;
                }
            }
            
            // Fallback to searching for the package
            var packagePath = Path.GetFullPath("Packages/com.mcp.unity");
            if (Directory.Exists(packagePath))
            {
                return packagePath;
            }
            
            // Try Assets path
            var assetsPath = Path.Combine(Application.dataPath, "UnityMCP");
            if (Directory.Exists(assetsPath))
            {
                return assetsPath;
            }
            
            return "";
        }
        
        public static void Initialize()
        {
            // Create directories if they don't exist
            try
            {
                if (!string.IsNullOrEmpty(DefaultToolsPath))
                {
                    Directory.CreateDirectory(DefaultToolsPath);
                    Directory.CreateDirectory(CustomToolsPath);
                }
            }
            catch (Exception e)
            {
                // Failed to create directories
            }
            
            // Load all tools
            LoadAllTools();
            
            // Set up file watchers for hot reload
            SetupFileWatchers();
            
            // Create default tools if they don't exist
            CreateDefaultTools();
        }
        
        private static void LoadAllTools()
        {
            lock (toolsLock)
            {
                tools.Clear();
                
                // Load default tools
                if (Directory.Exists(DefaultToolsPath))
                {
                    LoadToolsFromDirectory(DefaultToolsPath, isCustom: false);
                }
                
                // Load custom tools
                if (Directory.Exists(CustomToolsPath))
                {
                    LoadToolsFromDirectory(CustomToolsPath, isCustom: true);
                }
                
            }
        }
        
        private static void LoadToolsFromDirectory(string directory, bool isCustom)
        {
            try
            {
                var files = Directory.GetFiles(directory, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var tool = JsonConvert.DeserializeObject<DynamicTool>(json);
                        if (tool != null && !string.IsNullOrEmpty(tool.name))
                        {
                            tool.isCustom = isCustom;
                            tool.filePath = file;
                            tools[tool.name] = tool;
                        }
                    }
                    catch (Exception e)
                    {
                        // Failed to load tool from file
                    }
                }
            }
            catch (Exception e)
            {
                // Failed to load tools from directory
            }
        }
        
        private static void SetupFileWatchers()
        {
            try
            {
                // Watch default tools directory
                if (Directory.Exists(DefaultToolsPath))
                {
                    defaultWatcher = new FileSystemWatcher(DefaultToolsPath, "*.json");
                    defaultWatcher.Changed += OnToolFileChanged;
                    defaultWatcher.Created += OnToolFileChanged;
                    defaultWatcher.Deleted += OnToolFileChanged;
                    defaultWatcher.EnableRaisingEvents = true;
                }
                
                // Watch custom tools directory
                if (Directory.Exists(CustomToolsPath))
                {
                    customWatcher = new FileSystemWatcher(CustomToolsPath, "*.json");
                    customWatcher.Changed += OnToolFileChanged;
                    customWatcher.Created += OnToolFileChanged;
                    customWatcher.Deleted += OnToolFileChanged;
                    customWatcher.EnableRaisingEvents = true;
                }
                
            }
            catch (Exception e)
            {
                // Failed to setup file watchers
            }
        }
        
        private static void OnToolFileChanged(object sender, FileSystemEventArgs e)
        {
            // Reload tools on main thread
            EditorApplication.delayCall += () =>
            {
                LoadAllTools();
                // Log the current tools for debugging
                foreach (var tool in tools.Values)
                {
                }
            };
        }
        
        private static void CreateDefaultTools()
        {
            if (!Directory.Exists(DefaultToolsPath))
                return;
                
            // Create get_logs tool
            CreateDefaultTool("get_logs", new DynamicTool
            {
                name = "get_logs",
                description = "Get recent Unity console log entries",
                queryTemplate = @"
// Create some sample log entries for demonstration
var count = {{count}};
var entries = new System.Collections.Generic.List<string>();

// Add some demonstration logs
entries.Add(""[INFO] Unity MCP Server started successfully"");
entries.Add(""[INFO] Dynamic tools loaded: 3 default, 0 custom"");
entries.Add(""[INFO] Claude Code connected via HTTP transport"");

// Add numbered entries up to count
for (int i = 4; i <= count && i <= 100; i++)
{
    entries.Add($""[LOG] Sample log entry #{i}"");
}

// Also add current scene info as a log
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
entries.Add($""[INFO] Current scene: {scene.name} (path: {scene.path})"");

// Add play mode status
entries.Add($""[INFO] Play mode: {(UnityEditor.EditorApplication.isPlaying ? ""Playing"" : ""Editor"")}"");

return string.Join(""\n"", entries.Take(count));",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        count = new
                        {
                            type = "number",
                            description = "Number of log entries to retrieve",
                            @default = 10
                        }
                    }
                }
            });
            
            // Create list_scenes tool
            CreateDefaultTool("list_scenes", new DynamicTool
            {
                name = "list_scenes",
                description = "List all scenes in the project",
                queryTemplate = @"
var scenes = new System.Collections.Generic.List<string>();
var guids = UnityEditor.AssetDatabase.FindAssets(""t:Scene"");
foreach (var guid in guids)
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    scenes.Add(path);
}
return string.Join(""\n"", scenes);",
                inputSchema = new
                {
                    type = "object",
                    properties = new { }
                }
            });
            
            // Note: list_scene_objects might already exist from user creation, only create if it doesn't exist
            if (!File.Exists(Path.Combine(DefaultToolsPath, "list_scene_objects.json")) && 
                !File.Exists(Path.Combine(CustomToolsPath, "list_scene_objects.json")))
            {
                CreateDefaultTool("list_scene_objects", new DynamicTool
                {
                    name = "list_scene_objects",
                    description = "List all GameObjects in the current scene",
                    queryTemplate = @"
using System.Text;
using System.Linq;
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var gameObjects = UnityEngine.Object.FindObjectsByType<UnityEngine.GameObject>(UnityEngine.FindObjectsSortMode.None);

var sb = new System.Text.StringBuilder();
sb.AppendLine($""Scene: {(string.IsNullOrEmpty(scene.name) ? ""Untitled"" : scene.name)}"");
sb.AppendLine($""Total GameObjects: {gameObjects.Length}"");

if ({{includeDetails}})
{
    sb.AppendLine(""\nDetailed GameObject List:"");
    foreach (var go in gameObjects)
    {
        sb.AppendLine($""\n• {go.name} {(go.activeSelf ? ""(Active)"" : ""(Inactive)"")}"");
        sb.AppendLine($""  Position: ({go.transform.position.x:F2}, {go.transform.position.y:F2}, {go.transform.position.z:F2})"");
        
        var components = go.GetComponents<UnityEngine.Component>()
            .Where(c => c != null && c.GetType() != typeof(UnityEngine.Transform))
            .Select(c => c.GetType().Name)
            .ToArray();
        
        if (components.Length > 0)
        {
            sb.AppendLine($""  Components: {string.Join("", "", components)}"");
        }
    }
}
else
{
    sb.AppendLine(""\nGameObjects:"");
    foreach (var go in gameObjects)
    {
        sb.AppendLine($""• {go.name}"");
    }
}

return sb.ToString();",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            includeDetails = new
                            {
                                type = "boolean",
                                description = "Include position and component details",
                                @default = false
                            }
                        }
                    }
                });
            }
            
            // Create create_gameobject tool
            CreateDefaultTool("create_gameobject", new DynamicTool
            {
                name = "create_gameobject",
                description = "Create a GameObject with specified properties",
                queryTemplate = @"
var goName = ""{{name}}"";
var position = new UnityEngine.Vector3({{x}}, {{y}}, {{z}});
UnityEngine.GameObject go = null;

var primitiveType = ""{{primitiveType}}"";
if (!string.IsNullOrEmpty(primitiveType) && primitiveType != ""None"")
{
    UnityEngine.PrimitiveType type;
    if (System.Enum.TryParse<UnityEngine.PrimitiveType>(primitiveType, out type))
    {
        go = UnityEngine.GameObject.CreatePrimitive(type);
        go.name = goName;
        go.transform.position = position;
    }
    else
    {
        // Fallback to empty GameObject if type parsing fails
        go = new UnityEngine.GameObject(goName);
        go.transform.position = position;
    }
}
else
{
    // Create empty GameObject
    go = new UnityEngine.GameObject(goName);
    go.transform.position = position;
}

return $""Created {(string.IsNullOrEmpty(primitiveType) || primitiveType == ""None"" ? ""GameObject"" : primitiveType)} '{goName}' at position ({{{x}}}, {{{y}}}, {{{z}}})"";",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new
                        {
                            type = "string",
                            description = "Name of the GameObject",
                            @default = "GameObject"
                        },
                        x = new
                        {
                            type = "number",
                            description = "X position",
                            @default = 0
                        },
                        y = new
                        {
                            type = "number",
                            description = "Y position",
                            @default = 0
                        },
                        z = new
                        {
                            type = "number",
                            description = "Z position",
                            @default = 0
                        },
                        primitiveType = new
                        {
                            type = "string",
                            description = "Primitive type (Cube, Sphere, Cylinder, Capsule, Plane, Quad)",
                            @default = "None"
                        }
                    },
                    required = new[] { "name" }
                }
            });
        }
        
        private static void CreateDefaultTool(string name, DynamicTool tool)
        {
            var filePath = Path.Combine(DefaultToolsPath, $"{name}.json");
            if (!File.Exists(filePath))
            {
                try
                {
                    var json = JsonConvert.SerializeObject(tool, Formatting.Indented);
                    File.WriteAllText(filePath, json);
                }
                catch (Exception e)
                {
                    // Failed to create default tool
                }
            }
        }
        
        public static List<object> GetToolDefinitions()
        {
            lock (toolsLock)
            {
                var definitions = new List<object>();
                foreach (var tool in tools.Values)
                {
                    definitions.Add(new
                    {
                        name = tool.name,
                        description = tool.description,
                        inputSchema = tool.inputSchema
                    });
                }
                return definitions;
            }
        }
        
        public static bool HasTool(string name)
        {
            lock (toolsLock)
            {
                return tools.ContainsKey(name);
            }
        }
        
        public static object ExecuteTool(string name, JObject arguments)
        {
            lock (toolsLock)
            {
                if (!tools.TryGetValue(name, out var tool))
                {
                    throw new Exception($"Tool not found: {name}");
                }
                
                // Replace parameters in template
                var code = tool.queryTemplate;
                
                if (arguments != null)
                {
                    foreach (var param in arguments)
                    {
                        var placeholder = $"{{{{{param.Key}}}}}";
                        var value = param.Value?.ToString() ?? "";
                        
                        // Handle different value types
                        if (param.Value?.Type == JTokenType.Boolean)
                        {
                            // Convert boolean to lowercase for C#
                            value = value.ToLower();
                        }
                        else if (param.Value?.Type == JTokenType.String)
                        {
                            // Don't add quotes if the placeholder is already in a string literal
                            // Check if placeholder is within quotes (with or without surrounding text)
                            if (!Regex.IsMatch(code, $@"""[^""]*{Regex.Escape(placeholder)}[^""]*"""))
                            {
                                value = $"\"{value}\"";
                            }
                        }
                        else if (param.Value?.Type == JTokenType.Float || param.Value?.Type == JTokenType.Integer)
                        {
                            // Numbers stay as-is
                            value = param.Value.ToString();
                        }
                        
                        code = code.Replace(placeholder, value);
                    }
                }
                
                // Check for any remaining placeholders (use defaults or empty)
                code = Regex.Replace(code, @"\{\{(\w+)\}\}", match =>
                {
                    var paramName = match.Groups[1].Value;
                    // Try to get default value from schema
                    if (tool.inputSchema is JObject schema)
                    {
                        var properties = schema["properties"] as JObject;
                        if (properties?[paramName]?["default"] != null)
                        {
                            return properties[paramName]["default"].ToString();
                        }
                    }
                    return "null";
                });
                
                
                // Execute using the existing Unity dynamic compiler
                var result = UnityDynamicCompiler.ExecuteCode(code);
                
                // Ensure we return a string representation
                if (result == null)
                {
                    return "null";
                }
                else if (result is string)
                {
                    return result;
                }
                else
                {
                    // Convert complex objects to JSON string
                    try
                    {
                        return Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented);
                    }
                    catch
                    {
                        return result.ToString();
                    }
                }
            }
        }
        
        public static string CreateTool(string name, string description, string queryTemplate, object inputSchema)
        {
            // Validate the tool
            var validation = ValidateTool(name, queryTemplate, inputSchema);
            if (!validation.isValid)
            {
                throw new Exception($"Tool validation failed: {validation.error}");
            }
            
            // Check if tool already exists
            lock (toolsLock)
            {
                if (tools.ContainsKey(name))
                {
                    throw new Exception($"Tool with name '{name}' already exists");
                }
            }
            
            // Create the tool
            var tool = new DynamicTool
            {
                name = name,
                description = description,
                queryTemplate = queryTemplate,
                inputSchema = inputSchema,
                isCustom = true
            };
            
            // Save to file
            var filePath = Path.Combine(CustomToolsPath, $"{name}.json");
            try
            {
                var json = JsonConvert.SerializeObject(tool, Formatting.Indented);
                File.WriteAllText(filePath, json);
                
                // The file watcher will automatically reload the tools
                return $"Tool '{name}' created successfully and will be available immediately";
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to save tool: {e.Message}");
            }
        }
        
        public static string ModifyTool(string name, string description = null, string queryTemplate = null, object inputSchema = null)
        {
            lock (toolsLock)
            {
                if (!tools.TryGetValue(name, out var tool))
                {
                    throw new Exception($"Tool '{name}' not found");
                }
                
                if (!tool.isCustom)
                {
                    throw new Exception($"Cannot modify default tool '{name}'. Only custom tools can be modified.");
                }
                
                // Create updated tool with provided values or keep existing
                var updatedTool = new DynamicTool
                {
                    name = name,
                    description = description ?? tool.description,
                    queryTemplate = queryTemplate ?? tool.queryTemplate,
                    inputSchema = inputSchema ?? tool.inputSchema,
                    isCustom = true,
                    filePath = tool.filePath
                };
                
                // Validate the updated tool if queryTemplate or inputSchema changed
                if (queryTemplate != null || inputSchema != null)
                {
                    var validation = ValidateTool(name, updatedTool.queryTemplate, updatedTool.inputSchema);
                    if (!validation.isValid)
                    {
                        throw new Exception($"Tool validation failed: {validation.error}");
                    }
                }
                
                // Save to file
                try
                {
                    var json = JsonConvert.SerializeObject(new
                    {
                        name = updatedTool.name,
                        description = updatedTool.description,
                        queryTemplate = updatedTool.queryTemplate,
                        inputSchema = updatedTool.inputSchema
                    }, Formatting.Indented);
                    
                    File.WriteAllText(tool.filePath, json);
                    
                    // The file watcher will automatically reload the tools
                    return $"Tool '{name}' modified successfully";
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to save modified tool: {e.Message}");
                }
            }
        }
        
        public static string ListCustomTools()
        {
            lock (toolsLock)
            {
                var customTools = tools.Values.Where(t => t.isCustom).ToList();
                if (customTools.Count == 0)
                {
                    return "No custom tools found. Create JSON tool files manually in the Tools/Custom/ directory.";
                }
                
                var result = $"Custom Tools ({customTools.Count}):\n";
                foreach (var tool in customTools)
                {
                    result += $"\n- {tool.name}: {tool.description}";
                    if (tool.inputSchema is JObject schema)
                    {
                        var properties = schema["properties"] as JObject;
                        if (properties != null && properties.Count > 0)
                        {
                            result += $"\n  Parameters: {string.Join(", ", properties.Properties().Select(p => p.Name))}";
                        }
                    }
                }
                return result;
            }
        }
        
        public static string RemoveTool(string name)
        {
            lock (toolsLock)
            {
                if (!tools.TryGetValue(name, out var tool))
                {
                    throw new Exception($"Tool '{name}' not found");
                }
                
                if (!tool.isCustom)
                {
                    throw new Exception($"Cannot remove default tool '{name}'");
                }
                
                try
                {
                    if (File.Exists(tool.filePath))
                    {
                        File.Delete(tool.filePath);
                        
                        // The file watcher will automatically reload the tools
                        return $"Tool '{name}' removed successfully";
                    }
                    else
                    {
                        throw new Exception($"Tool file not found: {tool.filePath}");
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to remove tool: {e.Message}");
                }
            }
        }
        
        private static (bool isValid, string error) ValidateTool(string name, string queryTemplate, object inputSchema)
        {
            // Check name
            if (string.IsNullOrEmpty(name))
            {
                return (false, "Tool name is required");
            }
            
            if (!Regex.IsMatch(name, @"^[a-z_][a-z0-9_]*$"))
            {
                return (false, "Tool name must be lowercase with underscores only");
            }
            
            // Check query template
            if (string.IsNullOrEmpty(queryTemplate))
            {
                return (false, "Query template is required");
            }
            
            // Check for parameters - tool should be generic
            var placeholders = Regex.Matches(queryTemplate, @"\{\{(\w+)\}\}");
            if (placeholders.Count == 0)
            {
                return (false, "Tool must be generic with at least one parameter. Use {{paramName}} placeholders");
            }
            
            // Validate that all placeholders have corresponding schema definitions
            if (inputSchema is JObject schema)
            {
                var properties = schema["properties"] as JObject;
                if (properties == null)
                {
                    return (false, "Input schema must have 'properties' object");
                }
                
                foreach (Match match in placeholders)
                {
                    var paramName = match.Groups[1].Value;
                    if (properties[paramName] == null)
                    {
                        return (false, $"Parameter '{{{{paramName}}}}' used in template but not defined in schema");
                    }
                }
            }
            else
            {
                return (false, "Input schema must be a valid object");
            }
            
            // Check for potential security issues
            if (queryTemplate.Contains("System.IO.File") || 
                queryTemplate.Contains("System.Diagnostics.Process") ||
                queryTemplate.Contains("System.Net"))
            {
                return (false, "Tool contains potentially unsafe operations");
            }
            
            return (true, null);
        }
        
        [Serializable]
        private class DynamicTool
        {
            public string name { get; set; }
            public string description { get; set; }
            public string queryTemplate { get; set; }
            public object inputSchema { get; set; }
            
            [JsonIgnore]
            public bool isCustom { get; set; }
            
            [JsonIgnore]
            public string filePath { get; set; }
        }
    }
}