using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitRagAgent
{
    [Transaction(TransactionMode.Manual)]
    public class RevitRagCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Create and show the main form
                RevitRagForm form = new RevitRagForm(commandData.Application);
                form.ShowDialog();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    public class RevitRagForm : System.Windows.Forms.Form
    {
        private UIApplication _uiApp;
        private System.Windows.Forms.TextBox _questionTextBox;
        private System.Windows.Forms.Button _askButton;
        private System.Windows.Forms.Button _scriptButton;
        private System.Windows.Forms.Button _settingsButton;
        private System.Windows.Forms.RichTextBox _answerTextBox;
        private System.Windows.Forms.Label _statusLabel;
        private System.Windows.Forms.ComboBox _contextSelector;
        private Settings _settings;
        private DocumentEmbeddings _documentEmbeddings;
        private RagProcessor _ragProcessor;
        private RevitScriptExecutor _scriptExecutor;

        public RevitRagForm(UIApplication uiApp)
        {
            _uiApp = uiApp;
            
            // Load settings
            _settings = SettingsManager.LoadSettings();
            
            // Initialize document embeddings and RAG processor
            _documentEmbeddings = new DocumentEmbeddings(_settings.EmbeddingsPath);
            _ragProcessor = new RagProcessor(_uiApp, _documentEmbeddings, _settings);
            
            // Initialize script executor
            _scriptExecutor = new RevitScriptExecutor(_uiApp, _settings);
            
            InitializeComponents();
            PopulateContextSelector();
        }

        private void InitializeComponents()
        {
            this.Text = "Revit RAG Agent";
            this.Width = 600;
            this.Height = 500;
            this.StartPosition = FormStartPosition.CenterScreen;

            Label headerLabel = new Label
            {
                Text = "Revit RAG Agent",
                Dock = DockStyle.Top,
                Font = new System.Drawing.Font("Arial", 14, System.Drawing.FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Height = 30
            };

            Label contextLabel = new WinForms.Label
            {
                Text = "Select Context:",
                Location = new System.Drawing.Point(10, 40),
                Width = 100
            };

            _contextSelector = new WinForms.ComboBox
            {
                Location = new System.Drawing.Point(110, 40),
                Width = 400,
                DropDownStyle = WinForms.ComboBoxStyle.DropDownList
            };
            
            _settingsButton = new WinForms.Button
            {
                Text = "Settings",
                Location = new System.Drawing.Point(520, 40),
                Width = 60,
                Height = 23
            };
            _settingsButton.Click += SettingsButton_Click;

            Label questionLabel = new WinForms.Label
            {
                Text = "Ask a question:",
                Location = new System.Drawing.Point(10, 70),
                Width = 100
            };

            _questionTextBox = new WinForms.TextBox
            {
                Location = new System.Drawing.Point(10, 90),
                Width = 570,
                Height = 60,
                Multiline = true
            };

            _askButton = new WinForms.Button
            {
                Text = "Ask",
                Location = new System.Drawing.Point(10, 160),
                Width = 80,
                Height = 30
            };
            _askButton.Click += AskButton_Click;
            
            _scriptButton = new WinForms.Button
            {
                Text = "Generate Script",
                Location = new System.Drawing.Point(100, 160),
                Width = 100,
                Height = 30
            };
            _scriptButton.Click += ScriptButton_Click;

            _statusLabel = new WinForms.Label
            {
                Text = "",
                Location = new System.Drawing.Point(210, 165),
                Width = 370
            };

            Label answerLabel = new WinForms.Label
            {
                Text = "Answer:",
                Location = new System.Drawing.Point(10, 200),
                Width = 100
            };

            _answerTextBox = new WinForms.RichTextBox
            {
                Location = new System.Drawing.Point(10, 220),
                Width = 570,
                Height = 220,
                ReadOnly = true
            };

            this.Controls.Add(headerLabel);
            this.Controls.Add(contextLabel);
            this.Controls.Add(_contextSelector);
            this.Controls.Add(_settingsButton);
            this.Controls.Add(questionLabel);
            this.Controls.Add(_questionTextBox);
            this.Controls.Add(_askButton);
            this.Controls.Add(_scriptButton);
            this.Controls.Add(_statusLabel);
            this.Controls.Add(answerLabel);
            this.Controls.Add(_answerTextBox);
        }

        private void PopulateContextSelector()
        {
            HashSet<string> contexts = _documentEmbeddings.GetAvailableContexts();
            
            foreach (var context in contexts)
            {
                _contextSelector.Items.Add(context);
            }

            if (_contextSelector.Items.Count > 0)
            {
                _contextSelector.SelectedIndex = 0;
            }
        }

        private async void AskButton_Click(object sender, EventArgs e)
        {
            string question = _questionTextBox.Text.Trim();
            if (string.IsNullOrEmpty(question))
            {
                MessageBox.Show("Please enter a question.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _askButton.Enabled = false;
            _scriptButton.Enabled = false;
            _statusLabel.Text = "Processing...";
            _answerTextBox.Text = "";

            try
            {
                string selectedContext = _contextSelector.SelectedItem.ToString();
                string answer = await _ragProcessor.ProcessQuery(question, selectedContext);
                _answerTextBox.Text = answer;
            }
            catch (Exception ex)
            {
                _answerTextBox.Text = $"Error: {ex.Message}";
            }
            finally
            {
                _askButton.Enabled = true;
                _scriptButton.Enabled = true;
                _statusLabel.Text = "Ready";
            }
        }
        
        private async void ScriptButton_Click(object sender, EventArgs e)
        {
            string question = _questionTextBox.Text.Trim();
            if (string.IsNullOrEmpty(question))
            {
                MessageBox.Show("Please enter a task or operation description.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirmResult = MessageBox.Show(
                "This will generate and potentially execute code in your Revit model. Continue?",
                "Confirm Script Execution",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirmResult != DialogResult.Yes)
            {
                return;
            }

            _scriptButton.Enabled = false;
            _askButton.Enabled = false;
            _statusLabel.Text = "Generating script...";
            _answerTextBox.Text = "";

            try
            {
                // Prepare the script generation prompt
                string scriptPrompt = _scriptExecutor.PrepareScriptGenerationPrompt(question);
                
                // Get code from the LLM
                string selectedContext = _contextSelector.SelectedItem.ToString();
                string response = await _ragProcessor.ProcessScriptQuery(scriptPrompt, selectedContext);
                
                // Display the generated script
                _answerTextBox.Text = response;
                
                // Extract and execute the code
                DialogResult executeResult = MessageBox.Show(
                    "Review the generated script. Would you like to execute it?",
                    "Execute Script",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                
                if (executeResult == DialogResult.Yes)
                {
                    _statusLabel.Text = "Executing script...";
                    string executionResult = _scriptExecutor.ExecuteScriptFromResponse(response);
                    
                    // Append execution result
                    _answerTextBox.Text += "\n\n--- EXECUTION RESULT ---\n" + executionResult;
                }
            }
            catch (Exception ex)
            {
                _answerTextBox.Text = $"Error: {ex.Message}";
            }
            finally
            {
                _scriptButton.Enabled = true;
                _askButton.Enabled = true;
                _statusLabel.Text = "Ready";
            }
        }
        
        private void SettingsButton_Click(object sender, EventArgs e)
        {
            using (SettingsForm settingsForm = new SettingsForm(_settings))
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    // Reload settings
                    _settings = SettingsManager.LoadSettings();
                    
                    // Reinitialize the RAG processor with new settings
                    _documentEmbeddings = new DocumentEmbeddings(_settings.EmbeddingsPath);
                    _ragProcessor = new RagProcessor(_uiApp, _documentEmbeddings, _settings);
                    
                    // Refresh context selector
                    _contextSelector.Items.Clear();
                    PopulateContextSelector();
                }
            }
        }
    }
}