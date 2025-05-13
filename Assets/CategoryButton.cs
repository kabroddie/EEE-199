using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CategoryButton : MonoBehaviour
{
    [SerializeField] private TMP_Text headerLabel;
    [SerializeField] private Transform poiListParent;
    [SerializeField] private GameObject poiButtonPrefab;
    [SerializeField] private TargetHandler targetHandler;

    private bool isPopulated = false;
    private bool isVisible = false;
    private string categoryName = "";

    public void Initialize(string category)
    {
        categoryName = category;
        headerLabel.text = category;

        GetComponent<Button>().onClick.AddListener(() =>
        {
            if (!isPopulated)
            {
                PopulatePOIs();
                isPopulated = true;
            }

            // Toggle visibility
            isVisible = !isVisible;
            poiListParent.gameObject.SetActive(isVisible);
        });

        poiListParent.gameObject.SetActive(false); // Start hidden
    }

    private void PopulatePOIs()
    {
        List<TargetFacade> pois = targetHandler.CategoryPOIsNoFloorNo(categoryName);

        foreach (var poi in pois)
        {
            GameObject button = Instantiate(poiButtonPrefab, poiListParent);
            button.GetComponentInChildren<TMP_Text>().text = poi.Name;

            button.GetComponent<Button>().onClick.AddListener(() =>
            {
                targetHandler.NavigateToPOI(poi.Name);
            });
        }
    }
}
