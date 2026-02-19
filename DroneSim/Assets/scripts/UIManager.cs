using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField motiveInputField;
    public TMP_Dropdown mapDropdown;
    public TMP_Dropdown vibeDropdown;
    public Button generateButton;

    [Header("Logic Links")]
    public MissionController missionController;
    public DroneCommentator commentator;

    void Start()
    {
        if (generateButton != null)
            generateButton.onClick.AddListener(OnGenerateClicked);

        PopulateDropdowns();
    }

    void PopulateDropdowns()
    {
        // populate map dropdown from commentator profiles
        if (mapDropdown != null && commentator != null)
        {
            mapDropdown.ClearOptions();
            string[] mapNames = commentator.GetMapNames();
            if (mapNames.Length > 0)
                mapDropdown.AddOptions(new List<string>(mapNames));
            else
                mapDropdown.AddOptions(new List<string> { "disaster", "stanford_quad", "freestyle" });
        }

        // populate vibe dropdown
        if (vibeDropdown != null)
        {
            vibeDropdown.ClearOptions();
            vibeDropdown.AddOptions(new List<string> { "default", "serious", "chill", "hype", "drill sergeant" });
        }
    }

    public void OnGenerateClicked()
    {
        if (missionController == null) return;

        string userText = motiveInputField != null ? motiveInputField.text : "";
        if (string.IsNullOrEmpty(userText))
        {
            Debug.LogWarning("UIManager: empty prompt, ignoring");
            return;
        }

        // set map profile
        if (mapDropdown != null && commentator != null)
        {
            string mapName = mapDropdown.options[mapDropdown.value].text;
            commentator.SetMapProfile(mapName);
        }

        // set vibe
        if (vibeDropdown != null && commentator != null)
        {
            string vibe = vibeDropdown.options[vibeDropdown.value].text;
            commentator.SetVibe(vibe);
        }

        // trigger the mission pipeline
        missionController.StartMissionFromPrompt(userText);

        Debug.Log("UIManager: mission started with map profile + vibe.");
    }
}