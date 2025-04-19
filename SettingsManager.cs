using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace RevitRagAgent
{
    public class Settings
    {
        public string ApiKey { get; set; } = "your-api-key-here";
        public string LlmProvider { get; set; } = "OpenAI"; // Options: OpenAI, Anthropic, Gemini
        public string ModelName { get; set; } = "gpt-4"; // Model name according to the provider
        public int MaxTokens { get; set; } = 1000;
        public float Temperature { get; set; } = 0.7f;
        public string EmbeddingsPath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
            "RevitRagAgent", 
            "embeddings.json");
            
        // Helper method to get available models for the selected provider
        public static List<string> GetAvailableModels(string provider)
        {
            switch (provider.ToLower())
            {
                case "openai":
                    return new List<string>
                    {
                        "gpt-4",
                        "gpt-4-turbo",
                        "gpt-3.5-turbo"
                    };
                case "anthropic":
                    return new List<string>
                    {
                        "claude-3-opus-20240229",
                        "claude-3-sonnet-20240229",
                        "claude-3-haiku-20240307"
                    };
                case "gemini":
                    return new List<string>
                    {
                        "gemini-1.0-pro",
                        "gemini-1.5-pro"
                    };
                default:
                    return new List<string>();
            }
        }
        
        // Available LLM providers
        public static List<string> GetAvailableProviders()
        {
            return new List<string>
            {
                "OpenAI",
                "Anthropic",
                "Gemini"
            };
        }
    }

    public class SettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RevitRagAgent",
            "settings.json");

        public static Settings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<Settings>(json);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    $"Error loading settings: {ex.Message}. Default settings will be used.",
                    "Settings Error", 
                    System.Windows.Forms.MessageBoxButtons.OK, 
                    System.Windows.Forms.MessageBoxIcon.Warning);
            }

            // Return default settings if file doesn't exist or there's an error
            return new Settings();
        }

        public static void SaveSettings(Settings settings)
        {
            try
            {
                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    $"Error saving settings: {ex.Message}",
                    "Settings Error", 
                    System.Windows.Forms.MessageBoxButtons.OK, 
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }
    }
}