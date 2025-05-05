using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class onboardingManager : MonoBehaviour
{
    public RectTransform slideRoot;
    public RectTransform viewport; 
    public RectTransform[] pages;
    public float slideDuration = 0.5f;

    public GameObject onboardingPanel;
    public const string onboardingKey = "hasSeenOnboarding";

    private int currentPage = 0;

    void Start()
    {
        if (PlayerPrefs.GetInt(onboardingKey, 0) == 0)
        {
            onboardingPanel.SetActive(true);
        }
        else
        {
            onboardingPanel.SetActive(false);
        }

        GoToPage(0, true);
    }

    public void NextPage()
    {
        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            GoToPage(currentPage);
        }
        else
        {
            FinishOnboarding();
        }
    }

    private void GoToPage(int pageID, bool instant = false)
    {
        float pageWidth = viewport.rect.width;
        float targetX = -pageID * pageWidth;

        if (instant)
        {
            slideRoot.anchoredPosition = new Vector2(targetX, slideRoot.anchoredPosition.y);
        }
        else
        {
            slideRoot.DOAnchorPosX(targetX, slideDuration).SetEase(Ease.OutCubic);
        }
    }

    void FinishOnboarding()
    {
        PlayerPrefs.SetInt(onboardingKey, 1);
        PlayerPrefs.Save();
        onboardingPanel.SetActive(false);
    }
}
