using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityMCP.Editor
{
    [InitializeOnLoad]
    public class MCPServer
    {
        private static HttpListener httpListener;
        private static Thread httpListenerThread;
        private static bool isRunning = false;
        private static readonly Queue<RequestContext> requestQueue = new Queue<RequestContext>();
        private static readonly object queueLock = new object();
        private const int PORT = 9876; // Changed from 5001 to avoid conflicts
        
        public static bool IsRunning => isRunning;
        public static DateTime LastRequestTime { get; private set; } = DateTime.MinValue;
        public static int RequestCount { get; private set; } = 0;
        public static string LastMethod { get; private set; } = "";

        static MCPServer()
        {
            // Auto-start the MCP server when Unity loads
            StartServer();
        }

        [MenuItem("Tools/Unity MCP/Restart MCP Server")]
        public static void RestartServer()
        {
            StopServer();
            StartServer();
        }

        [MenuItem("Tools/Unity MCP/Stop MCP Server")]
        public static void StopServer()
        {
            if (!isRunning) return;
            
            isRunning = false;
            
            try
            {
                // Stop the listener first
                if (httpListener != null && httpListener.IsListening)
                {
                    httpListener.Stop();
                    httpListener.Close();
                    httpListener = null;
                }
                
                // Wait for the thread to finish
                if (httpListenerThread != null && httpListenerThread.IsAlive)
                {
                    httpListenerThread.Join(1000); // Wait max 1 second
                    httpListenerThread = null;
                }
            }
            catch (Exception e)
            {
                // Silently handle server stop errors
            }
            
            // Clear any pending requests
            lock (queueLock)
            {
                requestQueue.Clear();
            }
            
            EditorApplication.update -= ProcessQueuedRequests;
        }

        public static void StartServer()
        {
            if (isRunning) return;

            isRunning = true;
            
            // Start HTTP listener thread
            httpListenerThread = new Thread(ListenForRequests)
            {
                IsBackground = true,
                Name = "MCP HTTP Listener"
            };
            httpListenerThread.Start();

            // Process requests on main thread
            EditorApplication.update += ProcessQueuedRequests;
        }

        private static void ListenForRequests()
        {
            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add($"http://localhost:{PORT}/");
                httpListener.Prefixes.Add($"http://127.0.0.1:{PORT}/");
                httpListener.Start();

                while (isRunning)
                {
                    try
                    {
                        if (!httpListener.IsListening)
                        {
                            break; // Exit if listener was stopped
                        }
                        
                        var context = httpListener.GetContext();
                        
                        // Queue the request for processing on main thread
                        lock (queueLock)
                        {
                            requestQueue.Enqueue(new RequestContext { Context = context });
                        }
                    }
                    catch (HttpListenerException e)
                    {
                        if (isRunning && httpListener.IsListening)
                        {
                            // Silently handle HTTP listener exceptions during operation
                        }
                        else
                        {
                            // Listener was stopped, exit gracefully
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        if (isRunning)
                        {
                            // Silently handle general request exceptions during operation
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Silently handle HTTP listener setup errors
            }
            finally
            {
                try
                {
                    httpListener?.Stop();
                    httpListener?.Close();
                }
                catch { }
            }
        }

        private static void ProcessQueuedRequests()
        {
            if (!isRunning) return;

            lock (queueLock)
            {
                while (requestQueue.Count > 0)
                {
                    var requestContext = requestQueue.Dequeue();
                    ProcessHttpRequest(requestContext.Context);
                }
            }
        }

        private class RequestContext
        {
            public HttpListenerContext Context { get; set; }
        }

        private static void ProcessHttpRequest(HttpListenerContext context)
        {
            try
            {
                // Handle CORS preflight
                if (context.Request.HttpMethod == "OPTIONS")
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    context.Response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
                    context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                    context.Response.StatusCode = 200;
                    context.Response.Close();
                    return;
                }
                
                // Track connection
                LastRequestTime = DateTime.Now;
                RequestCount++;
                
                // Read the request body
                string jsonRequest;
                using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                {
                    jsonRequest = reader.ReadToEnd();
                }

                // Process the JSON-RPC request
                var request = JsonConvert.DeserializeObject<MCPRequest>(jsonRequest);
                
                // Track the method being called
                if (request != null && !string.IsNullOrEmpty(request.method))
                {
                    LastMethod = request.method;
                }
                
                var response = HandleRequest(request);
                
                // Send the response
                string jsonResponse = JsonConvert.SerializeObject(response, Formatting.None);
                byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
                
                // Add CORS headers for HTTP transport
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                context.Response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
                context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.StatusCode = 200;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Flush();
                context.Response.Close();
            }
            catch (Exception e)
            {
                var errorResponse = new MCPResponse
                {
                    jsonrpc = "2.0",
                    id = null,
                    error = new MCPError
                    {
                        code = -32603,
                        message = $"Internal error: {e.Message}"
                    }
                };
                
                string errorJson = JsonConvert.SerializeObject(errorResponse, Formatting.None);
                byte[] buffer = Encoding.UTF8.GetBytes(errorJson);
                
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.Close();
            }
        }

        private static bool IsUnityCompiling()
        {
            return EditorApplication.isCompiling || 
                   EditorApplication.isUpdating;
        }

        private static MCPResponse HandleRequest(MCPRequest request)
        {
            // Check if Unity is compiling and return appropriate status
            if (IsUnityCompiling())
            {
                return new MCPResponse 
                { 
                    jsonrpc = "2.0",
                    id = request.id,
                    error = new MCPError 
                    { 
                        code = -32000, // Server error
                        message = "Unity is currently compiling scripts. Please try again in a moment.",
                        data = new { status = "compiling" }
                    }
                };
            }

            switch (request.method)
            {
                case "initialize":
                    return HandleInitialize(request);
                case "tools/list":
                    return HandleListTools(request);
                case "tools/call":
                    return HandleCallTool(request);
                case "resources/list":
                    return HandleListResources(request);
                case "resources/read":
                    return HandleReadResource(request);
                case "server/status":
                    return HandleServerStatus(request);
                default:
                    return new MCPResponse
                    {
                        jsonrpc = "2.0",
                        id = request.id,
                        error = new MCPError
                        {
                            code = -32601,
                            message = $"Method not found: {request.method}"
                        }
                    };
            }
        }

        private static MCPResponse HandleInitialize(MCPRequest request)
        {
            return new MCPResponse
            {
                jsonrpc = "2.0",
                id = request.id,
                result = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new
                    {
                        tools = new { },
                        resources = new { }
                    },
                    serverInfo = new
                    {
                        name = "unity-mcp-server",
                        version = "1.0.0",
                        description = "Unity MCP Server with dynamic tools for Unity Editor automation and scene manipulation."
                    }
                }
            };
        }

        private static MCPResponse HandleListTools(MCPRequest request)
        {
            // Return available tools
            var toolsList = new List<object>();
            
            // Add execute_query tool
            toolsList.Add(new
            {
                name = "execute_query",
                description = "Execute C# code in Unity Editor. Full access to Unity Editor and Runtime APIs for scene manipulation and automation.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        queryCode = new
                        {
                            type = "string",
                            description = "C# code to execute. Should return a value or perform Unity operations. Full access to Unity Editor and Runtime APIs."
                        },
                        parameters = new
                        {
                            type = "object",
                            description = "Optional parameters to pass to the query"
                        }
                    },
                    required = new[] { "queryCode" }
                }
            });
            
            // Add remove_tool for removing custom tools
            toolsList.Add(new
            {
                name = "remove_tool",
                description = "Remove a custom tool (cannot remove default tools)",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new
                        {
                            type = "string",
                            description = "Name of the tool to remove"
                        }
                    },
                    required = new[] { "name" }
                }
            });
            
            // Add all dynamic tools from DynamicToolManager
            var dynamicTools = DynamicToolManager.GetToolDefinitions();
            toolsList.AddRange(dynamicTools);

            return new MCPResponse
            {
                jsonrpc = "2.0",
                id = request.id,
                result = new { tools = toolsList }
            };
        }

        private static MCPResponse HandleCallTool(MCPRequest request)
        {
            string toolName = null;
            try
            {
                var parameters = request.@params;
                toolName = parameters?["name"]?.ToString();
                var arguments = parameters?["arguments"] as JObject;

                if (toolName == "execute_query")
                {
                    string queryCode = arguments?["queryCode"]?.ToString();
                    if (string.IsNullOrEmpty(queryCode))
                    {
                        throw new Exception("queryCode parameter is required");
                    }

                    // Don't check for existing tools - let Claude decide
                    // We only want to suggest creating new tools, not using existing ones

                    var queryParams = arguments?["parameters"]?.ToObject<Dictionary<string, object>>();
                    
                    // Execute directly since we're already on the main thread
                    object result = UnityDynamicCompiler.ExecuteCode(queryCode, queryParams);
                    
                    // Add suggestion to create a tool if the query looks reusable
                    string resultText = result?.ToString() ?? "null";
                    
                 
                    
                    return new MCPResponse
                    {
                        jsonrpc = "2.0",
                        id = request.id,
                        result = new
                        {
                            content = new[]
                            {
                                new
                                {
                                    type = "text",
                                    text = resultText
                                }
                            }
                        }
                    };
                }
                else if (toolName == "remove_tool")
                {
                    string name = arguments?["name"]?.ToString();
                    if (string.IsNullOrEmpty(name))
                    {
                        throw new Exception("Tool name is required");
                    }
                    
                    string result = DynamicToolManager.RemoveTool(name);
                    
                    return new MCPResponse
                    {
                        jsonrpc = "2.0",
                        id = request.id,
                        result = new
                        {
                            content = new[]
                            {
                                new
                                {
                                    type = "text",
                                    text = result
                                }
                            }
                        }
                    };
                }
                else if (DynamicToolManager.HasTool(toolName))
                {
                    // Execute dynamic tool
                    object result = DynamicToolManager.ExecuteTool(toolName, arguments);
                    
                    return new MCPResponse
                    {
                        jsonrpc = "2.0",
                        id = request.id,
                        result = new
                        {
                            content = new[]
                            {
                                new
                                {
                                    type = "text",
                                    text = result?.ToString() ?? "null"
                                }
                            }
                        }
                    };
                }

                throw new Exception($"Unknown tool: {toolName}");
            }
            catch (Exception e)
            {
                // Add helpful guidance for tool failures
                string errorMessage = $"Tool execution failed: {e.Message}";
                
                // Check if it's a compilation error
                if (e.Message.Contains("Compilation errors") || e.Message.Contains("Unexpected symbol"))
                {
                    errorMessage += "\n\nHINT: The code has compilation errors. Check:\n";
                    errorMessage += "1. Syntax errors and missing semicolons\n";
                    errorMessage += "2. Proper namespace imports (using statements)\n";
                    errorMessage += "3. Correct Unity API usage and type names";
                }
                
                return new MCPResponse
                {
                    jsonrpc = "2.0",
                    id = request.id,
                    error = new MCPError
                    {
                        code = -32603,
                        message = errorMessage
                    }
                };
            }
        }

        private static MCPResponse HandleServerStatus(MCPRequest request)
        {
            return new MCPResponse
            {
                jsonrpc = "2.0",
                id = request.id,
                result = new
                {
                    running = isRunning,
                    compiling = EditorApplication.isCompiling,
                    updating = EditorApplication.isUpdating,
                    playing = EditorApplication.isPlaying,
                    paused = EditorApplication.isPaused,
                    requestCount = RequestCount,
                    lastRequestTime = LastRequestTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    lastMethod = LastMethod
                }
            };
        }

        private static MCPResponse HandleListResources(MCPRequest request)
        {
            var resources = new[]
            {
                new
                {
                    uri = "unity://console",
                    name = "Unity Console Logs",
                    mimeType = "text/plain",
                    description = "Real-time Unity console output"
                },
                new
                {
                    uri = "unity://project",
                    name = "Unity Project Info",
                    mimeType = "application/json",
                    description = "Current Unity project information"
                }
            };

            return new MCPResponse
            {
                jsonrpc = "2.0",
                id = request.id,
                result = new { resources = resources }
            };
        }

        private static MCPResponse HandleReadResource(MCPRequest request)
        {
            try
            {
                var parameters = request.@params;
                string uri = parameters?["uri"]?.ToString();

                if (uri == "unity://project")
                {
                    var projectInfo = new
                    {
                        projectName = Application.productName,
                        unityVersion = Application.unityVersion,
                        platform = Application.platform.ToString(),
                        isPlaying = EditorApplication.isPlaying,
                        isPaused = EditorApplication.isPaused
                    };

                    return new MCPResponse
                    {
                        jsonrpc = "2.0",
                        id = request.id,
                        result = new
                        {
                            contents = new[]
                            {
                                new
                                {
                                    uri = uri,
                                    mimeType = "application/json",
                                    text = JsonConvert.SerializeObject(projectInfo, Formatting.Indented)
                                }
                            }
                        }
                    };
                }

                if (uri == "unity://console")
                {
                    return new MCPResponse
                    {
                        jsonrpc = "2.0",
                        id = request.id,
                        result = new
                        {
                            contents = new[]
                            {
                                new
                                {
                                    uri = uri,
                                    mimeType = "text/plain",
                                    text = "Console log capture not yet implemented"
                                }
                            }
                        }
                    };
                }

                throw new Exception($"Unknown resource: {uri}");
            }
            catch (Exception e)
            {
                return new MCPResponse
                {
                    jsonrpc = "2.0",
                    id = request.id,
                    error = new MCPError
                    {
                        code = -32603,
                        message = $"Resource read failed: {e.Message}"
                    }
                };
            }
        }
    }

    [Serializable]
    public class MCPRequest
    {
        public string jsonrpc { get; set; }
        public string method { get; set; }
        public JObject @params { get; set; }
        public object id { get; set; }
    }

    [Serializable]
    public class MCPResponse
    {
        public string jsonrpc { get; set; }
        public object id { get; set; }
        
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object result { get; set; }
        
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public MCPError error { get; set; }
    }

    [Serializable]
    public class MCPError
    {
        public int code { get; set; }
        public string message { get; set; }
        public object data { get; set; }
    }
}