using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using System;

public class AIService : MonoBehaviour
{
    [Header("OpenAI Config")]
    public string apiUrl = "https://api.openai.com/v1/chat/completions";
    public string modelName = "gpt-4o";
    
    // Paste your key here or link it via a global manager
    private string string apiKey = ""; // Add key locally, do not commit

    [Header("Parameters")]
    public int maxTokens = 500;
    [Range(0f, 2f)] public float temperature = 0.7f;

    public void SendPrompt(string systemPrompt, string userPrompt, Action<string> callback)
    {
        StartCoroutine(PostRequest(systemPrompt, userPrompt, callback));
    }

    IEnumerator PostRequest(string systemRole, string userMessage, Action<string> callback) 
    {
        // 1. Construct the CHAT JSON (GPT-4o requires the "messages" array)
        string json = $@"{{
            ""model"": ""{modelName}"",
            ""messages"": [
                {{ ""role"": ""system"", ""content"": ""{EscapeJson(systemRole)}"" }},
                {{ ""role"": ""user"", ""content"": ""{EscapeJson(userMessage)}"" }}
            ],
            ""max_tokens"": {maxTokens},
            ""temperature"": {temperature.ToString(System.Globalization.CultureInfo.InvariantCulture)}
        }}";

        var request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        // 2. Set the Headers (Must include Bearer Token)
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            callback(ParseChatResponse(request.downloadHandler.text)); 
        } else {
            Debug.LogError($"AIService Error: {request.error}\nDetails: {request.downloadHandler.text}");
        }
    }



    // Updated parser for the "Message" content field in Chat Completions
    string ParseChatResponse(string json)
    {
        try {
            string key = "\"content\":";
            int keyIndex = json.IndexOf(key);
            if (keyIndex == -1) return null;

            int start = json.IndexOf("\"", keyIndex + key.Length) + 1;
            int end = start;
            
            while (end < json.Length) {
                if (json[end] == '"' && json[end - 1] != '\\') break;
                end++;
            }

            string content = json.Substring(start, end - start);
            return content.Replace("\\n", "\n").Replace("\\\"", "\"");
        } catch {
            return null;
        }
    }

    static string EscapeJson(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }
}

