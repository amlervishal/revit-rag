using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;

namespace RevitRagAgent
{
    public class RevitScriptExecutor
    {
        private UIApplication _uiApp;
        private Settings _settings;
        
        public RevitScriptExecutor(UIApplication uiApp, Settings settings)
        {
            _uiApp = uiApp;
            _settings = settings;
        }
        
        /// <summary>
        /// Extracts C# code blocks from LLM response
        /// </summary>
        public string ExtractCodeFromResponse(string llmResponse)
        {
            // Look for C# code blocks in markdown format (```csharp...```)
            string pattern = @"```csharp\s*([\s\S]*?)```";
            
            Match match = Regex.Match(llmResponse, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
            
            // If no specific csharp block, try generic code block
            pattern = @"```\s*([\s\S]*?)```";
            match = Regex.Match(llmResponse, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
            
            return null;
        }
        
        /// <summary>
        /// Generates a full class wrapper around provided code snippet 
        /// to make it compatible with Revit API execution
        /// </summary>
        public string PrepareExecutableCode(string codeSnippet)
        {
            StringBuilder codeBuilder = new StringBuilder();
            
            // Add necessary imports
            codeBuilder.AppendLine("using System;");
            codeBuilder.AppendLine("using System.Collections.Generic;");
            codeBuilder.AppendLine("using System.Linq;");
            codeBuilder.AppendLine("using Autodesk.Revit.DB;");
            codeBuilder.AppendLine("using Autodesk.Revit.UI;");
            codeBuilder.AppendLine("using Autodesk.Revit.DB.Architecture;");
            codeBuilder.AppendLine("using Autodesk.Revit.DB.Mechanical;");
            codeBuilder.AppendLine("using Autodesk.Revit.DB.Electrical;");
            codeBuilder.AppendLine("using Autodesk.Revit.DB.Plumbing;");
            codeBuilder.AppendLine("using Autodesk.Revit.DB.Structure;");
            codeBuilder.AppendLine();
            
            // Create script class
            codeBuilder.AppendLine("namespace RevitRagAgent.DynamicScript");
            codeBuilder.AppendLine("{");
            codeBuilder.AppendLine("    public class ScriptRunner");
            codeBuilder.AppendLine("    {");
            codeBuilder.AppendLine("        public static string Execute(UIApplication uiApp)");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            Document doc = uiApp.ActiveUIDocument.Document;");
            codeBuilder.AppendLine("            UIDocument uidoc = uiApp.ActiveUIDocument;");
            codeBuilder.AppendLine("            StringBuilder logBuilder = new StringBuilder();");
            codeBuilder.AppendLine();
            
            // Check if the code includes a transaction - if not, add one
            if (!codeSnippet.Contains("Transaction") && 
                !codeSnippet.Contains("SubTransaction") && 
                !codeSnippet.Contains("TransactionGroup"))
            {
                codeBuilder.AppendLine("            using (Transaction tx = new Transaction(doc, \"RAG Script\"))");
                codeBuilder.AppendLine("            {");
                codeBuilder.AppendLine("                try");
                codeBuilder.AppendLine("                {");
                codeBuilder.AppendLine("                    tx.Start();");
                codeBuilder.AppendLine();
                
                // Indent the provided code
                string[] lines = codeSnippet.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    codeBuilder.AppendLine("                    " + line);
                }
                
                codeBuilder.AppendLine();
                codeBuilder.AppendLine("                    tx.Commit();");
                codeBuilder.AppendLine("                    logBuilder.AppendLine(\"Script executed successfully.\");");
                codeBuilder.AppendLine("                }");
                codeBuilder.AppendLine("                catch (Exception ex)");
                codeBuilder.AppendLine("                {");
                codeBuilder.AppendLine("                    if (tx.HasStarted() && !tx.HasEnded())");
                codeBuilder.AppendLine("                        tx.RollBack();");
                codeBuilder.AppendLine("                    logBuilder.AppendLine($\"Error: {ex.Message}\");");
                codeBuilder.AppendLine("                    logBuilder.AppendLine($\"Stack trace: {ex.StackTrace}\");");
                codeBuilder.AppendLine("                }");
                codeBuilder.AppendLine("            }");
            }
            else
            {
                // If the code already includes transaction handling, just include it as-is
                codeBuilder.AppendLine("            try");
                codeBuilder.AppendLine("            {");
                
                // Indent the provided code
                string[] lines = codeSnippet.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    codeBuilder.AppendLine("                " + line);
                }
                
                codeBuilder.AppendLine("                logBuilder.AppendLine(\"Script executed successfully.\");");
                codeBuilder.AppendLine("            }");
                codeBuilder.AppendLine("            catch (Exception ex)");
                codeBuilder.AppendLine("            {");
                codeBuilder.AppendLine("                logBuilder.AppendLine($\"Error: {ex.Message}\");");
                codeBuilder.AppendLine("                logBuilder.AppendLine($\"Stack trace: {ex.StackTrace}\");");
                codeBuilder.AppendLine("            }");
            }
            
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("            return logBuilder.ToString();");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine("    }");
            codeBuilder.AppendLine("}");
            
            return codeBuilder.ToString();
        }
        
