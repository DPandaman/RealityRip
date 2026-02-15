// AI-driven waypoint generation via GPT-5.2 spatial reasoning

using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class RescueGoalGenerator : MonoBehaviour
{
    [Header("Simulation Parameters")]
    public GameObject goalPrefab; // visualization marker (torus)
    public LayerMask splatLayer;  // gaussian splat mesh collider layer

    [Header("AI Integration")]
    public AIService aiService;          // fallback local LLM
    public GoalManager goalManager;      // register spawned goals
    public float raycastMaxDistance = 50f;
    public float goalHoverOffset = 0.3f;

    [Header("GPT-5.2 (Trajectory)")]
    public string openaiApiUrl = "https://api.openai.com/v1/chat/completions";
    public string openaiModel = "gpt-5.2";
    public int requestTimeout = 30;
    private string openaiApiKey;

    // maintaining a list of active waypoints to manage scene clutter
    public List<Transform> activeGoals = new List<Transform>();

    [System.Serializable]
    public struct WaypointData {
        public string name;
        public float forward;
        public float right;
        public float up;
        public int priority;
    }

    void Awake()
    {
        openaiApiKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(openaiApiKey))
            Debug.LogWarning("RescueGoalGen: OPENAI_API_KEY not set, trajectory generation will use local fallback");
    }

    public void GenerateGoalsFromAI(string vlmDescription, string userPrompt,
                                    Transform droneTransform, System.Action<bool> onComplete)
    {
        ClearGoals();

        string systemPrompt = "You are a spatial reasoning engine for a drone simulator. " +
            "You receive a scene description from a vision model and a user's mission objective. " +
            "Identify 2-6 points of interest and output ONLY a JSON array. " +
            "Each entry: {\"name\": \"snake_case_string\", \"forward\": float_meters, \"right\": float_meters, \"up\": float_meters, \"priority\": int_1_is_highest}. " +
            "Use realistic distances 1-10m. Output nothing but the JSON array.";

        string userMsg = $"Scene: {vlmDescription}\nMission: {userPrompt}\nOutput JSON array of waypoints:";

        // use GPT-5.2 via OpenAI API if key is available, otherwise fall back to local Ollama
        if (!string.IsNullOrEmpty(openaiApiKey))
        {
            StartCoroutine(PostOpenAIRequest(systemPrompt, userMsg, droneTransform, onComplete));
        }
        else if (aiService != null)
        {
            aiService.SendPrompt(systemPrompt, userMsg, (response) =>
            {
                ProcessWaypointResponse(response, droneTransform, onComplete);
            });
        }
        else
        {
            Debug.LogWarning("RescueGoalGen: no AI available, using fallback");
            SpawnFallbackPattern(droneTransform);
            onComplete?.Invoke(activeGoals.Count > 0);
        }
    }

    IEnumerator PostOpenAIRequest(string systemPrompt, string userMsg,
                                  Transform droneTransform, System.Action<bool> onComplete)
    {
        string safeSystem = systemPrompt.Replace("\"", "\\\"").Replace("\n", " ");
        string safeUser = userMsg.Replace("\"", "\\\"").Replace("\n", " ");

        string json = $@"{{
            ""model"": ""{openaiModel}"",
            ""messages"": [
                {{ ""role"": ""system"", ""content"": ""{safeSystem}"" }},
                {{ ""role"": ""user"", ""content"": ""{safeUser}"" }}
            ]
        }}";

        var request = new UnityWebRequest(openaiApiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openaiApiKey);
        request.timeout = requestTimeout;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string content = ParseResponseContent(request.downloadHandler.text);
            ProcessWaypointResponse(content, droneTransform, onComplete);
        }
        else
        {
            Debug.LogError($"RescueGoalGen: GPT-5.2 request failed: {request.error}");
            SpawnFallbackPattern(droneTransform);
            onComplete?.Invoke(activeGoals.Count > 0);
        }
    }

    string ParseResponseContent(string json)
    {
        string key = "\"content\":";
        int keyIndex = json.LastIndexOf(key);
        if (keyIndex == -1) return null;

        int start = keyIndex + key.Length;
        while (start < json.Length && (json[start] == ' ' || json[start] == '\n' || json[start] == '\r'))
            start++;

        if (start >= json.Length || json[start] != '"') return null;
        start++;

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

    void ProcessWaypointResponse(string response, Transform droneTransform, System.Action<bool> onComplete)
    {
        if (string.IsNullOrEmpty(response))
        {
            Debug.LogWarning("RescueGoalGen: empty AI response, using fallback");
            SpawnFallbackPattern(droneTransform);
            onComplete?.Invoke(activeGoals.Count > 0);
            return;
        }

        Debug.Log($"RescueGoalGen: AI response: {response}");

        List<WaypointData> waypoints = ParseWaypointJSON(response);

        if (waypoints.Count == 0)
        {
            Debug.LogWarning("RescueGoalGen: failed to parse waypoints, using fallback");
            SpawnFallbackPattern(droneTransform);
            onComplete?.Invoke(activeGoals.Count > 0);
            return;
        }

        // sort by priority
        waypoints.Sort((a, b) => a.priority.CompareTo(b.priority));

        foreach (var wp in waypoints)
        {
            Vector3 worldPos = droneTransform.position
                + droneTransform.forward * wp.forward
                + droneTransform.right * wp.right
                + Vector3.up * wp.up;

            Vector3 snapped = ValidateAndSnap(worldPos, droneTransform);
            SpawnGoal(snapped, wp.name);
        }

        Debug.Log($"RescueGoalGen: spawned {activeGoals.Count} AI-generated waypoints");
        onComplete?.Invoke(activeGoals.Count > 0);
    }

    // legacy signature for backward compatibility
    public void GenerateGoalsFromAI(string aiResponse, Transform droneTransform)
    {
        GenerateGoalsFromAI(aiResponse, "", droneTransform, null);
    }

    List<WaypointData> ParseWaypointJSON(string json)
    {
        List<WaypointData> results = new List<WaypointData>();

        // strip markdown code fences if present
        if (json.Contains("```"))
        {
            int fenceStart = json.IndexOf("```");
            int contentStart = json.IndexOf("\n", fenceStart);
            if (contentStart == -1) contentStart = fenceStart + 3;
            else contentStart++;

            int fenceEnd = json.IndexOf("```", contentStart);
            if (fenceEnd == -1) fenceEnd = json.Length;

            json = json.Substring(contentStart, fenceEnd - contentStart).Trim();
        }

        // find the array
        int arrayStart = json.IndexOf('[');
        int arrayEnd = json.LastIndexOf(']');
        if (arrayStart == -1 || arrayEnd == -1 || arrayEnd <= arrayStart) return results;

        string arrayContent = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);

        // parse each object block
        int searchFrom = 0;
        while (searchFrom < arrayContent.Length)
        {
            int objStart = arrayContent.IndexOf('{', searchFrom);
            if (objStart == -1) break;

            int objEnd = arrayContent.IndexOf('}', objStart);
            if (objEnd == -1) break;

            string block = arrayContent.Substring(objStart, objEnd - objStart + 1);
            searchFrom = objEnd + 1;

            WaypointData wp = new WaypointData();
            wp.name = ExtractStringValue(block, "name");
            wp.forward = ExtractFloatValue(block, "forward");
            wp.right = ExtractFloatValue(block, "right");
            wp.up = ExtractFloatValue(block, "up");
            wp.priority = (int)ExtractFloatValue(block, "priority");

            if (string.IsNullOrEmpty(wp.name)) wp.name = $"waypoint_{results.Count}";
            if (wp.priority == 0) wp.priority = results.Count + 1;

            results.Add(wp);
        }

        return results;
    }

    string ExtractStringValue(string block, string key)
    {
        string pattern = $"\"{key}\"";
        int keyIdx = block.IndexOf(pattern);
        if (keyIdx == -1) return null;

        int colonIdx = block.IndexOf(':', keyIdx + pattern.Length);
        if (colonIdx == -1) return null;

        // find opening quote
        int quoteStart = block.IndexOf('"', colonIdx + 1);
        if (quoteStart == -1) return null;

        int quoteEnd = block.IndexOf('"', quoteStart + 1);
        if (quoteEnd == -1) return null;

        return block.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
    }

    float ExtractFloatValue(string block, string key)
    {
        string pattern = $"\"{key}\"";
        int keyIdx = block.IndexOf(pattern);
        if (keyIdx == -1) return 0f;

        int colonIdx = block.IndexOf(':', keyIdx + pattern.Length);
        if (colonIdx == -1) return 0f;

        // collect numeric chars after colon
        int numStart = colonIdx + 1;
        while (numStart < block.Length && (block[numStart] == ' ' || block[numStart] == '\t'))
            numStart++;

        int numEnd = numStart;
        while (numEnd < block.Length && (char.IsDigit(block[numEnd]) || block[numEnd] == '.' || block[numEnd] == '-'))
            numEnd++;

        if (numEnd == numStart) return 0f;

        string numStr = block.Substring(numStart, numEnd - numStart);
        if (float.TryParse(numStr, System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.InvariantCulture, out float val))
            return val;

        return 0f;
    }

    Vector3 ValidateAndSnap(Vector3 estimated, Transform droneTransform)
    {
        // strategy 1: raycast down from above estimated position
        Vector3 rayOrigin = estimated + Vector3.up * 10f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hitDown, raycastMaxDistance, splatLayer))
        {
            return hitDown.point + Vector3.up * goalHoverOffset;
        }

        // strategy 2: raycast from drone toward estimated position
        Vector3 dirToTarget = (estimated - droneTransform.position).normalized;
        if (Physics.Raycast(droneTransform.position, dirToTarget, out RaycastHit hitForward, raycastMaxDistance, splatLayer))
        {
            return hitForward.point + Vector3.up * goalHoverOffset;
        }

        // strategy 3: accept raw position
        Debug.LogWarning($"RescueGoalGen: no surface found for waypoint, using raw position");
        return estimated;
    }

    void SpawnGoal(Vector3 pos, string name)
    {
        GameObject g = Instantiate(goalPrefab, pos, Quaternion.identity);
        g.name = name;
        activeGoals.Add(g.transform);
        if (goalManager != null) goalManager.AddLandmark(name, g.transform);
    }

    void SpawnFallbackPattern(Transform droneTransform)
    {
        Debug.LogWarning("RescueGoalGen: using default search pattern");
        SpawnGoal(droneTransform.position + droneTransform.forward * 3f, "Search_Area_Alpha");
        SpawnGoal(droneTransform.position + droneTransform.forward * 5f + droneTransform.right * 2f, "Search_Area_Beta");
    }

    public void ClearGoals()
    {
        foreach (var t in activeGoals) Destroy(t.gameObject);
        activeGoals.Clear();
        if (goalManager != null) goalManager.ClearGeneratedLandmarks();
    }
}
