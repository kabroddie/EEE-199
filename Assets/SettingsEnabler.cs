using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsEnabler : MonoBehaviour
{
    public GameObject settings;
    public GameObject reminderPanel;

    private void Start()
    {
        reminderPanel.SetActive(true);
    }

    public void ScanFirst()
    {
        reminderPanel.SetActive(false);
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
