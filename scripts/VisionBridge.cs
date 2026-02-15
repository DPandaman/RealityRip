// captures drone view and sends it to local Ollama instance on GX10 

using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using System;

public class VisionBridge : MonoBehaviour
{
    [Header("VLM Config")]
    // local inference endpoint. running on the GX10 via ollama/localai
    // ensure port 11434 is exposed and firewall isn't blocking
    public string localApiUrl = "http://localhost:11434/v1/chat/completions"; 
    public string modelName = "llava"; // using llava/moondream for lower inference latency
    public Camera droneCamera; 

    public void ScanScene(string prompt, Action<string> callback)
    {
        // spin up the async routine to avoid blocking the main thread (rendering)
        StartCoroutine(ProcessScan(prompt, callback));
    }

    IEnumerator ProcessScan(string prompt, Action<string> callback)
    {
        // Measurement Acquisition
        // creating a temp render texture to grab the current frame buffer
        // standard 512x512 resolution to balance VLM context window vs. visual fidelity
        RenderTexture rt = new RenderTexture(512, 512, 24);
        droneCamera.targetTexture = rt; 
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        
        // force render trigger
        droneCamera.Render();
        RenderTexture.active = rt;
        
        // read pixels from GPU to CPU memory
        // expensive operation, optimized by reusing the rect
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        
        // cleanup to prevent memory leaks in VRAM
        droneCamera.targetTexture = null; 
        RenderTexture.active = null; 
        Destroy(rt);

        // 2. Data Marshalling
        // encoding to base64 jpg. standard protocol for sending image tensors to llms via json
        // compression quality 50 is a heuristic: good enough for object detection, low bandwidth
        byte[] bytes = tex.EncodeToJPG(50); 
        string base64Image = Convert.ToBase64String(bytes);
        Destroy(tex); // yeet the texture

        // 3. Construct Payload
        // constructing the JSON body for the POST request
        // mirroring the openai chat completion schema
        string json = $@"{{
            ""model"": ""{modelName}"",
            ""messages"": [
                {{
                    ""role"": ""user"",
                    ""content"": ""{prompt} \n[IMG]{base64Image}[/IMG]"" 
                }}
            ],
            ""stream"": false
        }}";

        var request = new UnityWebRequest(localApiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // debug: sending packet
        // Debug.Log("VisionBridge: propagating state to VLM...");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // measurement update successful
            string response = request.downloadHandler.text;
            // passing raw json to callback for parsing
            callback(response); 
        }
        else
        {
            // connection refused or timeout. 
            // probable cause: ollama service not running or port mismatch
            Debug.LogError($"VisionBridge: inference failed. error: {request.error}");
        }
    }
}