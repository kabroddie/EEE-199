using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ButtonGroupHandler : MonoBehaviour
{
    public ARSessionOrigin sessionOrigin;
    public GameObject canvas;
    public TargetHandler targetHandler;
    public TextMeshProUGUI titleText;
    public GameObject resultsPanel;
    public Transform resultsContent;
    public GameObject resultPrefab;
    public PullUpUI pullUp;
    private int floorNumber = 0;

    private string catName; 

    void Update()
    {
        
    }

    public void SetFloorNumber(int floor)
    {
        floorNumber = floor;
        ClearResults();
        targetHandler.clearCatPins();
        buttonClick(catName);
        Debug.Log($"[ButtonGroupHandler] Floor number set to {floor}");
    }

    public void BackButton(){
        canvas.SetActive(false);
        ClearResults();
        targetHandler.clearCatPins();
    }

    public void buttonClick(string categoryName){
        catName = categoryName;
        titleText.text = categoryName;
        Debug.Log(categoryName);
        canvas.SetActive(true);
        targetHandler.ShowPins(categoryName,floorNumber);

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

    public void FindNearestAmenity()
    {

        List<TargetFacade> matchingPOIs = targetHandler.CategoryPOIs(catName, floorNumber);

        Vector3 userPos = sessionOrigin.transform != null ? sessionOrigin.transform.position : Vector3.zero;

        TargetFacade nearest = matchingPOIs
            .OrderBy(tp => Vector3.Distance(userPos, tp.transform.position))
            .FirstOrDefault();

        // if (currentFloor == 0)
        // {
        //     Debug.Log($"3");
        //     nearest = matchingPOIs
        //     .Where(tp => tp.Floor == floor)
        //     .OrderBy(tp => Vector3.Distance(userPos, tp.transform.position))
        //     .FirstOrDefault();
        // }
        // else
        // {
        //     Debug.Log($"4");
        //     nearest = transitionPoints
        //         .Where(tp => (tp.Floor == floor) && (tp.Building == pendingTarget.Building))
        //         .OrderBy(tp => Vector3.Distance(userPos, tp.transform.position))
        //         .FirstOrDefault();
        // }

        canvas.SetActive(false);
        targetHandler.clearCatPins();
        ClearResults();
        targetHandler.NavigateToPOI(nearest.Name);
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