        /// <summary>
        /// Compiles and executes the provided code in the context of the current Revit session
        /// </summary>
        public string CompileAndExecuteCode(string code)
        {
            try
            {
                // Create the compiler
                CSharpCodeProvider provider = new CSharpCodeProvider();
                CompilerParameters parameters = new CompilerParameters();
                
                // Add necessary references
                parameters.ReferencedAssemblies.Add("System.dll");
                parameters.ReferencedAssemblies.Add("System.Core.dll");
                parameters.ReferencedAssemblies.Add(typeof(object).Assembly.Location);
                parameters.ReferencedAssemblies.Add(typeof(Enumerable).Assembly.Location);
                parameters.ReferencedAssemblies.Add(typeof(Document).Assembly.Location); // RevitAPI.dll
                parameters.ReferencedAssemblies.Add(typeof(UIApplication).Assembly.Location); // RevitAPIUI.dll
                parameters.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location); // Current assembly
                
                // Keep the assembly in memory
                parameters.GenerateInMemory = true;
                parameters.CompilerOptions = "/optimize";
                
                // Compile the code
                CompilerResults results = provider.CompileAssemblyFromSource(parameters, code);
                
                if (results.Errors.HasErrors)
                {
                    StringBuilder errorBuilder = new StringBuilder();
                    errorBuilder.AppendLine("Compilation errors:");
                    
                    foreach (CompilerError error in results.Errors)
                    {
                        errorBuilder.AppendLine($"Line {error.Line}: {error.ErrorText}");
                    }
                    
                    return errorBuilder.ToString();
                }
                
                // Get the compiled assembly and execute the method
                Assembly assembly = results.CompiledAssembly;
                Type scriptType = assembly.GetType("RevitRagAgent.DynamicScript.ScriptRunner");
                
                if (scriptType != null)
                {
                    MethodInfo executeMethod = scriptType.GetMethod("Execute");
                    if (executeMethod != null)
                    {
                        // Execute the method and return its result
                        string result = (string)executeMethod.Invoke(null, new object[] { _uiApp });
                        return result;
                    }
                }
                
                return "Error: Could not find or execute the script method.";
            }
            catch (Exception ex)
            {
                return $"Error compiling and executing code: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Generates, prepares, and executes a script from an LLM response
        /// </summary>
        public string ExecuteScriptFromResponse(string llmResponse)
        {
            // Extract the code from the LLM response
            string extractedCode = ExtractCodeFromResponse(llmResponse);
            
            if (string.IsNullOrWhiteSpace(extractedCode))
            {
                return "No executable code found in the response.";
            }
            
            // Prepare the code for execution
            string executableCode = PrepareExecutableCode(extractedCode);
            
            // Compile and execute the code
            return CompileAndExecuteCode(executableCode);
        }
        
        /// <summary>
        /// Modifies the original query to ask for executable Revit code
        /// </summary>
        public string PrepareScriptGenerationPrompt(string originalQuestion)
        {
            StringBuilder promptBuilder = new StringBuilder();
            
            promptBuilder.AppendLine($"I need C# code to execute in Revit API to perform this task: {originalQuestion}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Please write valid, runnable C# code that uses the Revit API to accomplish this task.");
            promptBuilder.AppendLine("Focus on writing code that:");
            promptBuilder.AppendLine("1. Is complete, concise, and ready to execute");
            promptBuilder.AppendLine("2. Only includes essential functionality to accomplish the task");
            promptBuilder.AppendLine("3. Uses proper error handling");
            promptBuilder.AppendLine("4. Provides informative comments");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Return ONLY the C# code wrapped in ```csharp code blocks. Do not include explanations outside the code block.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Important Revit API context:");
            promptBuilder.AppendLine("- Document doc = the active document");
            promptBuilder.AppendLine("- UIDocument uidoc = the active UI document");
            promptBuilder.AppendLine("- Use FilteredElementCollector to query elements");
            promptBuilder.AppendLine("- All model modifications must be within a Transaction");
            
            return promptBuilder.ToString();
        }
    }
}