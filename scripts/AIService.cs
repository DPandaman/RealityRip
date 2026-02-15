using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using System;

public class AIService : MonoBehaviour
{
    [Header("API Settings")]
    public string apiKey = ""; 
    public string apiUrl = "https://api.openai.com/v1/chat/completions";

    void Awake() {
        // Load key from environment variable safely
        apiKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }

    void Start() {
        if (string.IsNullOrEmpty(apiKey)) {
            Debug.LogError("<color=red>CRITICAL:</color> OpenAI Key missing. Check environment variables.");
        } else {
             Debug.Log($"<color=green>SUCCESS:</color> OpenAI Key loaded.");
        }
    }

    public void SendPrompt(string systemPrompt, string userPrompt, Action<string> callback){
        StartCoroutine(PostRequest(systemPrompt, userPrompt, callback));
    }

    IEnumerator PostRequest(string systemRole, string userMessage, Action<string> callback){
        // Sanitize inputs to prevent JSON breakage
        string safeSystem = systemRole.Replace("\"", "\\\"").Replace("\n", " ");
        string safeUser = userMessage.Replace("\"", "\\\"").Replace("\n", " ");

        string json = $@"{{
            ""model"": ""gpt-4o"",
            ""messages"": [
                {{ ""role"": ""system"", ""content"": ""{safeSystem}"" }},
                {{ ""role"": ""user"", ""content"": ""{safeUser}"" }}
            ]
        }}";

        var request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success){
            string responseText = request.downloadHandler.text;
            
            // Manual JSON parsing
            string key = "\"content\": \"";
            int start = responseText.IndexOf(key);
            
            if (start != -1) {
                start += key.Length;
                int end = responseText.IndexOf("\"", start);
                
                // Handle cases where the response might contain escaped quotes
                while (end != -1 && responseText[end - 1] == '\\') {
                    end = responseText.IndexOf("\"", end + 1);
                }

                string cleanMessage = responseText.Substring(start, end - start);
                
                // Unescape special characters for UI display
                cleanMessage = cleanMessage.Replace("\\n", "\n").Replace("\\\"", "\"");
                
                callback(cleanMessage); 
            } 
        }
        else{
            Debug.LogError($"AI Error: {request.error} \nResponse: {request.downloadHandler.text}");
        }
    }
}