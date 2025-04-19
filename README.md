# Revit RAG Agent

A flexible Retrieval-Augmented Generation (RAG) agent for Revit that uses the Revit API to provide contextual information and answer questions about Revit and the current model. The agent is API-agnostic and can work with OpenAI, Anthropic Claude, or Google Gemini.

## Overview

This application creates a simple Windows Form interface within Revit that allows users to:
- Ask questions about Revit or the current model
- Generate and execute Revit API scripts to automate tasks
- Select specific contexts for their queries
- Get answers based on both documentation and the current Revit document state
- Configure different LLM providers through a settings interface

## Features

- **Context-Aware Retrieval**: The agent retrieves relevant documentation chunks based on the user's question
- **Revit API Integration**: Leverages the Revit API to gather real-time data from the current model
- **Script Generation and Execution**: Generate and run Revit API scripts to automate tasks like creating walls, copying elements, etc.
- **Extensible Architecture**: Easily add more document sources or enhance retrieval methods
- **Multi-Provider Support**: Works with OpenAI GPT models, Anthropic Claude, or Google Gemini
- **Configurable Settings**: Customize API keys, model selection, and parameters

## Requirements

- Autodesk Revit 2024 or later
- .NET Framework 4.8
- Visual Studio 2022 (for development)
- API key for one of the supported LLM providers (OpenAI, Anthropic, or Google)

## Installation

1. Build the solution to generate the DLL
2. Copy the DLL and dependencies to a folder
3. Place the .addin manifest in the Revit add-ins folder (typically found in `%APPDATA%\Autodesk\Revit\Addins\[Version]`)
4. Launch Revit and configure your API settings through the Settings button

## Project Structure

- **Core Classes**:
  - `RevitRagCommand.cs` - Contains the main external command implementation and the Windows Forms UI
  - `RevitRagApp.cs` - Application entry point for Revit
  - `DocumentEmbeddings.cs` - Manages document chunks and retrieval functionality
  - `RagProcessor.cs` - Handles the RAG process: retrieval, context enhancement, and generation
  - `RevitScriptExecutor.cs` - Enables generation and execution of Revit API scripts
  - `SettingsManager.cs` - Manages user settings and configuration
  - `SettingsForm.cs` - Provides UI for configuring API settings

## Usage

1. Click on the "Revit RAG Agent" button in the Revit ribbon
2. Configure your LLM provider settings by clicking the "Settings" button
3. Select a context for your query ("All", "API", "General", etc.)
4. Type your question or task description in the text box
5. Click "Ask" to get an answer to your question
6. Click "Generate Script" to create and execute a Revit API script based on your description

## API Integration

The application is designed to work with multiple LLM providers:

- **OpenAI**: Compatible with GPT-4, GPT-4-Turbo, and GPT-3.5-Turbo models
- **Anthropic**: Compatible with Claude 3 models (Opus, Sonnet, Haiku)
- **Google**: Compatible with Gemini models (1.0 Pro, 1.5 Pro)

## Development Notes

- The document chunks are simplified for demonstration. In a real implementation, you would convert Revit SDK documentation into vector embeddings
- For improved performance, consider implementing a local vector database

## License

[Your License Information]