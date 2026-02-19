using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using System;

public class VisionBridge : MonoBehaviour
{
    [Header("OpenAI Config")]
    // Update this to the official OpenAI endpoint
    public string apiUrl = "https://api.openai.com/v1/chat/completions"; 
    public string modelName = "gpt-4o"; // You can use gpt-4o or gpt-4o-mini
    public Camera droneCamera; 

    // Hardcode your key here so the E-Quad network doesn't block you
    private string string apiKey = ""; // Add key locally, do not commit;

    public void ScanScene(string prompt, Action<string> callback)
    {
        StartCoroutine(ProcessScan(prompt, callback));
    }

    IEnumerator ProcessScan(string prompt, Action<string> callback)
    {
        // 1. Capture the Frame
        RenderTexture rt = new RenderTexture(512, 512, 24);
        droneCamera.targetTexture = rt; 
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        
        droneCamera.Render();
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        
        droneCamera.targetTexture = null; 
        RenderTexture.active = null; 
        Destroy(rt);

        // 2. Encode to Base64
        byte[] bytes = tex.EncodeToJPG(50); 
        string base64Image = Convert.ToBase64String(bytes);
        Destroy(tex);

        // 3. Construct OpenAI-Specific JSON Payload
        // Note: OpenAI requires a specific nested structure for images
        string json = $@"{{
            ""model"": ""{modelName}"",
            ""messages"": [
                {{
                    ""role"": ""user"",
                    ""content"": [
                        {{ ""type"": ""text"", ""text"": ""{prompt}"" }},
                        {{ ""type"": ""image_url"", ""image_url"": {{ ""url"": ""data:image/jpeg;base64,{base64Image}"" }} }}
                    ]
                }}
            ],
            ""max_tokens"": 300
        }}";

        var request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        
        // 4. Set Headers (Crucial for Connection)
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        Debug.Log("VisionBridge: Sending image to OpenAI...");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;
            Debug.Log("VisionBridge: AI Analysis received.");
            callback(response); 
        }
        else
        {
            Debug.LogError($"VisionBridge: OpenAI request failed. Error: {request.error}");
            Debug.LogError($"Response Code: {request.responseCode} - {request.downloadHandler.text}");
        }
    }
}
