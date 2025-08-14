using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

namespace UnityMCP
{
    /// <summary>
    /// Dynamic C# compiler for Unity that compiles and executes code at runtime
    /// </summary>
    public class DynamicCompiler
    {

        /// <summary>
        /// Compiles and executes a one-time query script using UnityDynamicCompiler
        /// </summary>
        public static object ExecuteQuery(string queryCode, Dictionary<string, object> parameters = null)
        {
            return UnityDynamicCompiler.ExecuteCode(queryCode, parameters);
        }

    }

}