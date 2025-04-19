using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;

namespace RevitRagAgent
{
    public class RagProcessor
    {
        private UIApplication _uiApp;
        private DocumentEmbeddings _documentEmbeddings;
        private string _apiKey;
        private Settings _settings;
        
        public RagProcessor(UIApplication uiApp, DocumentEmbeddings documentEmbeddings, Settings settings)
        {
            _uiApp = uiApp;
            _documentEmbeddings = documentEmbeddings;
            _settings = settings;
            _apiKey = settings.ApiKey;
        }
        
        public async Task<string> ProcessQuery(string question, string selectedContext)
        {
            // 1. Retrieve relevant documents
            List<DocumentChunk> relevantChunks = _documentEmbeddings.RetrieveRelevantDocuments(question, selectedContext);
            if (relevantChunks.Count == 0)
            {
                // No relevant documents found, try to answer based on Revit current state
                return GenerateRevitContextAnswer(question);
            }

            // 2. Format the context from retrieved documents
            string context = FormatContext(relevantChunks);

            // 3. Get Revit-specific context if needed
            string revitContext = GetRevitContext(question);
            if (!string.IsNullOrEmpty(revitContext))
            {
                context += "\n\nCurrent Revit Context:\n" + revitContext;
            }

            // 4. Generate answer using the language model API
            return await GenerateResponse(question, context, relevantChunks);
        }
        
        public async Task<string> ProcessScriptQuery(string scriptPrompt, string selectedContext)
        {
            // For script queries, we should retrieve relevant documentation but focus on the script prompt
            List<DocumentChunk> relevantChunks = _documentEmbeddings.RetrieveRelevantDocuments(scriptPrompt, selectedContext);
            
            // Format the context from retrieved documents
            string context = FormatContext(relevantChunks);
            
            // Get Revit-specific context
            string revitContext = GetRevitContext(scriptPrompt);
            if (!string.IsNullOrEmpty(revitContext))
            {
                context += "\n\nCurrent Revit Context:\n" + revitContext;
            }
            
            // Generate script using the language model API
            return await GenerateScript(scriptPrompt, context, relevantChunks);
        }
        
        private string FormatContext(List<DocumentChunk> chunks)
        {
            StringBuilder context = new StringBuilder();
            
            foreach (var chunk in chunks)
            {
                context.AppendLine($"Source: {chunk.Source}");
                context.AppendLine(chunk.Text);
                context.AppendLine();
            }
            
            return context.ToString();
        }
        
        private string GetRevitContext(string question)
        {
            // Check if the question is about the current Revit model or elements
            bool isAboutCurrentModel = question.ToLower().Contains("current") || 
                                      question.ToLower().Contains("model") ||
                                      question.ToLower().Contains("project") ||
                                      question.ToLower().Contains("document");

            if (!isAboutCurrentModel)
            {
                return string.Empty;
            }

            StringBuilder context = new StringBuilder();
            Document doc = _uiApp.ActiveUIDocument.Document;
            
            try
            {
                // Get basic document information
                context.AppendLine($"Document Title: {doc.Title}");
                context.AppendLine($"File Path: {doc.PathName}");
                
                // Count elements by category
                Dictionary<string, int> elementCounts = new Dictionary<string, int>();
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                collector.WhereElementIsNotElementType();
                
                foreach (Element element in collector)
                {
                    Category category = element.Category;
                    if (category != null)
                    {
                        string categoryName = category.Name;
                        if (elementCounts.ContainsKey(categoryName))
                        {
                            elementCounts[categoryName]++;
                        }
                        else
                        {
                            elementCounts[categoryName] = 1;
                        }
                    }
                }
                
                context.AppendLine("\nElement Counts by Category:");
                foreach (var kvp in elementCounts.OrderByDescending(x => x.Value).Take(10))
                {
                    context.AppendLine($"  {kvp.Key}: {kvp.Value}");
                }
            }
            catch (Exception ex)
            {
                context.AppendLine($"Error retrieving Revit context: {ex.Message}");
            }
            
            return context.ToString();
        }
        
