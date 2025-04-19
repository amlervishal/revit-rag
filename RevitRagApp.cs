using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace RevitRagAgent
{
    public class RevitRagApp : IExternalApplication
    {
        // Button ID
        public const string ButtonId = "RevitRagAgent.RevitRagCommand";
        
        // Application name
        public const string AppName = "Revit RAG";
        
        public Result OnStartup(UIControlledApplication application)
        {
            // Create a ribbon panel
            RibbonPanel ribbonPanel = CreateRibbonPanel(application);
            
            // Add button to the panel
            AddButtons(ribbonPanel);
            
            return Result.Succeeded;
        }
        
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
        
        private RibbonPanel CreateRibbonPanel(UIControlledApplication application)
        {
            // Try to create the ribbon panel
            RibbonPanel panel = null;
            
            try
            {
                // Create or get the tab
                application.CreateRibbonTab("RAG Tools");
            }
            catch (Exception)
            {
                // Tab already exists
            }
            
            // Create a new panel
            try
            {
                panel = application.CreateRibbonPanel("RAG Tools", "Revit RAG Agent");
            }
            catch (Exception)
            {
                // Panel might already exist
                List<RibbonPanel> panels = application.GetRibbonPanels("RAG Tools");
                foreach (RibbonPanel p in panels)
                {
                    if (p.Name == "Revit RAG Agent")
                    {
                        panel = p;
                        break;
                    }
                }
                
                if (panel == null)
                {
                    // Try with a different name
                    panel = application.CreateRibbonPanel("RAG Tools", "RAG Agent");
                }
            }
            
            return panel;
        }
        
        private void AddButtons(RibbonPanel panel)
        {
            // Get the assembly path
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            
            // Create a push button
            PushButtonData buttonData = new PushButtonData(
                "RevitRagAgent",
                "Revit RAG\nAgent",
                thisAssemblyPath,
                "RevitRagAgent.RevitRagCommand")
            {
                ToolTip = "Open the Revit RAG Agent to ask questions about Revit or the current model",
                LongDescription = "The Revit RAG Agent allows you to ask questions about Revit functionality or analyze the current model using natural language."
            };
            
            // Add icon (use your own icon or create a simple one)
            // For this example, let's load the icon from a resource if available
            Uri iconUri = new Uri("pack://application:,,,/RevitRagAgent;component/Resources/rag_icon.png", UriKind.Absolute);
            try
            {
                BitmapImage bmp = new BitmapImage(iconUri);
                buttonData.LargeImage = bmp;
            }
            catch
            {
                // If the icon isn't found, that's okay, Revit will use a default one
            }
            
            // Add the button
            panel.AddItem(buttonData);
        }
    }
}