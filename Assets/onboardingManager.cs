using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class onboardingManager : MonoBehaviour
{
    [Header("Onboarding Setup")]
    public GameObject onboardingPanel;
    public const string onboardingKey = "hasSeenOnboarding";

    private int currentPage = 0;

    void Start()
    {
        if (PlayerPrefs.GetInt(onboardingKey, 0) == 0)
        {
            onboardingPanel.SetActive(true);
            PlayerPrefs.SetInt(onboardingKey, 1);
            PlayerPrefs.Save();
        }
        else
        {
            onboardingPanel.SetActive(false);
            return;
        }
    }

}
