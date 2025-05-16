using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Handles multi-floor navigation, ensuring users are guided correctly between floors.
/// </summary>
public class FloorTransitionManager : MonoBehaviour
{
    [SerializeField]    
    private ARSession session;

    [SerializeField]
    private NavigationController navigationController;

    [SerializeField]
    private TargetHandler targetHandler;

    /// <summary>
    /// The transform (e.g., ARSessionOrigin) that is repositioned upon floor transition.
    /// </summary>
    [SerializeField]
    private ARSessionOrigin sessionOrigin;

    [SerializeField]
    private GameObject map;

    [SerializeField]
    private QrCodeRecenter qrCodeScanner;

    [SerializeField]
    private GameObject qrCodeScanningPanel;

    /// <summary>
    /// Current floor of the user.
    /// </summary>
    public int currentFloor;
    public string currentBuilding;

    /// <summary>
    /// The final POI the user originally selected.
    /// </summary>
    private string pendingTargetName;

    /// <summary>
    /// The floor of the final destination POI.
    /// </summary>
    private int targetFloor;

    private string targetTransitionPOI;

    private TargetFacade recenterTarget;

    public Button scanQRButton;

    private bool isScanning = false;

    // UI Elements
    [SerializeField] private GameObject floorTransitionPanel;
    [SerializeField] private TextMeshProUGUI floortransitionText;
    [SerializeField] private Button proceedButton;

    public enum FloorState
    {
        Idle,
        NavigatingSameFloor,
        NavigatingToTransition,
        FloorTransitionPrompt,
        NavigatingNewFloor
    }

    private FloorState currentState = FloorState.Idle;

    /// <summary>
    /// Hardcoded mapping of transition POIs to their designated recenter points.
    /// </summary>
    private List<string> transitionPOINames = new List<string>
    {
        // "B1 Entrance",
        // "120",
        "B1 1F Stairs",
        "B2 1F Stairs 1",
        "B2 1F Stairs 2",
        "B1 2F Stairs",
        "B2 2F Stairs 1",
        "B2 2F Stairs 2",
    };

    // -----------------------------------------------------------------------------------------
    // Initialization
    // -----------------------------------------------------------------------------------------

    private void Start()
    {
        currentState = FloorState.Idle;

        // Ensure UI is properly set up
        floorTransitionPanel.SetActive(false);
        // proceedButton.onClick.AddListener(OnUserConfirmedFloorChange);
    }

    void Update()
    {
        scanQRButton.onClick.RemoveAllListeners();
        
        if(!isScanning){
            scanQRButton.onClick.AddListener(ConfirmationPrompt);
        } else {
            scanQRButton.onClick.AddListener(OnUserConfirmedFloorChange);
        }
    }


    // -----------------------------------------------------------------------------------------
    // Public entry points:
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Called when a user selects a POI. Determines if navigation is same-floor or multi-floor.
    /// </summary>
    public void OnPOISelected(string poiName)
    {
        TargetFacade target = targetHandler.GetCurrentTargetByTargetText(poiName);
        if (target == null)
        {
            Debug.LogWarning($"[FloorTransitionManager] POI not found: {poiName}");
            return;
        }

        if (target.Floor == currentFloor)
        {
            // ✅ Same floor: navigate normally
            currentState = FloorState.NavigatingSameFloor;
            navigationController.ActivateNavigation(target.transform.position);
        }
        else
        {
            // ✅ Different floor: guide the user to the nearest transition point first
            pendingTargetName = poiName;
            targetFloor = target.Floor;
            currentState = FloorState.NavigatingToTransition;

            Vector3 transitionPos = FindNearestTransitionPoint(currentFloor, currentBuilding);
            if (transitionPos == Vector3.zero)
            {
                Debug.LogWarning("[FloorTransitionManager] No transitions found on this floor!");
                return;
            }

            navigationController.ActivateNavigation(transitionPos);
        }
    }

    /// <summary>
    /// Called when the user arrives at a transition POI.
    /// </summary>
    public void OnArrivedAtPOI(string arrivedPoiName)
    {
        if (currentState == FloorState.NavigatingToTransition && transitionPOINames.Contains(arrivedPoiName))
        {
            currentState = FloorState.FloorTransitionPrompt;
            ShowFloorTransitionPrompt();
        }
    }

