using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace RevitRagAgent
{
    public class DocumentChunk
    {
        public string Text { get; set; }
        public string Source { get; set; }
        public List<float> Embedding { get; set; }
        public string Context { get; set; }
    }

    public class DocumentEmbeddings
    {
        private List<DocumentChunk> _documentChunks;
        private string _embeddingsPath;

        public DocumentEmbeddings(string embeddingsPath)
        {
            _embeddingsPath = embeddingsPath;
            LoadEmbeddings();
        }

        public List<DocumentChunk> GetAllChunks()
        {
            return _documentChunks;
        }

        public List<DocumentChunk> GetChunksByContext(string context)
        {
            if (string.IsNullOrEmpty(context) || context == "All")
            {
                return _documentChunks;
            }

            return _documentChunks.Where(c => c.Context == context).ToList();
        }

        public HashSet<string> GetAvailableContexts()
        {
            HashSet<string> contexts = new HashSet<string>();
            contexts.Add("All");

            foreach (var chunk in _documentChunks)
            {
                if (!string.IsNullOrEmpty(chunk.Context))
                {
                    contexts.Add(chunk.Context);
                }
            }

            return contexts;
        }

        public List<DocumentChunk> RetrieveRelevantDocuments(string question, string selectedContext, int topN = 3)
        {
            // This is a simplified implementation for demonstration purposes
            List<DocumentChunk> relevantChunks = new List<DocumentChunk>();

            // Filter by context if not "All"
            var filteredChunks = _documentChunks;
            if (selectedContext != "All")
            {
                filteredChunks = _documentChunks.Where(c => c.Context == selectedContext).ToList();
            }

            // Simple keyword matching (this would be replaced with embedding similarity in a real implementation)
            foreach (var chunk in filteredChunks)
            {
                string[] keywords = question.ToLower().Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var keyword in keywords)
                {
                    if (keyword.Length > 3 && chunk.Text.ToLower().Contains(keyword.ToLower()))
                    {
                        relevantChunks.Add(chunk);
                        break;
                    }
                }
            }

            // Return top N chunks (in a real implementation, this would be ordered by embedding similarity)
            return relevantChunks.Take(topN).ToList();
        }

        private void LoadEmbeddings()
        {
            try
            {
                // Check if embeddings file exists
                if (File.Exists(_embeddingsPath))
                {
                    string json = File.ReadAllText(_embeddingsPath);
                    _documentChunks = JsonConvert.DeserializeObject<List<DocumentChunk>>(json);
                }
                else
                {
                    // Initialize with some sample data if no embeddings file exists
                    _documentChunks = GetSampleDocumentChunks();
                    
                    // Create directory if it doesn't exist
                    string directory = Path.GetDirectoryName(_embeddingsPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    // Save the sample data
                    SaveEmbeddings();
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error loading embeddings: {ex.Message}", "Error", 
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                _documentChunks = GetSampleDocumentChunks();
            }
        }

        public void SaveEmbeddings()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_documentChunks, Formatting.Indented);
                File.WriteAllText(_embeddingsPath, json);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error saving embeddings: {ex.Message}", "Error", 
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private List<DocumentChunk> GetSampleDocumentChunks()
        {
            return new List<DocumentChunk>
            {
                new DocumentChunk 
                { 
                    Text = "Revit is a Building Information Modeling (BIM) software for architects, engineers, and construction professionals.",
                    Source = "Revit Overview",
                    Context = "General"
                },
                new DocumentChunk 
                { 
                    Text = "The Revit API allows you to automate tasks and extend Revit's functionality through external applications.",
                    Source = "Revit API Documentation",
                    Context = "API"
                },
                new DocumentChunk 
                { 
                    Text = "FilteredElementCollector is used to query elements from the Revit database based on specified criteria.",
                    Source = "Revit API Documentation",
                    Context = "API"
                },
                new DocumentChunk 
                { 
                    Text = "Walls in Revit can be created programmatically using the Wall.Create method.",
                    Source = "Revit API Documentation",
                    Context = "API"
                },
                new DocumentChunk 
                { 
                    Text = "Model Context Protocol (MCP) is an open framework for standardizing how AI models integrate with diverse data sources.",
                    Source = "MCP Documentation",
                    Context = "AI Integration"
                },
                new DocumentChunk 
                { 
                    Text = "RAG (Retrieval-Augmented Generation) combines document retrieval with language model generation to provide accurate answers.",
                    Source = "RAG Overview",
                    Context = "AI Integration"
                },
                new DocumentChunk 
                { 
                    Text = "Element.Id property returns the unique identifier for a Revit element which can be used to retrieve the element later.",
                    Source = "Revit API Documentation",
                    Context = "API"
                },
                new DocumentChunk 
                { 
                    Text = "Transaction class in Revit API is used to modify elements in the Revit model. All modifications must be within a transaction.",
                    Source = "Revit API Documentation",
                    Context = "API"
                },
                new DocumentChunk 
                { 
                    Text = "Families in Revit are templates that contain parameters which control the appearance and behavior of elements.",
                    Source = "Revit Documentation",
                    Context = "General"
                },
                new DocumentChunk 
                { 
                    Text = "Parameters in Revit allow you to define the characteristics of elements and can be accessed via the Parameters property.",
                    Source = "Revit API Documentation",
                    Context = "API"
                }
            };
        }

        public void AddDocumentChunk(DocumentChunk newChunk)
        {
            _documentChunks.Add(newChunk);
            SaveEmbeddings();
        }

        public void RemoveDocumentChunk(DocumentChunk chunk)
        {
            _documentChunks.Remove(chunk);
            SaveEmbeddings();
        }
    }
}