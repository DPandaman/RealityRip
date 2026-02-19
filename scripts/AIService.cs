using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using System;

public class AIService : MonoBehaviour
{
    [Header("Conversation Model Settings")]
    public string apiUrl = "http://10.32.83.219:8000/v1/completions";
    public string modelName = "openai/gpt-oss-20b";
    public string healthUrl = "";
    public int requestTimeout = 60;
    public int maxTokens = 256;
    [Range(0f, 2f)] public float temperature = 0.7f;
    [Range(0f, 1f)] public float topP = 1f;

    void Start() {
        StartCoroutine(CheckConnection());
    }

    IEnumerator CheckConnection() {
        string pingUrl = !string.IsNullOrEmpty(healthUrl)
            ? healthUrl
            : (apiUrl.Contains("/v1/completions")
                ? apiUrl.Replace("/v1/completions", "/v1/models")
                : apiUrl);
        var ping = new UnityWebRequest(pingUrl, "GET");
        ping.downloadHandler = new DownloadHandlerBuffer();
        ping.timeout = 5;
        yield return ping.SendWebRequest();

        if (ping.result == UnityWebRequest.Result.Success)
            Debug.Log($"<color=green>SUCCESS:</color> LLM connected. Model: {modelName}");
        else
            Debug.LogError($"<color=red>CRITICAL:</color> Cannot reach LLM at {apiUrl}. Is it running?");
    }

    public void SendPrompt(string systemPrompt, string userPrompt, Action<string> callback){
        StartCoroutine(PostRequest(systemPrompt, userPrompt, callback));
    }

    IEnumerator PostRequest(string systemRole, string userMessage, Action<string> callback){
        // Sanitize inputs to prevent JSON breakage
        string safeSystem = EscapeJson(systemRole);
        string safeUser = EscapeJson(userMessage);

        string prompt = $"System: {safeSystem}\nUser: {safeUser}\nAssistant:";
        string safePrompt = EscapeJson(prompt);

        string json = $@"{{
            \"model\": \"{modelName}\",
            \"prompt\": \"{safePrompt}\",
            \"max_tokens\": {maxTokens},
            \"temperature\": {temperature.ToString(System.Globalization.CultureInfo.InvariantCulture)},
            \"top_p\": {topP.ToString(System.Globalization.CultureInfo.InvariantCulture)}
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

            // Manual JSON parsing — OpenAI-compatible completions response format
            string content = ParseResponseText(responseText);
            if (content != null)
                callback(content);
            else
                Debug.LogError($"AIService: failed to parse response: {responseText}");
        }
        else{
            Debug.LogError($"AIService: {request.error}\nResponse: {request.downloadHandler.text}");
        }
    }

    string ParseResponseText(string json)
    {
        // find the last "text" key in choices
        string key = "\"text\":";
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

    static string EscapeJson(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input.Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }
}
