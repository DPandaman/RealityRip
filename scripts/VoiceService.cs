// speaks the commentary out loud via local TTS server

using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;

public class VoiceService : MonoBehaviour
{
    [Header("local tts settings")]
    public string ttsUrl = "http://10.32.83.219:8004";
    public string voiceId = "Michael.wav";
    public int requestTimeout = 15;

    [Header("connections")]
    public AudioSource voiceSource; // drag drone audio source here

    public void Speak(string text){
        if (string.IsNullOrEmpty(text)) return;
        StartCoroutine(PostTTS(text));
    }

    IEnumerator PostTTS(string text){
        string safeText = text.Replace("\"", "\\\"").Replace("\n", " ");

        string json = $@"{{
            ""text"": ""{safeText}"",
            ""temperature"": 0.8,
            ""exaggeration"": 1.3,
            ""cfg_weight"": 0.5,
            ""speed_factor"": 1,
            ""seed"": 3000,
            ""language"": ""en"",
            ""voice_mode"": ""predefined"",
            ""split_text"": true,
            ""chunk_size"": 240,
            ""output_format"": ""wav"",
            ""predefined_voice_id"": ""{voiceId}""
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
            Debug.LogWarning($"TTS error: {request.error}");
        }
    }
}