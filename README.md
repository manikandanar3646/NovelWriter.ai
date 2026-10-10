# 🖋️ AI Novel Writing System

A desktop-based AI Novel Writing System built with **C# and .NET 8 WPF** that helps users generate creative stories, develop ideas, and improve written content using AI.

The application supports **two AI providers**:

- 🖥️ **Local AI** — Ollama + Llama 3.1
- ☁️ **Cloud AI** — OpenAI API

Users can choose between running an AI model locally or using OpenAI's cloud-based models.

---

## ✨ Features

### 🤖 Multiple AI Providers

Choose how the application generates AI responses:

#### 🖥️ Local AI — Ollama + Llama 3.1

- Runs the AI model locally on your computer
- No OpenAI API key required
- No cloud API usage cost
- Can work without an internet connection once Ollama and the model are installed
- Requires sufficient system RAM and hardware resources

#### ☁️ OpenAI Cloud

- Uses OpenAI's API to generate responses
- Does not require running a large local AI model
- Suitable for computers with limited RAM
- Requires an internet connection
- Requires the user's own OpenAI API key
- OpenAI API usage is paid separately according to OpenAI's current pricing and billing

---

### ✍️ Creative Mode

Creative Mode is designed for generating imaginative content.

The AI can help with:

- Story ideas
- Dark fantasy writing
- Character concepts
- Story scenes
- World-building
- Creative descriptions
- Novel development

Example:

> Generate a dark fantasy story about a forest tribe whose homeland is destroyed by a mysterious fire.

---

### ✒️ Manual Mode

Manual Mode works as a writing assistant.

It focuses on:

- Grammar correction
- Spelling correction
- Sentence structure
- Punctuation
- Improving readability

The original meaning of the user's text is preserved.

---

## 🧠 AI Architecture

The project uses an abstraction-based AI architecture so that different AI providers can be used without changing the main application logic.

```text
                AI Novel Writing System
                          │
                      AI Provider  
                     /          \
                    /            \
             Local AI             Cloud AI
                │                     │
             Ollama               OpenAI API
                │                     │
            Llama 3.1             OpenAI Model
                │                     │
                └──────────┬──────────┘
                           │
                     AI Service
                           │
                 Creative / Manual
                           │
                    User Prompt
                           │
                     AI Response
