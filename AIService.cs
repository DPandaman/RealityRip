// bridge between drone simulator and openAI 
// constructs sim request -> json + api key -> Coroutine -> OpenAI -> cleans data -> drone simulator

using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using System;

public class AIService : MonoBehaviour
{
    [Header("api settings")]
    public string apiKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    public string apiUrl = "https://api.openai.com/v1/chat/completions";

    public void SendPrompt(string systemPrompt, string userPrompt, Action<string> callback){
        // start request
        StartCoroutine(PostRequest(systemPrompt, userPrompt, callback));
    }

    IEnumerator PostRequest(string systemRole, string userMessage, Action<string> callback){
        // build json body
        string json = $@"
        {{
            ""model"": ""gpt-4o"",
            ""messages"": [
                {{ ""role"": ""system"", ""content"": ""{systemRole}"" }},
                {{ ""role"": ""user"", ""content"": ""{userMessage}"" }}
            ]
        }}";

        // setup web request
        var request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success){
            // clean up message from open ai 
            string responseText = request.downloadHandler.text;
            string key = "\"content\": \"";
            int start = responseText.IndexOf(key) + key.Length;
            int end = responseText.IndexOf("\"", start);
            
            string cleanMessage = responseText.Substring(start, end - start);
            
            cleanMessage = cleanMessage.Replace("\\n", "\n");
            
            callback(cleanMessage); 
        }
        else{
            Debug.LogError("ai error: " + request.error);
        }
    }


    void Start() {
        string checkKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(checkKey)) {
            Debug.LogError("<color=red>CRITICAL:</color> OpenAI Key not found! Restart Unity or check .zshrc");
        } else {
            Debug.Log("<color=green>SUCCESS:</color> OpenAI Key loaded. Ready for rizz.");
        }
    }
}