using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RevitRagAgent
{
    public class SettingsForm : Form
    {
        private Settings _settings;
        private TextBox _apiKeyTextBox;
        private ComboBox _providerComboBox;
        private ComboBox _modelNameComboBox;
        private NumericUpDown _maxTokensNumeric;
        private TrackBar _temperatureTrackBar;
        private Label _temperatureValueLabel;
        private TextBox _embeddingsPathTextBox;
        private Button _browseButton;
        private Button _saveButton;
        private Button _cancelButton;
        
        public SettingsForm(Settings settings)
        {
            _settings = settings;
            InitializeComponents();
            LoadSettings();
        }
        
        private void InitializeComponents()
        {
            this.Text = "Revit RAG Agent Settings";
            this.Width = 500;
            this.Height = 400;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            int labelWidth = 120;
            int controlWidth = 300;
            int rowHeight = 30;
            int currentY = 20;
            
            // API Key
            WinForms.Label apiKeyLabel = new WinForms.Label
            {
                Text = "API Key:",
                Location = new System.Drawing.Point(20, currentY + 5),
                Width = labelWidth
            };
            
            _apiKeyTextBox = new WinForms.TextBox
            {
                Location = new System.Drawing.Point(150, currentY),
                Width = controlWidth,
                PasswordChar = '*'
            };
            
            currentY += rowHeight;
            
            // Provider
            WinForms.Label providerLabel = new WinForms.Label
            {
                Text = "LLM Provider:",
                Location = new System.Drawing.Point(20, currentY + 5),
                Width = labelWidth
            };
            
            _providerComboBox = new WinForms.ComboBox
            {
                Location = new System.Drawing.Point(150, currentY),
                Width = controlWidth,
                DropDownStyle = WinForms.ComboBoxStyle.DropDownList
            };
            _providerComboBox.SelectedIndexChanged += ProviderComboBox_SelectedIndexChanged;
            
            currentY += rowHeight;
            
            // Model Name
            Label modelNameLabel = new Label
            {
                Text = "Model Name:",
                Location = new System.Drawing.Point(20, currentY + 5),
                Width = labelWidth
            };
            
            _modelNameComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(150, currentY),
                Width = controlWidth,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            
            currentY += rowHeight;
            
            // Max Tokens
            Label maxTokensLabel = new Label
            {
                Text = "Max Tokens:",
                Location = new System.Drawing.Point(20, currentY + 5),
                Width = labelWidth
            };
            
            _maxTokensNumeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(150, currentY),
                Width = 100,
                Minimum = 100,
                Maximum = 4000,
                Increment = 100
            };
            
            currentY += rowHeight;
            
            // Temperature
            Label temperatureLabel = new Label
            {
                Text = "Temperature:",
                Location = new System.Drawing.Point(20, currentY + 5),
                Width = labelWidth
            };
            
            _temperatureTrackBar = new TrackBar
            {
                Location = new System.Drawing.Point(150, currentY),
                Width = 200,
                Minimum = 0,
                Maximum = 20, // 0.0 to 2.0 with one decimal precision
                TickFrequency = 2,
                SmallChange = 1,
                LargeChange = 5
            };
            _temperatureTrackBar.ValueChanged += TemperatureTrackBar_ValueChanged;
            
            _temperatureValueLabel = new Label
            {
                Text = "0.0",
                Location = new System.Drawing.Point(400, currentY + 5),
                Width = 50
            };
            
            currentY += rowHeight + 10;
            
            // Embeddings Path
            Label embeddingsPathLabel = new Label
            {
                Text = "Embeddings Path:",
                Location = new System.Drawing.Point(20, currentY + 5),
                Width = labelWidth
            };
            
            _embeddingsPathTextBox = new TextBox
            {
                Location = new System.Drawing.Point(150, currentY),
                Width = 240
            };
            
            _browseButton = new Button
            {
                Text = "Browse...",
                Location = new System.Drawing.Point(400, currentY),
                Width = 70
            };
            _browseButton.Click += BrowseButton_Click;
            
            currentY += rowHeight + 30;
            
            // Buttons
            _saveButton = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(300, currentY),
                Width = 80
            };
            _saveButton.Click += SaveButton_Click;
            
            _cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(390, currentY),
                Width = 80
            };
            
            // Add controls to form
            this.Controls.Add(apiKeyLabel);
            this.Controls.Add(_apiKeyTextBox);
            this.Controls.Add(providerLabel);
            this.Controls.Add(_providerComboBox);
            this.Controls.Add(modelNameLabel);
            this.Controls.Add(_modelNameComboBox);
            this.Controls.Add(maxTokensLabel);
            this.Controls.Add(_maxTokensNumeric);
            this.Controls.Add(temperatureLabel);
            this.Controls.Add(_temperatureTrackBar);
            this.Controls.Add(_temperatureValueLabel);
            this.Controls.Add(embeddingsPathLabel);
            this.Controls.Add(_embeddingsPathTextBox);
            this.Controls.Add(_browseButton);
            this.Controls.Add(_saveButton);
            this.Controls.Add(_cancelButton);
            
            this.AcceptButton = _saveButton;
            this.CancelButton = _cancelButton;
        }
        
        private void LoadSettings()
        {
            // Load providers
            foreach (var provider in Settings.GetAvailableProviders())
            {
                _providerComboBox.Items.Add(provider);
            }
            
            // Set current values
            _apiKeyTextBox.Text = _settings.ApiKey;
            _providerComboBox.SelectedItem = _settings.LlmProvider;
            
            // Load models for selected provider
            LoadModelsForProvider(_settings.LlmProvider);
            
            // Select current model
            _modelNameComboBox.SelectedItem = _settings.ModelName;
            
            _maxTokensNumeric.Value = _settings.MaxTokens;
            _temperatureTrackBar.Value = (int)(_settings.Temperature * 10);
            _temperatureValueLabel.Text = _settings.Temperature.ToString("0.0");
            _embeddingsPathTextBox.Text = _settings.EmbeddingsPath;
        }
        
        private void ProviderComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedProvider = _providerComboBox.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedProvider))
            {
                LoadModelsForProvider(selectedProvider);
            }
        }
        
        private void LoadModelsForProvider(string provider)
        {
            _modelNameComboBox.Items.Clear();
            
            foreach (var model in Settings.GetAvailableModels(provider))
            {
                _modelNameComboBox.Items.Add(model);
            }
            
            if (_modelNameComboBox.Items.Count > 0)
            {
                _modelNameComboBox.SelectedIndex = 0;
            }
        }
        
        private void TemperatureTrackBar_ValueChanged(object sender, EventArgs e)
        {
            float temperature = (float)_temperatureTrackBar.Value / 10.0f;
            _temperatureValueLabel.Text = temperature.ToString("0.0");
        }
        
        private void BrowseButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "Select Embeddings File Location",
                Filter = "JSON Files (*.json)|*.json",
                FileName = "embeddings.json",
                InitialDirectory = System.IO.Path.GetDirectoryName(_settings.EmbeddingsPath)
            };
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _embeddingsPathTextBox.Text = dialog.FileName;
            }
        }
        
        private void SaveButton_Click(object sender, EventArgs e)
        {
            // Validate settings
            if (string.IsNullOrWhiteSpace(_apiKeyTextBox.Text))
            {
                WinForms.MessageBox.Show("API Key cannot be empty.", "Validation Error", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                _apiKeyTextBox.Focus();
                return;
            }
            
            if (_providerComboBox.SelectedItem == null)
            {
                WinForms.MessageBox.Show("Please select an LLM provider.", "Validation Error", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                _providerComboBox.Focus();
                return;
            }
            
            if (_modelNameComboBox.SelectedItem == null)
            {
                WinForms.MessageBox.Show("Please select a model.", "Validation Error", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                _modelNameComboBox.Focus();
                return;
            }
            
            if (string.IsNullOrWhiteSpace(_embeddingsPathTextBox.Text))
            {
                WinForms.MessageBox.Show("Embeddings path cannot be empty.", "Validation Error", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                _embeddingsPathTextBox.Focus();
                return;
            }
            
            // Update settings
            _settings.ApiKey = _apiKeyTextBox.Text;
            _settings.LlmProvider = _providerComboBox.SelectedItem.ToString();
            _settings.ModelName = _modelNameComboBox.SelectedItem.ToString();
            _settings.MaxTokens = (int)_maxTokensNumeric.Value;
            _settings.Temperature = (float)_temperatureTrackBar.Value / 10.0f;
            _settings.EmbeddingsPath = _embeddingsPathTextBox.Text;
            
            // Save settings
            SettingsManager.SaveSettings(_settings);
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}