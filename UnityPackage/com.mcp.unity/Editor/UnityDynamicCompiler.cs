using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Linq;

namespace UnityMCP
{
    /// <summary>
    /// Unity-specific dynamic compiler that uses Unity's compilation system
    /// </summary>
    public static class UnityDynamicCompiler
    {
        private static System.Reflection.Assembly executionAssembly;
        
        static UnityDynamicCompiler()
        {
            InitializeCompiler();
        }

        private static void InitializeCompiler()
        {
            // Get the current assembly context
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            executionAssembly = assemblies.FirstOrDefault(a => a.GetName().Name.Contains("Assembly-CSharp"));
            
            if (executionAssembly == null)
            {
                executionAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            }
            
        }

        /// <summary>
        /// Executes C# code dynamically using Unity's compilation system
        /// </summary>
        public static object ExecuteCode(string code, Dictionary<string, object> parameters = null)
        {
            try
            {
                string className = $"DynamicExecution_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                
                // Wrap the code in a proper class structure
                string fullCode = WrapCodeInClass(code, className);

                
                // Compile using Unity's system
                var assembly = CompileCodeToAssembly(fullCode, className);
                
                // Execute the code
                return ExecuteFromAssembly(assembly, className, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityDynamicCompiler] Execution failed: {ex.Message}");
                Debug.LogError($"[UnityDynamicCompiler] Stack trace: {ex.StackTrace}");
                throw new Exception($"Dynamic code execution failed: {ex.Message}", ex);
            }
        }

        private static string WrapCodeInClass(string code, string className)
        {
            string processedCode = code.Trim();
            
            // Extract using statements if they exist at the beginning
            var lines = processedCode.Split('\n');
            var usingStatements = new List<string>();
            var codeLines = new List<string>();
            bool inUsings = true;
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (inUsings && (trimmedLine.StartsWith("using ") && trimmedLine.EndsWith(";")))
                {
                    usingStatements.Add(trimmedLine);
                }
                else
                {
                    inUsings = false;
                    codeLines.Add(line);
                }
            }
            
            // Rebuild code without using statements
            string codeBody = string.Join("\n", codeLines).Trim();
            
            // Check if the code already has a return statement
            bool hasReturn = codeBody.Contains("return ");
            
            // If it's a simple expression without return, add return
            if (!hasReturn && !codeBody.Contains(";") && !codeBody.Contains("{"))
            {
                codeBody = $"return {codeBody};";
                hasReturn = true;
            }
            
            // Build the method body
            string methodBody;
            if (hasReturn)
            {
                // Code has explicit return, use as-is
                methodBody = codeBody;
            }
            else
            {
                // Code doesn't have return, execute it and return success message
                methodBody = codeBody + @"
                
                // If no explicit return, return success message
                return ""Execution completed successfully"";";
            }
            
            // Combine default and extracted using statements
            var allUsings = new HashSet<string>
            {
                "using System;",
                "using System.Collections.Generic;",
                "using System.Linq;",
                "using UnityEngine;",
                "using UnityEditor;"
            };
            
            foreach (var usingStmt in usingStatements)
            {
                allUsings.Add(usingStmt);
            }
            
            string usingsBlock = string.Join("\n", allUsings);
            
            return $@"
{usingsBlock}

namespace MCPDynamic
{{
    public class {className}
    {{
        public static object ExecuteCode(Dictionary<string, object> parameters = null)
        {{
            try 
            {{
                {methodBody}
            }}
            catch (Exception ex)
            {{
                UnityEngine.Debug.LogError($""Dynamic execution error: {{ex.Message}}"");
                throw;
            }}
        }}
    }}
}}";
        }

