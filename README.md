# LLM-Integration-Unity

A small project made with **Unity Engine** featuring a chatbot integrated with an **LLM**.

---

## 🔑 API Key Setup

This project uses **external APIs (OpenAI and Google Gemini)**, which require authentication keys.  
For security reasons, **never hardcode your keys directly into the source code**.

---

## 📂 1. Create the `.env` file

In the **root folder** of the project, create a file named **`.env`** with the following content:

```env
OPENAI_KEY=your_openai_key_here
OPENAI_PROJECT_ID=your_openai_project_id_here
GEMINI_KEY=your_gemini_key_here
GEMINI_MODEL=gemini-2.5-flash
⚠️ Important:

Replace your_openai_key_here, your_openai_project_id_here, and your_gemini_key_here with your real keys.

The default Gemini model is gemini-2.5-flash, but you may change it if needed.

🚫 2. Ensure .env is not versioned
The .env file is already listed in .gitignore.
Check that it contains the following entry:

bash
Copy code
.env
This prevents your API keys from being pushed to the remote repository.

⚙️ 3. Load variables in Unity
The project includes a simple .env loader.
Inside the Awake() method of any script that requires the keys, use:

csharp
Copy code
EnvLoader.Load(); // Loads the .env file

var openAiApiKey = EnvLoader.Get("OPENAI_KEY");
var openAiProjectId = EnvLoader.Get("OPENAI_PROJECT_ID");
var geminiKey = EnvLoader.Get("GEMINI_KEY");
var geminiModel = EnvLoader.Get("GEMINI_MODEL");

if (string.IsNullOrEmpty(openAiApiKey))
{
    Debug.LogError("OPENAI_KEY not found! Please configure the .env file.");
}
else
{
    Debug.Log("OPENAI_KEY successfully loaded!");
}
✅ Example workflow
Create the .env file in the project root with your keys.

Start Unity.

The script automatically loads the keys via EnvLoader.

Access the variables anywhere using EnvLoader.Get("VARIABLE").
```
