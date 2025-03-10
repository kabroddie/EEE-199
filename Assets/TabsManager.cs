using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabsManager : MonoBehaviour
{
    public GameObject[] Tabs;
    public Button[] TabButtons; 
    
    private Color activeColor = Color.green;
    private Color inactiveColor = Color.white;
    private int defaultTab = 0;
    void Start()
    {
        SwitchToTab(defaultTab);
    }

    public void SwitchToTab(int TabID)
    {
        foreach(GameObject go in Tabs)
        {
            go.SetActive(false);
        }
        Tabs[TabID].SetActive(true);

        foreach(Button boon in TabButtons)
        {
            Image buttonImage = boon.GetComponent<Image>();
            buttonImage.color = inactiveColor;
        }
        Image buttonActive = TabButtons[TabID].GetComponent<Image>();
        buttonActive.color = activeColor;
    }
}

