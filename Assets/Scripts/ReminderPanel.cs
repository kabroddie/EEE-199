using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReminderPanel : MonoBehaviour
{
    [SerializeField]
    public GameObject uiMaster;

    [SerializeField]
    public GameObject reminderPanel;
    // Start is called before the first frame update
    void Start()
    {
        uiMaster.SetActive(false);
        reminderPanel.SetActive(true);
    }

    // Update is called once per frame
    public void CloseReminderPanel()
    {
        uiMaster.SetActive(true);
        reminderPanel.SetActive(false);
    }
}