    // -----------------------------------------------------------------------------------------
    // Floor Transition & Recenter Logic
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Displays a UI panel instructing the user to move floors.
    /// </summary>
    private void ShowFloorTransitionPrompt()
    {
        floorTransitionPanel.SetActive(true);
        map.SetActive(false);
        
        scanQRButton.gameObject.SetActive(false); // Hide the button initially
        StartCoroutine(ShowProceedButtonAfterDelay(6f)); // 5 seconds delay
    }

    private System.Collections.IEnumerator ShowProceedButtonAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        scanQRButton.gameObject.SetActive(true); // Show the button after delay
    }  

    public void ConfirmationPrompt()
    {
        isScanning = true;
        floortransitionText.text = $"Are you next to the QR marker now?";
    }

    /// <summary>
    /// Moves the user to the correct floor and resumes navigation.
    /// </summary>
    public void OnUserConfirmedFloorChange()
    {
        Debug.Log($"[FloorTransitionManager] User confirmed floor change to {targetTransitionPOI}");
        floorTransitionPanel.SetActive(false);


        qrCodeScanner.ToggleScanning();

        currentState = FloorState.NavigatingNewFloor;
    }

    public void QRCodeScanned()
    {
        TargetFacade finalTarget = targetHandler.GetCurrentTargetByTargetText(pendingTargetName);
        if (finalTarget != null)
        {   
            // navigationController.HandleArrival();
            navigationController.ActivateNavigation(finalTarget.transform.position);
            targetTransitionPOI = string.Empty;
            
        }
        
        currentState = FloorState.Idle;
    }

    private Vector3 FindNearestTransitionPoint(int floor, string building)
    {
        
        List<TargetFacade> transitionPoints = targetHandler.GetTransitionPOIs();
        
        if (transitionPoints.Count == 0)
            return Vector3.zero;

        Vector3 userPos = sessionOrigin.transform != null ? sessionOrigin.transform.position : Vector3.zero;

        TargetFacade pendingTarget = targetHandler.GetCurrentTargetByTargetText(pendingTargetName);

        TargetFacade nearest = null; // ✅ Declare outside

        if (currentFloor == 0)
        {
            Debug.Log($"3");
                nearest = transitionPoints
                .Where(tp => tp.Floor == floor)
                .OrderBy(tp => Vector3.Distance(userPos, tp.transform.position))
                .FirstOrDefault();
        } 
        else 
        {
            Debug.Log($"4");
            nearest = transitionPoints
                .Where(tp => (tp.Floor == floor) && (tp.Building == pendingTarget.Building))
                .OrderBy(tp => Vector3.Distance(userPos, tp.transform.position))
                .FirstOrDefault();
        }

        if (pendingTarget.Floor == 0)
        {
            if (nearest.Name == "B1 2F Stairs")
            {
                floortransitionText.text = $"Kindly go downstairs to reach the first floor.\n\nPlease scan the QR marker posted on the glass enclosed bulletin board downstairs.";
            }
            else if (nearest.Name == "B2 2F Stairs 1")
            {
                floortransitionText.text = $"Kindly go downstairs to reach the first floor.\n\nPlease scan the QR marker posted on the wall facing the plaques.";
            }
            else
            {
                floortransitionText.text = $"Kindly go downstairs to reach the first floor.\n\nPlease scan the QR marker posted on foot of the staircase.";
            }
        }
        else
        {
            if (nearest.Name == "B1 1F Stairs")
            {
                floortransitionText.text = $"Kindly go upstairs to reach the second floor.\n\nPlease scan the QR marker posted on the bulletin board upstairs.";
            }
            else if (nearest.Name == "B2 1F Stairs 1")
            {
                floortransitionText.text = $"Kindly go upstairs to reach the second floor.\n\nPlease scan the QR marker posted on the wall facing the other wall.";
            }
            else
            {
                floortransitionText.text = $"Kindly go upstairs to reach the second floor.\n\nPlease scan the QR marker posted on the wall upstairs.";
            }
        }
            

        targetTransitionPOI = nearest != null ? nearest.Name : string.Empty;

        return nearest != null ? nearest.transform.position : Vector3.zero;
    }

        /// <summary>
    /// Checks if a given POI is a transition point.
    /// </summary>
    public bool IsTransitionPOI(string poiName)
    {
        return transitionPOINames.Contains(poiName);
    }

    public int GetCurrentFloor()
    {
        return currentFloor;
    }

    public FloorState GetCurrentState()
    {
        return currentState;
    }

    public void UpdateDetailsFromScanning(int newFloor, string buildingName)
    {
        currentFloor = newFloor;
        currentBuilding = buildingName;
    }
    

}
