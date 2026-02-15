// speaks the commentary out loud 

using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;

public class VoiceService : MonoBehaviour
{
    [Header("api settings")]
    public string apiKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    public string ttsUrl = "https://api.openai.com/v1/audio/speech";

    [Header("connections")]
    public AudioSource voiceSource; // drag drone audio source here

    public void Speak(string text){
        // trigger speech request
        StartCoroutine(PostTTS(text));
    }

    IEnumerator PostTTS(string text){
        // build tts json
        string json = $@"{{
            ""model"": ""tts-1"",
            ""input"": ""{text}"",
            ""voice"": ""shimmer"" 
        }}";

        var request = new UnityWebRequest(ttsUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerAudioClip(ttsUrl, AudioType.MPEG);
        
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success){
            // play result
            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            voiceSource.clip = clip;
            voiceSource.Play();
        }
        else{
            Debug.LogError("tts error: " + request.error);
        }
    }
}