using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsEnabler : MonoBehaviour
{
    public GameObject settings;
    public GameObject reminderPanel;
    public GameObject debugText;

    private void Start()
    {
        reminderPanel.SetActive(true);
        debugText.SetActive(false);
        
    }

    public void ScanFirst()
    {
        reminderPanel.SetActive(false);
        debugText.SetActive(true);
    }

    public void openSettings()
    {
        settings.SetActive(true);
    }

    public void closeSettings()
    {
        settings.SetActive(false);
    }
}