        private string GenerateRevitContextAnswer(string question)
        {
            // This method handles queries about the current Revit document using the API
            try
            {
                Document doc = _uiApp.ActiveUIDocument.Document;
                
                if (question.ToLower().Contains("how many walls"))
                {
                    // Count walls in the document
                    FilteredElementCollector collector = new FilteredElementCollector(doc);
                    collector.OfCategory(BuiltInCategory.OST_Walls);
                    int wallCount = collector.Count();
                    return $"There are {wallCount} walls in the current document.";
                }
                else if (question.ToLower().Contains("how many levels"))
                {
                    // Count levels in the document
                    FilteredElementCollector collector = new FilteredElementCollector(doc);
                    collector.OfCategory(BuiltInCategory.OST_Levels);
                    int levelCount = collector.Count();
                    return $"There are {levelCount} levels in the current document.";
                }
                else if (question.ToLower().Contains("how many rooms") || question.ToLower().Contains("room count"))
                {
                    // Count rooms in the document
                    FilteredElementCollector collector = new FilteredElementCollector(doc);
                    collector.OfCategory(BuiltInCategory.OST_Rooms);
                    int roomCount = collector.Count();
                    return $"There are {roomCount} rooms in the current document.";
                }
                else if (question.ToLower().Contains("list levels") || question.ToLower().Contains("level names"))
                {
                    // List all level names
                    FilteredElementCollector collector = new FilteredElementCollector(doc);
                    collector.OfCategory(BuiltInCategory.OST_Levels);
                    
                    StringBuilder levels = new StringBuilder();
                    levels.AppendLine("The levels in this project are:");
                    
                    foreach (Element element in collector)
                    {
                        Level level = element as Level;
                        if (level != null)
                        {
                            levels.AppendLine($"- {level.Name} (Elevation: {level.Elevation})");
                        }
                    }
                    
                    return levels.ToString();
                }
                
                return "I couldn't find specific information to answer your question about the current Revit document. Please try to be more specific about what you're looking for.";
            }
            catch (Exception ex)
            {
                return $"Error accessing Revit document: {ex.Message}";
            }
        }
        
        private async Task<string> GenerateResponse(string question, string context, List<DocumentChunk> relevantChunks)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Get provider settings from the Settings object
                    string apiProvider = _settings.LlmProvider.ToLower();
                    string apiEndpoint;
                    object requestData;
                    
                    // Format request based on selected provider
                    switch (apiProvider)
                    {
                        case "openai":
                            apiEndpoint = "https://api.openai.com/v1/chat/completions";
                            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                            
                            requestData = new
                            {
                                model = _settings.ModelName, // e.g., "gpt-4" or "gpt-3.5-turbo"
                                messages = new[]
                                {
                                    new { role = "system", content = "You are a Revit assistant that answers questions based on provided context. Answer only based on the context provided." },
                                    new { role = "user", content = $"Context:\n{context}\n\nQuestion: {question}" }
                                },
                                temperature = _settings.Temperature,
                                max_tokens = _settings.MaxTokens
                            };
                            break;
                            
                        case "anthropic":
                            apiEndpoint = "https://api.anthropic.com/v1/messages";
                            client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                            
                            requestData = new
                            {
                                model = _settings.ModelName, // e.g., "claude-3-opus-20240229"
                                messages = new[]
                                {
                                    new { role = "user", content = $"You are a Revit assistant that answers questions based on provided context. Answer only based on the context provided.\n\nContext:\n{context}\n\nQuestion: {question}" }
                                },
                                temperature = _settings.Temperature,
                                max_tokens = _settings.MaxTokens
                            };
                            break;
                            
                        case "gemini":
                            apiEndpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.ModelName}:generateContent?key={_apiKey}";
                            
                            requestData = new
                            {
                                contents = new[]
                                {
                                    new
                                    {
                                        parts = new[]
                                        {
                                            new { text = $"You are a Revit assistant that answers questions based on provided context. Answer only based on the context provided.\n\nContext:\n{context}\n\nQuestion: {question}" }
                                        }
                                    }
                                },
                                generationConfig = new
                                {
                                    temperature = _settings.Temperature,
                                    maxOutputTokens = _settings.MaxTokens
                                }
                            };
                            break;
                            
                        default:
                            return "Error: Unknown LLM provider selected in settings. Please select OpenAI, Anthropic, or Gemini.";
                    }
                    
