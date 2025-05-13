using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class onboardingManager : MonoBehaviour
{
    [Header("Onboarding Setup")]
    public GameObject onboardingPanel;
    public RectTransform[] pages;
    public float fadeDuration = 0.5f;

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
            return;
        }

        // Ensure all pages have CanvasGroup and initialize states
        for (int i = 0; i < pages.Length; i++)
        {
            CanvasGroup group = EnsureCanvasGroup(pages[i]);
            bool isActive = i == 0;
            pages[i].gameObject.SetActive(isActive);
            group.alpha = isActive ? 1f : 0f;
            group.interactable = isActive;
            group.blocksRaycasts = isActive;
        }
    }

    public void NextPage()
    {
        if (currentPage < pages.Length - 1)
        {
            FadeToPage(currentPage + 1);
        }
        else
        {
            FinishOnboarding();
        }
    }

    private void FadeToPage(int nextPage)
    {
        RectTransform current = pages[currentPage];
        RectTransform next = pages[nextPage];

        CanvasGroup currentGroup = EnsureCanvasGroup(current);
        CanvasGroup nextGroup = EnsureCanvasGroup(next);

        // Prepare next page
        next.gameObject.SetActive(true);
        nextGroup.alpha = 0;
        nextGroup.interactable = false;
        nextGroup.blocksRaycasts = false;

        // Sequence fade-out current, then fade-in next
        Sequence sequence = DOTween.Sequence();
        sequence.Append(currentGroup.DOFade(0f, fadeDuration));
        sequence.AppendCallback(() =>
        {
            current.gameObject.SetActive(false);
            currentGroup.interactable = false;
            currentGroup.blocksRaycasts = false;

            nextGroup.DOFade(1f, fadeDuration).OnComplete(() =>
            {
                nextGroup.interactable = true;
                nextGroup.blocksRaycasts = true;
            });
        });

        currentPage = nextPage;
    }

    private void FinishOnboarding()
    {
        PlayerPrefs.SetInt(onboardingKey, 1);
        PlayerPrefs.Save();
        onboardingPanel.SetActive(false);
    }

    private CanvasGroup EnsureCanvasGroup(RectTransform rect)
    {
        CanvasGroup group = rect.GetComponent<CanvasGroup>();
        if (group == null)
            group = rect.gameObject.AddComponent<CanvasGroup>();
        return group;
    }
}
