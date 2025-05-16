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

    // Global reference to track currently expanded category
    private CategoryItem currentlyExpandedCategory = null;

    private void Start()
    {
        List<string> categories = new List<string> { "Laboratory", "Office", "Classroom" };

        foreach (string category in categories)
        {
            CreateCategoryBlock(category);
        }
    }

    // Helper class to keep track of category items
    private class CategoryItem
    {
        public Transform poiListParent;
        public CanvasGroup canvasGroup;
        public RectTransform arrow;
        public string categoryName;
        public bool isPopulated;

        public CategoryItem(Transform poiListParent, CanvasGroup canvasGroup, RectTransform arrow, string categoryName)
        {
            this.poiListParent = poiListParent;
            this.canvasGroup = canvasGroup;
            this.arrow = arrow;
            this.categoryName = categoryName;
            this.isPopulated = false;
        }

        public void Expand()
        {
            poiListParent.gameObject.SetActive(true);
            canvasGroup.DOFade(1f, 0.25f);
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            
            if (arrow != null)
            {
                arrow.DORotate(new Vector3(0, 0, 90), 0.3f).SetEase(Ease.OutExpo);
            }
            
            Debug.Log($"Category expanded: {categoryName}");
        }

        public void Collapse()
        {
            canvasGroup.DOFade(0f, 0.25f).OnComplete(() =>
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                poiListParent.gameObject.SetActive(false);
            });

            if (arrow != null)
            {
                arrow.DORotate(new Vector3(0, 0, 0), 0.3f).SetEase(Ease.OutExpo);
            }
            
            Debug.Log($"Category collapsed: {categoryName}");
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

        CanvasGroup canvasGroup = poiListParent.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = poiListParent.gameObject.AddComponent<CanvasGroup>();

        // Set default collapsed state
        poiListParent.gameObject.SetActive(false);
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        // Initialize arrow rotation for collapsed state (pointing right)
        if (arrow != null)
        {
            arrow.localRotation = Quaternion.Euler(0, 0, 0);
        }

        // Create a CategoryItem to manage this category
        CategoryItem categoryItem = new CategoryItem(poiListParent, canvasGroup, arrow, categoryName);

        headerButton.onClick.AddListener(() =>
        {
            // Populate POIs if needed
            if (!categoryItem.isPopulated)
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
                categoryItem.isPopulated = true;
            }

            // Case 1: This category is already open - close it
            if (currentlyExpandedCategory == categoryItem)
            {
                categoryItem.Collapse();
                currentlyExpandedCategory = null;
            }
            // Case 2: Another category is open - close it and open this one
            else if (currentlyExpandedCategory != null)
            {
                // Close the currently expanded category
                currentlyExpandedCategory.Collapse();
                
                // Open this category
                categoryItem.Expand();
                currentlyExpandedCategory = categoryItem;
            }
            // Case 3: No category is open - open this one
            else
            {
                categoryItem.Expand();
                currentlyExpandedCategory = categoryItem;
            }
        });
    }
}