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

    // UI Elements
    [SerializeField] private GameObject floorTransitionPanel;
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
    private Dictionary<string, string> transitionToRecenterMap = new Dictionary<string, string>()
    {
        { "Old Entrance", "New1stLink" },
        { "120", "Entry" },
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

            Vector3 transitionPos = FindNearestTransitionPoint(currentFloor);
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
        if (currentState == FloorState.NavigatingToTransition && transitionToRecenterMap.ContainsKey(arrivedPoiName))
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
    }

    /// <summary>
    /// Moves the user to the correct floor and resumes navigation.
    /// </summary>
    public void OnUserConfirmedFloorChange()
    {
        floorTransitionPanel.SetActive(false);
        map.SetActive(true);
        // qrCodeScanningPanel.SetActive(true);
        
        foreach (var transition in transitionToRecenterMap)
        {
            if (transition.Key == targetTransitionPOI)
            {
                recenterTarget = targetHandler.GetCurrentTargetByTargetText(transition.Value);
                break;
            }
        }

        if (recenterTarget != null)
        {
            Debug.Log($"[FloorTransitionManager] Transition to recenter map: {recenterTarget.Name}");
        }
        else
        {
            Debug.LogError("[FloorTransitionManager] ❌ recenterTarget is NULL before mapping!");
        }

        
        // ✅ Update floor and recenter
        currentFloor = recenterTarget.Floor;

        // session.Reset();
        // sessionOrigin.transform.position = recenterTarget.transform.position;
        // sessionOrigin.transform.rotation = recenterTarget.transform.rotation;

        qrCodeScanner.ToggleScanning();

        currentState = FloorState.NavigatingNewFloor;

        // ✅ Resume navigation to the final POI
        // TargetFacade finalTarget = targetHandler.GetCurrentTargetByTargetText(pendingTargetName);
        // if (finalTarget != null)
        // {   
        //     navigationController.HandleArrival();
        //     navigationController.ActivateNavigation(finalTarget.transform.position);
        //     targetTransitionPOI = string.Empty;
        // }
        // else
        // {
        //     Debug.LogWarning($"[FloorTransitionManager] Final target not found: {pendingTargetName}");
        //     currentState = FloorState.Idle;
        // }
    }

    public void QRCodeScanned()
    {
        TargetFacade finalTarget = targetHandler.GetCurrentTargetByTargetText(pendingTargetName);
        if (finalTarget != null)
        {   
            navigationController.HandleArrival();
            navigationController.ActivateNavigation(finalTarget.transform.position);
            targetTransitionPOI = string.Empty;
        }
        else
        {
            Debug.LogWarning($"[FloorTransitionManager] Final target not found: {pendingTargetName}");
            currentState = FloorState.Idle;
        }
    }

    private Vector3 FindNearestTransitionPoint(int floor)
    {
        
        List<TargetFacade> transitionPoints = targetHandler.GetTransitionPOIs();
        
        if (transitionPoints.Count == 0)
            return Vector3.zero;

        Vector3 userPos = sessionOrigin.transform != null ? sessionOrigin.transform.position : Vector3.zero;

        TargetFacade pendingTarget = targetHandler.GetCurrentTargetByTargetText(pendingTargetName);

        TargetFacade nearest = null; // ✅ Declare outside


        foreach (var tp in transitionPoints)
        {
            Debug.Log($"hotdog: Name: {tp.Name} Purpose: {tp.Purpose} Floor: {tp.Floor} Building: {tp.Building}");
        }

        Debug.Log($"pending target: {pendingTarget.Building} Floor: {floor}");

        if (pendingTarget != null && pendingTarget.Building == "New" && floor == 0)
        {
            nearest = transitionPoints.Find(tp => tp.Name == "Old Entrance");
        }
        else if (pendingTarget != null && pendingTarget.Building == "Old" && floor == 1)
        {
            nearest = transitionPoints.Find(tp => tp.Name == "120");
        }
        else
        {
            nearest = transitionPoints
                .Where(tp => tp.Floor == floor)
                .OrderBy(tp => Vector3.Distance(userPos, tp.transform.position))
                .FirstOrDefault();
        }

        targetTransitionPOI = nearest != null ? nearest.Name : string.Empty;

        return nearest != null ? nearest.transform.position : Vector3.zero;
    }

        /// <summary>
    /// Checks if a given POI is a transition point.
    /// </summary>
    public bool IsTransitionPOI(string poiName)
    {
        return transitionToRecenterMap.ContainsKey(poiName);
    }

    public int GetCurrentFloor()
    {
        return currentFloor;
    }

    public FloorState GetCurrentState()
    {
        return currentState;
    }

    public void UpdateCurrentFloorFromScanning(int newFloor)
    {
        currentFloor = newFloor;
    }
    

}
