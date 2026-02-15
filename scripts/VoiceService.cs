// speaks the commentary out loud via local piper TTS

using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;

public class VoiceService : MonoBehaviour
{
    [Header("local tts settings")]
    public string ttsUrl = "http://10.32.83.219:5000";
    public int requestTimeout = 15;

    [Header("connections")]
    public AudioSource voiceSource; 

    public void Speak(string text){
        if (string.IsNullOrEmpty(text)) return;
        StartCoroutine(PostTTS(text));
    }

    IEnumerator PostTTS(string text){
        // sanitize for json
        string safeText = text.Replace("\"", "\\\"").Replace("\n", " ");

        string json = $@"{{
            ""text"": ""{safeText}""
        }}";

        var request = new UnityWebRequest(ttsUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerAudioClip(ttsUrl, AudioType.WAV);
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = requestTimeout;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success){
            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip != null && voiceSource != null){
                voiceSource.clip = clip;
                voiceSource.Play();
            }
        }
        else{
            Debug.LogWarning($"TTS unavailable: {request.error} (is piper running on {ttsUrl}?)");
        }
    }
}