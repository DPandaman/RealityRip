// captures drone pov and sends to local vlm for scene analysis

using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using System;

public class VisionBridge : MonoBehaviour
{
    [Header("VLM Config")]
    // local inference endpoint (ollama openai-compatible api)
    public string localApiUrl = "http://10.32.83.219:11434/v1/chat/completions";
    public string modelName = "llava";
    public Camera droneCamera;

    [Header("Capture Settings")]
    public int captureResolution = 512;
    public int jpegQuality = 50;
    public int requestTimeout = 30;

    public void ScanScene(string prompt, Action<string> callback){
        if (droneCamera == null){
            Debug.LogError("VisionBridge: drone camera not assigned");
            callback?.Invoke(null);
            return;
        }

        StartCoroutine(ProcessScan(prompt, callback));
    }

    IEnumerator ProcessScan(string prompt, Action<string> callback){
        // capture frame from drone camera
        RenderTexture rt = new RenderTexture(captureResolution, captureResolution, 24);
        droneCamera.targetTexture = rt;
        droneCamera.Render();
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        // cleanup gpu resources
        droneCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        // encode to base64 jpg
        byte[] bytes = tex.EncodeToJPG(jpegQuality);
        string base64Image = Convert.ToBase64String(bytes);
        Destroy(tex);

        // build openai-compatible multimodal payload
        // ollama /v1/chat/completions expects image_url with data uri for vision models
        string safePrompt = prompt.Replace("\"", "\\\"").Replace("\n", " ");
        string json = $@"{{
            ""model"": ""{modelName}"",
            ""messages"": [
                {{
                    ""role"": ""user"",
                    ""content"": [
                        {{
                            ""type"": ""text"",
                            ""text"": ""{safePrompt}""
                        }},
                        {{
                            ""type"": ""image_url"",
                            ""image_url"": {{
                                ""url"": ""data:image/jpeg;base64,{base64Image}""
                            }}
                        }}
                    ]
                }}
            ],
            ""stream"": false
        }}";

        // send request to local vlm
        using (var request = new UnityWebRequest(localApiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = requestTimeout;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // parse content from response json
                string content = ParseResponseContent(request.downloadHandler.text);

                if (content != null){
                    callback?.Invoke(content);
                }
                else{
                    Debug.LogError("VisionBridge: failed to parse vlm response");
                    callback?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"VisionBridge: request failed - {request.error}");
                callback?.Invoke(null);
            }
        }
    }

    // extract "content" field from openai-style chat completion response
    string ParseResponseContent(string json)
    {
        // target the assistant message content
        string key = "\"content\":";
        int keyIndex = json.LastIndexOf(key);
        if (keyIndex == -1) return null;

        // skip key and whitespace to reach opening quote
        int start = keyIndex + key.Length;
        while (start < json.Length && (json[start] == ' ' || json[start] == '\n' || json[start] == '\r'))
            start++;

        if (start >= json.Length || json[start] != '"') return null;
        start++; // skip opening quote

        // walk to closing quote, respecting escaped quotes
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
