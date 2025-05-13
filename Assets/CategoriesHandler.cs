using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CategoriesHandler : MonoBehaviour
{
    [SerializeField] private Button headerButton;
    [SerializeField] private TMP_Text headerLabel;
    [SerializeField] private GameObject dropdownContainer;
    [SerializeField] private Transform poiListParent;
    [SerializeField] private GameObject poiButtonPrefab;

    private bool isExpanded = false;
    private bool hasSpawned = false;
    private List<GameObject> spawnedPOIs = new List<GameObject>();
    private CategoriesDropdown manager; // ✅ Reference to the manager

    public void Initialize(string categoryName, List<TargetFacade> pois, TargetHandler handler, CategoriesDropdown dropdownManager)
    {
        headerLabel.text = categoryName;
        dropdownContainer.SetActive(false);
        manager = dropdownManager;

        headerButton.onClick.RemoveAllListeners();
        headerButton.onClick.AddListener(() =>
        {
            // Ask manager to collapse others before expanding this
            manager.CollapseAllExcept(this);

            isExpanded = !isExpanded;
            dropdownContainer.SetActive(isExpanded);

            if (isExpanded && !hasSpawned)
            {
                PopulatePOIs(pois, handler);
                hasSpawned = true;
            }
        });
    }

    private void PopulatePOIs(List<TargetFacade> pois, TargetHandler handler)
    {
        foreach (var poi in pois)
        {
            GameObject poiButton = Instantiate(poiButtonPrefab, poiListParent);
            poiButton.GetComponentInChildren<TMP_Text>().text = poi.Name;

            poiButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                handler.NavigateToPOI(poi.Name);
            });

            spawnedPOIs.Add(poiButton);
        }
    }

    public void Collapse()
    {
        isExpanded = false;
        dropdownContainer.SetActive(false);
    }
}