using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class CategoriesHandlerUI : MonoBehaviour
{
    [Header("Prefab + Content")]
    [SerializeField] private GameObject categoryItemPrefab;
    [SerializeField] private GameObject poiButtonPrefab;
    [SerializeField] private Transform categoryListContent;
    [SerializeField] private TargetHandler targetHandler;

    public PullUpUI pullUpUI;

    private void Start()
    {
        List<string> categories = new List<string> { "Laboratory", "Office", "Classroom" };

        foreach (string category in categories)
        {
            CreateCategoryBlock(category);
        }
    }

    private void CreateCategoryBlock(string categoryName)
    {
        GameObject categoryGO = Instantiate(categoryItemPrefab, categoryListContent);

        // Get references
        TMP_Text headerLabel = categoryGO.transform.Find("HeaderButton/Text (TMP)")?.GetComponent<TMP_Text>();
        Button headerButton = categoryGO.transform.Find("HeaderButton")?.GetComponent<Button>();
        RectTransform arrow = categoryGO.transform.Find("HeaderButton/Arrow")?.GetComponent<RectTransform>();
        Transform poiListParent = categoryGO.transform.Find("POIListParent");

        if (headerLabel != null) headerLabel.text = categoryName;

        if (poiListParent == null)
        {
            Debug.LogError("POIListParent not found in prefab.");
            return;
        }

        // Ensure CanvasGroup exists for fading
        CanvasGroup canvasGroup = poiListParent.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = poiListParent.gameObject.AddComponent<CanvasGroup>();

        // Initial state
        poiListParent.gameObject.SetActive(false);
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        bool isExpanded = false;
        bool isPopulated = false;

        headerButton.onClick.AddListener(() =>
        {
            if (!isPopulated)
            {
                List<TargetFacade> pois = targetHandler.CategoryPOIsNoFloorNo(categoryName);
                foreach (var poi in pois)
                {
                    GameObject button = Instantiate(poiButtonPrefab, poiListParent);
                    TMP_Text poiLabel = button.GetComponentInChildren<TMP_Text>();
                    poiLabel.text = poi.Name;

                    button.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        targetHandler.NavigateToPOI(poi.Name);
                        pullUpUI.ClosePanel();
                    });
                }

                isPopulated = true;
            }

            isExpanded = !isExpanded;

            if (isExpanded)
            {
                poiListParent.gameObject.SetActive(true); // must activate first
                canvasGroup.DOFade(1f, 0.25f);
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            else
            {
                // Fade out and THEN deactivate
                canvasGroup.DOFade(0f, 0.25f).OnComplete(() =>
                {
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                    poiListParent.gameObject.SetActive(false);
                });
            }

            // Rotate arrow
            if (arrow != null)
            {
                arrow.DORotate(new Vector3(0, 0, isExpanded ? 180 : 0), 0.3f).SetEase(Ease.OutExpo);
            }

        });
    }
}
