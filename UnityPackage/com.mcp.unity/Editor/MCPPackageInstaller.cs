using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace UnityMCP.Editor
{
    [InitializeOnLoad]
    public static class MCPPackageInstaller
    {
        private static AddRequest addRequest;
        private const string FIRST_RUN_KEY = "UnityMCP_FirstRun";
        
        static MCPPackageInstaller()
        {
            // Check if this is the first run
            if (!EditorPrefs.HasKey(FIRST_RUN_KEY))
            {
                EditorPrefs.SetBool(FIRST_RUN_KEY, true);
                EditorApplication.delayCall += OnFirstRun;
            }
        }

        private static void OnFirstRun()
        {
            // Unity MCP Bridge initializing
            
            // Check if Newtonsoft.Json is installed
            CheckNewtonsoft();
            
            // Show setup window
            EditorApplication.delayCall += () =>
            {
                if (EditorUtility.DisplayDialog("Unity MCP Bridge", 
                    "Welcome to Unity MCP Bridge!\n\n" +
                    "This package enables Claude Code to interact with Unity Editor.\n\n" +
                    "The MCP server will start automatically.\n\n" +
                    "Would you like to open the Server Status window?", 
                    "Open Status", "Later"))
                {
                    MCPStatusWindow.ShowWindow();
                }
            };
        }

        private static void CheckNewtonsoft()
        {
            // Check if Newtonsoft.Json is already available
            var newtonsoftType = Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json");
            if (newtonsoftType == null)
            {
                // Installing Newtonsoft.Json dependency
                addRequest = Client.Add("com.unity.nuget.newtonsoft-json");
                EditorApplication.update += CheckNewtonsoftProgress;
            }
        }

        private static void CheckNewtonsoftProgress()
        {
            if (addRequest.IsCompleted)
            {
                EditorApplication.update -= CheckNewtonsoftProgress;
                
                if (addRequest.Status == StatusCode.Success)
                {
                    // Newtonsoft.Json installed successfully
                }
                else if (addRequest.Status == StatusCode.Failure)
                {
                    // Failed to install Newtonsoft.Json dependency
                }
            }
        }
    }
}