        private static System.Reflection.Assembly CompileCodeToAssembly(string code, string className)
        {
            try
            {
                // Try to use System.CodeDom.Compiler if available
                return TryCompileWithCodeDom(code, className);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityDynamicCompiler] Compilation failed: {ex.Message}");
                throw new Exception($"Failed to compile dynamic code: {ex.Message}", ex);
            }
        }

        private static System.Reflection.Assembly TryCompileWithCodeDom(string code, string className)
        {
            // Use reflection to access CodeDom compiler
            Type codeProviderType = GetTypeFromAssemblies("Microsoft.CSharp.CSharpCodeProvider") ??
                                  GetTypeFromAssemblies("System.CodeDom.Compiler.CodeDomProvider");
            
            if (codeProviderType == null)
            {
                throw new Exception("No suitable compiler found");
            }

            // Create compiler instance
            object codeProvider = Activator.CreateInstance(codeProviderType);
            
            // Create compiler parameters
            Type compilerParamsType = GetTypeFromAssemblies("System.CodeDom.Compiler.CompilerParameters");
            if (compilerParamsType == null)
            {
                throw new Exception("CompilerParameters type not found");
            }
            
            object compilerParams = Activator.CreateInstance(compilerParamsType);
            
            // Set compiler parameters using reflection
            var generateInMemoryProp = compilerParamsType.GetProperty("GenerateInMemory");
            generateInMemoryProp?.SetValue(compilerParams, true);
            
            var generateExecutableProp = compilerParamsType.GetProperty("GenerateExecutable");
            generateExecutableProp?.SetValue(compilerParams, false);
            
            // Add assembly references
            var referencedAssembliesProperty = compilerParamsType.GetProperty("ReferencedAssemblies");
            if (referencedAssembliesProperty?.GetValue(compilerParams) is System.Collections.IList refAssemblies)
            {
                AddRequiredAssemblies(refAssemblies);
            }
            
            // Compile
            var compileMethod = codeProviderType.GetMethod("CompileAssemblyFromSource");
            if (compileMethod == null)
            {
                throw new Exception("CompileAssemblyFromSource method not found");
            }
            
            // CompileAssemblyFromSource expects string[] not string
            object results = compileMethod.Invoke(codeProvider, new object[] { compilerParams, new string[] { code } });
            
            // Check for errors
            var errorsProperty = results.GetType().GetProperty("Errors");
            if (errorsProperty?.GetValue(results) is System.Collections.ICollection errors && errors.Count > 0)
            {
                var hasErrorsProperty = errors.GetType().GetProperty("HasErrors");
                if (hasErrorsProperty != null && (bool)hasErrorsProperty.GetValue(errors))
                {
                    var errorMessages = new StringBuilder();
                    foreach (var error in errors)
                    {
                        var errorTextProperty = error.GetType().GetProperty("ErrorText");
                        var lineProperty = error.GetType().GetProperty("Line");
                        if (errorTextProperty != null && lineProperty != null)
                        {
                            errorMessages.AppendLine($"Line {lineProperty.GetValue(error)}: {errorTextProperty.GetValue(error)}");
                        }
                    }
                    throw new Exception($"Compilation errors:\n{errorMessages}");
                }
            }
            
            // Get compiled assembly
            var compiledAssemblyProperty = results.GetType().GetProperty("CompiledAssembly");
            if (compiledAssemblyProperty?.GetValue(results) is System.Reflection.Assembly assembly)
            {
                return assembly;
            }
            
            throw new Exception("Failed to get compiled assembly");
        }

        private static void AddRequiredAssemblies(System.Collections.IList refAssemblies)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var addedAssemblies = new System.Collections.Generic.HashSet<string>();
            
            // First, try to find and add netstandard explicitly
            foreach (var assembly in assemblies)
            {
                if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
                    continue;
                    
                string name = assembly.GetName().Name;
                if (name.Contains("netstandard") || name.Contains("System.Runtime"))
                {
                    try
                    {
                        if (!addedAssemblies.Contains(assembly.Location))
                        {
                            refAssemblies.Add(assembly.Location);
                            addedAssemblies.Add(assembly.Location);
                                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Could not add assembly {name}: {ex.Message}");
                    }
                }
            }
            
            // Add all other required assemblies
            foreach (var assembly in assemblies)
            {
                if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
                    continue;
                    
                if (addedAssemblies.Contains(assembly.Location))
                    continue;
                    
                string name = assembly.GetName().Name;
                if (name.StartsWith("UnityEngine") || 
                    name.StartsWith("UnityEditor") ||
                    name.StartsWith("System") ||
                    name.StartsWith("mscorlib") ||
                    name.Contains("Newtonsoft") ||
                    name.Contains("UnityMCP") ||
                    name.Contains("netstandard"))
                {
                    try
                    {
                        refAssemblies.Add(assembly.Location);
                        addedAssemblies.Add(assembly.Location);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Could not add assembly {name}: {ex.Message}");
                    }
                }
            }
            
            // Try to manually add netstandard if not found
            if (!addedAssemblies.Any(a => a.Contains("netstandard")))
            {
                // Common Unity netstandard paths
                var possiblePaths = new[]
                {
                    "/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/NetStandard/ref/2.1.0/netstandard.dll",
                    "/Applications/Unity/Unity.app/Contents/NetStandard/ref/2.1.0/netstandard.dll",
                    Path.Combine(EditorApplication.applicationPath, "../NetStandard/ref/2.1.0/netstandard.dll"),
                    Path.Combine(EditorApplication.applicationContentsPath, "NetStandard/ref/2.1.0/netstandard.dll")
                };
                
                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        try
                        {
                            refAssemblies.Add(path);
                                            break;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Could not add netstandard from {path}: {ex.Message}");
                        }
                    }
                }
            }
        }

        private static Type GetTypeFromAssemblies(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var type = assembly.GetType(typeName);
                    if (type != null) return type;
                }
                catch
                {
                    // Continue searching
                }
            }
            return null;
        }

        private static object ExecuteFromAssembly(System.Reflection.Assembly assembly, string className, Dictionary<string, object> parameters)
        {
            var type = assembly.GetType($"MCPDynamic.{className}");
            if (type == null)
            {
                throw new Exception($"Could not find type MCPDynamic.{className}");
            }
            
            var method = type.GetMethod("ExecuteCode", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (method == null)
            {
                throw new Exception($"Could not find ExecuteCode method in {className}");
            }
            
            return method.Invoke(null, new object[] { parameters });
        }
    }
}