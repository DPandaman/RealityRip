// speaks the commentary out loud via local TTS

using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using System.Globalization;

public class VoiceService : MonoBehaviour
{
    [Header("TTS Endpoint")]
    public string ttsUrl = "http://10.32.83.219:8004/tts";
    public int requestTimeout = 15;

    [Header("TTS Parameters")]
    [Range(0f, 2f)] public float temperature = 0.8f;
    [Range(0f, 2f)] public float exaggeration = 1.3f;
    [Range(0f, 2f)] public float cfgWeight = 0.5f;
    [Range(0.25f, 2f)] public float speedFactor = 1f;
    public int seed = 3000;
    public string language = "en";
    public string voiceMode = "predefined";
    public bool splitText = true;
    public int chunkSize = 240;
    public string outputFormat = "wav";
    public string predefinedVoiceId = "Michael.wav";

    [Header("connections")]
    public AudioSource voiceSource; 

    public void Speak(string text){
        if (string.IsNullOrEmpty(text)) return;
        StartCoroutine(PostTTS(text));
    }

    IEnumerator PostTTS(string text){
        // sanitize for json
        string safeText = EscapeJson(text);
        string safeLanguage = EscapeJson(language);
        string safeVoiceMode = EscapeJson(voiceMode);
        string safeOutputFormat = EscapeJson(outputFormat);
        string safeVoiceId = EscapeJson(predefinedVoiceId);

        string json = $@"{{
            ""text"": ""{safeText}"",
            ""temperature"": {ToInvariant(temperature)},
            ""exaggeration"": {ToInvariant(exaggeration)},
            ""cfg_weight"": {ToInvariant(cfgWeight)},
            ""speed_factor"": {ToInvariant(speedFactor)},
            ""seed"": {seed},
            ""language"": ""{safeLanguage}"",
            ""voice_mode"": ""{safeVoiceMode}"",
            ""split_text"": {(splitText ? "true" : "false")},
            ""chunk_size"": {chunkSize},
            ""output_format"": ""{safeOutputFormat}"",
            ""predefined_voice_id"": ""{safeVoiceId}""
        }}";

        var request = new UnityWebRequest(ttsUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerAudioClip(ttsUrl, GetAudioType(outputFormat));
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
            Debug.LogWarning($"TTS unavailable: {request.error} (is TTS running on {ttsUrl}?)");
        }
    }

    static string EscapeJson(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input.Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }

    static string ToInvariant(float value)
        => value.ToString(CultureInfo.InvariantCulture);

    static AudioType GetAudioType(string format)
    {
        if (string.IsNullOrEmpty(format)) return AudioType.WAV;
        switch (format.Trim().ToLowerInvariant())
        {
            case "mp3":
            case "mpeg":
                return AudioType.MPEG;
            case "ogg":
            case "ogg_vorbis":
            case "vorbis":
                return AudioType.OGGVORBIS;
            case "wav":
            default:
                return AudioType.WAV;
        }
    }
}
