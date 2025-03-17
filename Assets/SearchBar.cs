using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class SearchBar : MonoBehaviour
{
    [SerializeField] private TMP_InputField searchBar; 
    [SerializeField] private RectTransform searchContainer; 
    [SerializeField] private CanvasGroup directoryText;
    [SerializeField] private GameObject searchResultsPanel;
    [SerializeField] private Transform searchResultsContent;
    [SerializeField] private GameObject searchResultPrefab;
    [SerializeField] private TargetHandler targetHandler;
    [SerializeField] private PullUpUI pullUp;
    // Start is called before the first frame update

    private Vector2 originalPos;
    private Vector2 focusedPos;
    private bool isSearchActive;

    void Start()
    {
        originalPos = searchContainer.anchoredPosition;
        focusedPos = new Vector2(originalPos.x, 625f);

        searchBar.onSelect.AddListener(_ => FocusSearchBar());
        //searchBar.onDeselect.AddListener(_ => UnfocusedSearchBar());
        searchBar.onValueChanged.AddListener(UpdateSearchResults);

    }

    // Update is called once per frame
    public void FocusSearchBar()
    {
        if (isSearchActive) return;
        isSearchActive = true;
        searchContainer.DOAnchorPos(focusedPos, 0.5f).SetEase(Ease.OutExpo);
        directoryText.DOFade(0f,0.3f).SetEase(Ease.OutQuad);

        searchResultsPanel.SetActive(true);
        UpdateSearchResults("");
    }

    public void UnfocusedSearchBar()
    {
        if(!isSearchActive) return;
        isSearchActive = false;
        searchBar.text = "";
        searchContainer.DOAnchorPos(originalPos, 0.5f).SetEase(Ease.OutExpo);
        directoryText.DOFade(1f,0.3f).SetEase(Ease.OutQuad);

        searchResultsPanel.SetActive(false);
        ClearSearchResults();

    }

    private void UpdateSearchResults(string searchText)
    {
        ClearSearchResults();

        List<string> matchingPOIs = string.IsNullOrWhiteSpace(searchText)
            ? targetHandler.GetNonQRTargetNames() 
            : targetHandler.SearchPOIs(searchText);

        if (matchingPOIs.Count > 0)
        {
            searchResultsPanel.SetActive(true);
        }

        foreach (string poi in matchingPOIs)
        {
            GameObject resultItem = Instantiate(searchResultPrefab, searchResultsContent);
            TMP_Text poiText = resultItem.GetComponentInChildren<TMP_Text>();
            poiText.text = poi;

            Button poiButton = resultItem.GetComponent<Button>();
            poiButton.onClick.AddListener(() => SelectPOI(poi));
        }
    }


    private void ClearSearchResults()
    {
        foreach (Transform child in searchResultsContent)
        {
            Destroy(child.gameObject);
        }
    }

    private void SelectPOI(string poiName)
    {
        Debug.Log($"[SearchBar] POI Selected: {poiName}");

        if (string.IsNullOrWhiteSpace(poiName))
        {
            Debug.LogWarning("[SearchBar] POI Name is Empty!");
            return;
        }

        targetHandler.NavigateToPOI(poiName);
        searchBar.text = poiName;
        UnfocusedSearchBar();
        pullUp.ClosePanel();
    }
}
