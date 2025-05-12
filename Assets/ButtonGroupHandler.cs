using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ButtonGroupHandler : MonoBehaviour
{
    public GameObject canvas;
    public TargetHandler targetHandler;
    public TextMeshProUGUI titleText;
    public GameObject resultsPanel;
    public Transform resultsContent;
    public GameObject resultPrefab;
    public PullUpUI pullUp;
    private int floorNumber = 0;

    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BackButton(){
        canvas.SetActive(false);
        ClearResults();
        targetHandler.clearCatPins();
    }

    public void buttonClick(string categoryName){
        titleText.text = categoryName;
        Debug.Log(categoryName);
        canvas.SetActive(true);
        targetHandler.ShowPins(categoryName);

        List<TargetFacade> matchingPOIs = targetHandler.CategoryPOIs(categoryName,floorNumber);

        foreach(TargetFacade poi in matchingPOIs){
            GameObject resultItem = Instantiate(resultPrefab, resultsContent);
            TMP_Text poiText = resultItem.GetComponentInChildren<TMP_Text>();
            poiText.text = poi.Name;

            Button poiButton = resultItem.GetComponent<Button>();
            poiButton.onClick.AddListener(() => SelectPOI(poi.Name));
        }
    }

    private void SelectPOI(string poiName)
    {
        Debug.Log($"[SearchBar] POI Selected: {poiName}");

        canvas.SetActive(false);
        targetHandler.clearCatPins();
        ClearResults();
        targetHandler.NavigateToPOI(poiName);
        pullUp.ClosePanel();
        
    }

    private void ClearResults()
    {
        foreach (Transform child in resultsContent)
        {
            Destroy(child.gameObject);
        }
    }




}
