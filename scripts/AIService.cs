using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using System;

public class AIService : MonoBehaviour
{
    [Header("Ollama Settings")]
    public string apiUrl = "http://10.32.83.219:11434/v1/chat/completions";
    public string modelName = "openai-gpt-oss-20b";
    public int requestTimeout = 60;

    void Start() {
        StartCoroutine(CheckConnection());
    }

    IEnumerator CheckConnection() {
        var ping = new UnityWebRequest(apiUrl.Replace("/v1/chat/completions", "/api/tags"), "GET");
        ping.downloadHandler = new DownloadHandlerBuffer();
        ping.timeout = 5;
        yield return ping.SendWebRequest();

        if (ping.result == UnityWebRequest.Result.Success)
            Debug.Log($"<color=green>SUCCESS:</color> Ollama connected. Model: {modelName}");
        else
            Debug.LogError($"<color=red>CRITICAL:</color> Cannot reach Ollama at {apiUrl}. Is it running?");
    }

    public void SendPrompt(string systemPrompt, string userPrompt, Action<string> callback){
        StartCoroutine(PostRequest(systemPrompt, userPrompt, callback));
    }

    IEnumerator PostRequest(string systemRole, string userMessage, Action<string> callback){
        // Sanitize inputs to prevent JSON breakage
        string safeSystem = systemRole.Replace("\"", "\\\"").Replace("\n", " ");
        string safeUser = userMessage.Replace("\"", "\\\"").Replace("\n", " ");

        string json = $@"{{
            ""model"": ""{modelName}"",
            ""messages"": [
                {{ ""role"": ""system"", ""content"": ""{safeSystem}"" }},
                {{ ""role"": ""user"", ""content"": ""{safeUser}"" }}
            ],
            ""stream"": false
        }}";

        var request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = requestTimeout;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success){
            string responseText = request.downloadHandler.text;

            // Manual JSON parsing — Ollama uses same OpenAI response format
            string content = ParseResponseContent(responseText);
            if (content != null)
                callback(content);
            else
                Debug.LogError($"AIService: failed to parse response: {responseText}");
        }
        else{
            Debug.LogError($"AIService: {request.error}\nResponse: {request.downloadHandler.text}");
        }
    }

    string ParseResponseContent(string json)
    {
        // find the last "content" key (the assistant message)
        string key = "\"content\":";
        int keyIndex = json.LastIndexOf(key);
        if (keyIndex == -1) return null;

        int start = keyIndex + key.Length;
        while (start < json.Length && (json[start] == ' ' || json[start] == '\n' || json[start] == '\r'))
            start++;

        if (start >= json.Length || json[start] != '"') return null;
        start++; // skip opening quote

        int end = start;
        while (end < json.Length){
            if (json[end] == '"' && json[end - 1] != '\\') break;
            end++;
        }

        if (end >= json.Length) return null;

        string content = json.Substring(start, end - start);
        content = content.Replace("\\n", "\n").Replace("\\\"", "\"");
        return content;
    }
}