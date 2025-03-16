using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TargetHandler : MonoBehaviour
{
    [SerializeField]
    private NavigationController navigationController;
    [SerializeField]
    private TextAsset targetModelData;
    [SerializeField]
    private TMP_Dropdown targetDataDropdown;

    [SerializeField]
    private GameObject targetObjectPrefab;
    [SerializeField]
    private Transform[] targetObjectsParentTransforms;

    [SerializeField]
    private LineRenderer lineRenderer; // ✅ Reference to LineRenderer

    private List<TargetFacade> currentTargetItems = new List<TargetFacade>();
    private List<TargetFacade> qrTargetItems = new List<TargetFacade>(); // ✅ Store QR targets as TargetFacade

    private Dictionary<Vector3, GameObject> targetPins = new Dictionary<Vector3, GameObject>(); // ✅ Store pins by position
    private Vector3 tourStartingPoint; // ✅ Stores the dynamically assigned tour start



    private void Start()
    {
        GenerateTargetItems();
        FillDropdownWithTargetItems();
        SetStartingPoint(); // ✅ Automatically set the starting point at "Entry"
    }

    private void SetStartingPoint()
    {
        TargetFacade startingPoint = GetCurrentTargetByTargetText("Entry");
        if (startingPoint != null)
        {
            tourStartingPoint = startingPoint.transform.position;
        }
    }


    private void GenerateTargetItems()
    {
        
        IEnumerable<Target> targets = GenerateTargetDataFromSource();
        foreach (Target target in targets)
        {
            if (target.Purpose == "QR")
            {
                // ✅ Create a TargetFacade for QR targets, but do not instantiate a prefab
                TargetFacade qrTarget = CreateTargetFacadeWithoutInstantiation(target);
                qrTargetItems.Add(qrTarget);
            }
            else
            {

                currentTargetItems.Add(CreateTargetFacade(target));

            }
        }
    }

    private IEnumerable<Target> GenerateTargetDataFromSource()
    {
        return JsonUtility.FromJson<TargetWrapper>(targetModelData.text).TargetList;
    }

    private TargetFacade CreateTargetFacade(Target target)
    {
        GameObject targetObject = Instantiate(targetObjectPrefab, targetObjectsParentTransforms[target.Floor], false);

        targetObject.name = target.Name;
        targetObject.transform.position = target.Position;
        targetObject.transform.rotation = Quaternion.Euler(target.Rotation);

        TargetFacade targetData = targetObject.GetComponent<TargetFacade>();

        targetData.Name = target.Name;
        targetData.Floor = target.Floor;
        targetData.Building = target.Building;
        targetData.Purpose = target.Purpose;

        targetObject.SetActive(false); // Start hidden
        targetPins[target.Position] = targetObject; // Store the pin reference
        
        return targetData;
    }

    // ✅ New method: Store QR targets as TargetFacade without instantiating prefabs
    private TargetFacade CreateTargetFacadeWithoutInstantiation(Target target)
    {
        // ✅ Create an empty GameObject for the QR target
        GameObject qrTargetObject = new GameObject(target.Name);

        // ✅ Add TargetFacade component (since we can't use "new")
        TargetFacade qrTarget = qrTargetObject.AddComponent<TargetFacade>();

        // ✅ Assign target data
        qrTarget.Name = target.Name;
        qrTarget.Floor = target.Floor;
        qrTarget.Building = target.Building;
        qrTarget.Purpose = target.Purpose;

        // ✅ Set position and rotation
        qrTarget.transform.position = target.Position;
        qrTarget.transform.rotation = Quaternion.Euler(target.Rotation);

        // ✅ Hide the QR target GameObject (so it doesn’t appear in the scene)
        qrTargetObject.SetActive(false);

        return qrTarget;
    }

    private void FillDropdownWithTargetItems()
    {
        List<TMP_Dropdown.OptionData> targetFacadeOptionData = currentTargetItems
            .Where(x => x.Purpose != "QR")
            .Select(x => new TMP_Dropdown.OptionData 
            { 
                text = $"{x.Building} - {x.Name}" 
            })
            .ToList();

        targetDataDropdown.ClearOptions();
        targetDataDropdown.AddOptions(targetFacadeOptionData);
    }

    public List<string> SearchPOIs(string searchBarText){
        if (string.IsNullOrWhiteSpace(searchBarText)){
            return new List<string>();
        }

        return currentTargetItems
        .Where(x => x.Name.ToLower().Contains(searchBarText.ToLower()))
        .Select(x => x.Name)
        .ToList();
    }

    public void NavigateToPOI(string targetName)
    {
        Vector3 targetPos = GetTargetPositionByName(targetName);
        navigationController.ActivateNavigation(targetPos);

    }

    private Vector3 GetTargetPositionByName(string targetName){
        TargetFacade target = currentTargetItems
        .Find(x => x.Name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase));

        return target != null ? target.transform.position : Vector3.zero;
    }

    public void SetSelectedTargetPositionWithDropdown (int selectedValue)
    {
        Vector3 targetPos = GetCurrentlySelectedTarget(selectedValue);
        navigationController.ActivateNavigation(targetPos);
    }

    private Vector3 GetCurrentlySelectedTarget(int selectedValue)
    {
        if (selectedValue >= currentTargetItems.Count)
        {
            return Vector3.zero;
        }

        return currentTargetItems[selectedValue].transform.position;
    }

    public TargetFacade GetCurrentTargetByTargetText(string targetText)
    {
        // ✅ First, check non-QR targets
        TargetFacade target = currentTargetItems.Find(x => x.Name.Equals(targetText));

        if (target == null)
        {
            // ✅ If not found, check QR targets
            target = qrTargetItems.Find(x => x.Name.Equals(targetText));
        }

        return target;
    }

        /// <summary>
    /// Finds a target based on its world position.
    /// </summary>
    public TargetFacade GetCurrentTargetByPosition(Vector3 position)
    {
        return currentTargetItems.FirstOrDefault(target => Vector3.Distance(target.transform.position, position) < 0.5f);
    }


    public void TogglePinVisibility(Vector3 targetPosition, bool isVisible)
    {   
        HideAllPins(); // ✅ Hide all previous pins before making a new one visible

        if (targetPins.ContainsKey(targetPosition))
        {
            targetPins[targetPosition].SetActive(isVisible);
        }
    }

    public void HideAllPins()
    {
        foreach (var pin in targetPins.Values)
        {
            pin.SetActive(false);
        }
    }

    public List<Vector3> GetNonQRTargetPositions()
    {
        return currentTargetItems.Select(x => x.transform.position).ToList();
    }

    public List<string> GetNonQRTargetNames()
    {
        return currentTargetItems.Select(x => x.Name).ToList();
    }

    public List<TargetFacade> GetTransitionPOIs()
    {
        return currentTargetItems
            .Where(x => x.Purpose == "POI" && 
                        (x.Name.ToLower().Contains("stairs") || 
                         x.Name.ToLower().Contains("entrance") || 
                         x.Name.Contains("120")))
            .ToList();
    }

    public Vector3 GetTourStartingPoint()
    {
        return tourStartingPoint;
    }

}
