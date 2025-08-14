using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace UnityMCP.Editor
{
    public class MCPStatusWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool isServerRunning = false;
        private string serverUrl = "";
        private const int PORT = 9876; // Changed from 5001 to avoid conflicts
        private static DateTime lastConnectionTime = DateTime.MinValue;
        private static int connectionCount = 0;
        private static string lastClientInfo = "None";

        [MenuItem("Tools/Unity MCP/Server Status")]
        public static void ShowWindow()
        {
            var window = GetWindow<MCPStatusWindow>("Unity MCP Status");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            CheckServerStatus();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Header
            EditorGUILayout.Space(10);
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Unity MCP Server", headerStyle);
            EditorGUILayout.Space(10);

            // Server Status
            DrawServerStatus();
            
            EditorGUILayout.Space(20);
            
            // Connection Info
            DrawConnectionInfo();
            
            EditorGUILayout.Space(20);
            
            // Actions
            DrawActions();
            
            EditorGUILayout.Space(20);
            
            // Quick Start Guide
            DrawQuickStartGuide();

            EditorGUILayout.EndScrollView();
        }

        private void DrawServerStatus()
        {
            EditorGUILayout.LabelField("Server Status", EditorStyles.boldLabel);
            
            var statusStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            if (isServerRunning)
            {
                statusStyle.normal.textColor = Color.green;
                EditorGUILayout.LabelField("✓ Server is running", statusStyle);
                EditorGUILayout.LabelField($"URL: {serverUrl}");
                
                // Connection status
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Claude Connection", EditorStyles.boldLabel);
                
                if (MCPServer.LastRequestTime != DateTime.MinValue)
                {
                    var timeSinceLastRequest = DateTime.Now - MCPServer.LastRequestTime;
                    
                    if (timeSinceLastRequest.TotalSeconds < 30)
                    {
                        var connectedStyle = new GUIStyle(EditorStyles.label);
                        connectedStyle.normal.textColor = Color.green;
                        EditorGUILayout.LabelField("✓ Claude is connected", connectedStyle);
                    }
                    else if (timeSinceLastRequest.TotalMinutes < 5)
                    {
                        var idleStyle = new GUIStyle(EditorStyles.label);
                        idleStyle.normal.textColor = Color.yellow;
                        EditorGUILayout.LabelField("⚡ Claude is idle", idleStyle);
                    }
                    else
                    {
                        var disconnectedStyle = new GUIStyle(EditorStyles.label);
                        disconnectedStyle.normal.textColor = Color.gray;
                        EditorGUILayout.LabelField("○ Claude not connected", disconnectedStyle);
                    }
                    
                    EditorGUILayout.LabelField($"Last request: {timeSinceLastRequest.TotalSeconds:F0}s ago");
                    EditorGUILayout.LabelField($"Total requests: {MCPServer.RequestCount}");
                    
                    if (!string.IsNullOrEmpty(MCPServer.LastMethod))
                    {
                        EditorGUILayout.LabelField($"Last method: {MCPServer.LastMethod}");
                    }
                }
                else
                {
                    var waitingStyle = new GUIStyle(EditorStyles.label);
                    waitingStyle.normal.textColor = Color.gray;
                    EditorGUILayout.LabelField("○ Waiting for Claude connection", waitingStyle);
                }
            }
            else
            {
                statusStyle.normal.textColor = Color.red;
                EditorGUILayout.LabelField("✗ Server is not running", statusStyle);
            }
        }

        private void DrawConnectionInfo()
        {
            EditorGUILayout.LabelField("Connection Information", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Server Type:", "HTTP/JSON-RPC");
            EditorGUILayout.LabelField("Port:", PORT.ToString());
            EditorGUILayout.LabelField("Host:", "localhost");
            
            if (isServerRunning)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.TextField("Endpoint:", serverUrl);
                
                if (GUILayout.Button("Copy URL"))
                {
                    GUIUtility.systemCopyBuffer = serverUrl;
                    // URL copied to clipboard
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (!isServerRunning)
            {
                if (GUILayout.Button("Start Server", GUILayout.Height(30)))
                {
                    MCPServer.StartServer();
                    CheckServerStatus();
                }
            }
            else
            {
                if (GUILayout.Button("Restart Server", GUILayout.Height(30)))
                {
                    MCPServer.RestartServer();
                    CheckServerStatus();
                }
                
                if (GUILayout.Button("Stop Server", GUILayout.Height(30)))
                {
                    MCPServer.StopServer();
                    CheckServerStatus();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Configure Claude Code", GUILayout.Height(30)))
            {
                ConfigureClaudeCode();
            }
            
            if (GUILayout.Button("Refresh Status", GUILayout.Height(30)))
            {
                CheckServerStatus();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawQuickStartGuide()
        {
            EditorGUILayout.LabelField("Quick Start Guide", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("1. Server Auto-Start", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("The MCP server automatically starts when Unity loads.", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("2. Configure Claude Code", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Run this command in your terminal:", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.Space(3);
            
            // Show command
            string command = $"claude mcp add --transport http unity http://localhost:{PORT}";
            EditorGUILayout.TextArea(command, GUILayout.MinHeight(30));
            
            if (GUILayout.Button("Copy Command"))
            {
                GUIUtility.systemCopyBuffer = command;
                // Command copied to clipboard
            }
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("3. Use in Claude", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("You can now use the 'execute_query' tool to run C# code in Unity:", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.Space(3);
            
            string exampleCode = @"// Example: Create a cube
var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
cube.name = ""MyCube"";
cube.transform.position = new Vector3(0, 1, 0);
return ""Created cube at position (0, 1, 0)"";";
            
            EditorGUILayout.TextArea(exampleCode, GUILayout.MinHeight(80));
            
            EditorGUILayout.EndVertical();
        }

        private void CheckServerStatus()
        {
            try
            {
                // Check if port is in use (server is running)
                using (TcpClient tcpClient = new TcpClient())
                {
                    try
                    {
                        tcpClient.Connect("127.0.0.1", PORT);
                        isServerRunning = true;
                        serverUrl = $"http://localhost:{PORT}";
                        tcpClient.Close();
                    }
                    catch
                    {
                        isServerRunning = false;
                        serverUrl = "";
                    }
                }
            }
            catch
            {
                isServerRunning = false;
                serverUrl = "";
            }
            
            Repaint();
        }

        private void OnInspectorUpdate()
        {
            // Periodically check server status
            CheckServerStatus();
        }
        
        private void ConfigureClaudeCode()
        {
            try
            {
                // Configuring Claude Code with HTTP transport
                
                // Use Claude CLI to add the Unity MCP server with HTTP transport
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "claude",
                        Arguments = $"mcp add --transport http unity http://localhost:{PORT}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                if (process.ExitCode == 0)
                {
                    EditorUtility.DisplayDialog("Success!", 
                        "Claude Code has been configured successfully!\n\n" +
                        "✅ Direct HTTP connection established\n" +
                        "✅ No bridge scripts needed\n" +
                        "✅ Unity MCP server is now available in Claude Code\n\n" +
                        "You can use the 'execute_query' tool to run C# code in Unity.", 
                        "Great!");
                    // Claude Code configured successfully
                }
                else
                {
                    ShowManualConfigInstructions(error);
                }
            }
            catch (Exception e)
            {
                // Configuration error occurred
                ShowManualConfigInstructions(e.Message);
            }
        }
        
        private void ShowManualConfigInstructions(string error)
        {
            string commandLine = $"claude mcp add --transport http unity http://localhost:{PORT}";
            
            string message = "Could not automatically configure Claude Code.\n\n";
            
            if (error.Contains("command not found"))
            {
                message += "The 'claude' command was not found. Please install Claude CLI first.\n\n";
            }
            else if (!string.IsNullOrEmpty(error))
            {
                message += $"Error: {error}\n\n";
            }
            
            message += "To configure manually, run this command in your terminal:\n\n" +
                      commandLine + "\n\n" +
                      "Or if you need to remove and re-add:\n" +
                      "claude mcp remove unity\n" +
                      commandLine;
            
            EditorUtility.DisplayDialog("Manual Configuration", message, "Copy Command");
            
            GUIUtility.systemCopyBuffer = commandLine;
            // Command copied to clipboard
        }
    }
}