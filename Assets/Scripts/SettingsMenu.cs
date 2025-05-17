using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private CanvasGroup gradient;
    public Button mainButton; // Assign your three-dot button
    public RectTransform[] menuItems; // Assign the menu items (Settings, Tour Presets, Custom Tour)
    public float spacing = 80f; // Space between menu items
    public float animationTime = 0.3f; // Animation speed
    public float xOffset = -100f; 

    private bool isExpanded = false;

    private void Start()
    {
        mainButton.onClick.AddListener(ToggleMenu);
        ResetPositions();

        // Add a listener to each menu button to collapse the menu when clicked
        foreach (var item in menuItems)
        {
            Button btn = item.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(CollapseMenu);
            }
        }
    }


    void ResetPositions()
    {
        foreach (var item in menuItems)
        {
            item.anchoredPosition = mainButton.GetComponent<RectTransform>().anchoredPosition; // Start at main button
            item.localScale = Vector3.zero; // Hide items at start
            item.GetComponent<CanvasGroup>().alpha = 0;
        }
    }

    public void ToggleMenu()
    {
        isExpanded = !isExpanded;

        if (isExpanded)
        {
            ExpandMenu();
        }
        else
        {
            CollapseMenu();
        }
    }

    void ExpandMenu()
    {
        gradient.DOFade(1f, 0.3f).SetEase(Ease.InQuad); 

        for (int i = 0; i < menuItems.Length; i++)
        {
            Vector2 targetPos = mainButton.GetComponent<RectTransform>().anchoredPosition 
                                + new Vector2(xOffset, -spacing * (i + 1)); // Expands downward & shifts left

            menuItems[i].DOAnchorPos(targetPos, animationTime)
                        .SetEase(Ease.OutBack);

            menuItems[i].DOScale(1, animationTime).SetEase(Ease.OutBack);
            menuItems[i].GetComponent<CanvasGroup>().DOFade(1, animationTime);
        }
    }


    void CollapseMenu()
    {
        isExpanded = false; // Make sure the state is updated
        gradient.DOFade(0f, 0.3f).SetEase(Ease.OutQuad); 

        foreach (var item in menuItems)
        {
            Vector2 targetPos = mainButton.GetComponent<RectTransform>().anchoredPosition + new Vector2(-30f, 0); // Shifted left
            
            item.DOAnchorPos(targetPos, animationTime)
                .SetEase(Ease.InBack);

            item.DOScale(0, animationTime).SetEase(Ease.InBack);

            CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0, animationTime); // Fade out
            }
        }
    }

}