                    var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(apiEndpoint, content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        dynamic responseObj = JsonConvert.DeserializeObject(responseString);
                        string generatedContent = "";
                        
                        // Extract content based on provider format
                        switch (apiProvider)
                        {
                            case "openai":
                                generatedContent = responseObj.choices[0].message.content;
                                break;
                                
                            case "anthropic":
                                generatedContent = responseObj.content[0].text;
                                break;
                                
                            case "gemini":
                                generatedContent = responseObj.candidates[0].content.parts[0].text;
                                break;
                        }
                        
                        // Add source citations at the end if not included by LLM
                        if (!generatedContent.Contains("Sources:") && relevantChunks.Count > 0)
                        {
                            generatedContent += "\n\nSources:\n";
                            foreach (var chunk in LinqExtensions.DistinctBy(relevantChunks, c => c.Source))
                            {
                                generatedContent += $"- {chunk.Source}\n";
                            }
                        }
                        
                        return generatedContent;
                    }
                    else
                    {
                        return $"Error calling LLM API: {responseString}";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error generating response: {ex.Message}";
            }
        }
        
        private async Task<string> GenerateScript(string prompt, string context, List<DocumentChunk> relevantChunks)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Get provider settings from the Settings object
                    string apiProvider = _settings.LlmProvider.ToLower();
                    string apiEndpoint;
                    object requestData;
                    
                    // Format request based on selected provider
                    switch (apiProvider)
                    {
                        case "openai":
                            apiEndpoint = "https://api.openai.com/v1/chat/completions";
                            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                            
                            requestData = new
                            {
                                model = _settings.ModelName, // e.g., "gpt-4" or "gpt-3.5-turbo"
                                messages = new[]
                                {
                                    new { role = "system", content = "You are a Revit API expert that generates C# code for Revit operations. Write only executable code that works with the Revit API." },
                                    new { role = "user", content = $"Context (Revit info):\n{context}\n\nTask: {prompt}" }
                                },
                                temperature = 0.2, // Lower temperature for code generation
                                max_tokens = _settings.MaxTokens
                            };
                            break;
                            
                        case "anthropic":
                            apiEndpoint = "https://api.anthropic.com/v1/messages";
                            client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                            
                            requestData = new
                            {
                                model = _settings.ModelName, // e.g., "claude-3-opus-20240229"
                                messages = new[]
                                {
                                    new { role = "user", content = $"You are a Revit API expert that generates C# code for Revit operations. Write only executable code that works with the Revit API.\n\nContext (Revit info):\n{context}\n\nTask: {prompt}" }
                                },
                                temperature = 0.2, // Lower temperature for code generation
                                max_tokens = _settings.MaxTokens
                            };
                            break;
                            
                        case "gemini":
                            apiEndpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.ModelName}:generateContent?key={_apiKey}";
                            
                            requestData = new
                            {
                                contents = new[]
                                {
                                    new
                                    {
                                        parts = new[]
                                        {
                                            new { text = $"You are a Revit API expert that generates C# code for Revit operations. Write only executable code that works with the Revit API.\n\nContext (Revit info):\n{context}\n\nTask: {prompt}" }
                                        }
                                    }
                                },
                                generationConfig = new
                                {
                                    temperature = 0.2, // Lower temperature for code generation
                                    maxOutputTokens = _settings.MaxTokens
                                }
                            };
                            break;
                            
                        default:
                            return "Error: Unknown LLM provider selected in settings. Please select OpenAI, Anthropic, or Gemini.";
                    }
                    
                    var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(apiEndpoint, content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        dynamic responseObj = JsonConvert.DeserializeObject(responseString);
                        string generatedContent = "";
                        
                        // Extract content based on provider format
                        switch (apiProvider)
                        {
                            case "openai":
                                generatedContent = responseObj.choices[0].message.content;
                                break;
                                
                            case "anthropic":
                                generatedContent = responseObj.content[0].text;
                                break;
                                
                            case "gemini":
                                generatedContent = responseObj.candidates[0].content.parts[0].text;
                                break;
                        }
                        
                        return generatedContent;
                    }
                    else
                    {
                        return $"Error calling LLM API: {responseString}";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error generating script: {ex.Message}";
            }
        }
    }
}