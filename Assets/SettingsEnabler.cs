using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsEnabler : MonoBehaviour
{
    public GameObject settings;
    public void openSettings()
    {
        settings.SetActive(true);
    }

    public void closeSettings()
    {
        settings.SetActive(false);
    }
}
