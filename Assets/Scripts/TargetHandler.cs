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

    [SerializeField] private GameObject categoryPinPrefab; // assign in Inspector
    private List<GameObject> spawnedCategoryPins = new List<GameObject>();



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
                TargetFacade qrTarget = CreateTargetFacadeWithoutInstantiation(target);
                qrTargetItems.Add(qrTarget);
            }
            else
            {
                TargetFacade facade = CreateTargetFacade(target);
                currentTargetItems.Add(facade);

                Debug.Log($"[TargetHandler] Added POI: {facade.Name} at {facade.transform.position}");
            }
        }

        Debug.Log($"[TargetHandler] Total POIs Loaded: {currentTargetItems.Count}");
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
        targetData.Category = target.Category;

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
        qrTarget.Category = target.Category;

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

        if (targetPos == Vector3.zero)
        {
            Debug.LogWarning($"[TargetHandler] POI Not Found or Position is Zero: {targetName}");
            return; // ✅ Prevents activating navigation to (0,0,0)
        }

        Debug.Log($"[TargetHandler] Navigating to: {targetName} at Position: {targetPos}");
        navigationController.ActivateNavigation(targetPos);
    }

    public int GetIndexOfTarget(TargetFacade target)
    {
        return currentTargetItems.IndexOf(target);
    }

    private Vector3 GetTargetPositionByName(string targetName)
    {
        Debug.Log($"[TargetHandler] Looking for POI: {targetName} in {currentTargetItems.Count} POIs");

        TargetFacade target = currentTargetItems
            .FirstOrDefault(x => x.Name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase));

        if (target == null)
        {
            Debug.LogWarning($"[TargetHandler] POI Not Found: {targetName}");
            return Vector3.zero;
        }

        Debug.Log($"[TargetHandler] Found POI: {target.Name} at {target.transform.position}");
        return target.transform.position;
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
        // ✅ Check if the position exists in currentTargetItems
        TargetFacade target = currentTargetItems.Find(t => Vector3.Distance(t.transform.position, position) < 0.5f);

        if (target == null) // ✅ If not found, check QR targets
        {
            target = qrTargetItems.Find(t => Vector3.Distance(t.transform.position, position) < 0.5f);
        }

        return target; // ✅ Returns null if no match is found
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

    public List<TargetFacade> GetAllFirstFloorPresetPath()
    {
        string[] hardcodedOrder = new string[]
        {
            "UP Meralco Innovation Hall (Rm 120)", "Lecture Room (Rm 123)",
            "Power Electronics Laboratory (PEL) (Rm 124)", "Electronics Laboratory (ELab) (Rm 126)",
            "Electric Machines Instructional Laboratory (Rm 127)",
            "Fabrications Laboratory (Rm 128)",
            "Motors Drive Instructional Laboratory (Rm 129)",

            "PLDT Multimedia Lecture Hall (VLC)", 
            "ASTEC Multimedia Lecture Hall (LC2)", 
            "MERALCO Multimedia Lecture Hall (LC1)"   
        };

        return currentTargetItems
            .Where(x => x.Purpose == "POI" && System.Array.Exists(hardcodedOrder, poi => poi == x.Name))
            .OrderBy(x => System.Array.IndexOf(hardcodedOrder, x.Name))
            .ToList();
    }

    public List<TargetFacade> GetAllSecondFloorPresetPath()
    {
        string[] hardcodedOrder = new string[]
        {
            "Samsung Laboratory (Rm 204)",
            "Communications Electronics and Embedded Systems Laboratory (CEESL) (Rm 205)",
            "Computer Networks Laboratory (CNL) (Rm 207)",
            "Ubiquitous Computing Laboratory (UCL) (Rm 208)",

            "EEE Administrative Office", "UP OpenRAN Laboratory (Rm 227)",
            "PEL Instructional Laboratory (Rm 228)", "Small Classroom 6 (SCL) (Rm 229)"
        };

        return currentTargetItems
            .Where(x => x.Purpose == "POI" && System.Array.Exists(hardcodedOrder, poi => poi == x.Name))
            .OrderBy(x => System.Array.IndexOf(hardcodedOrder, x.Name))
            .ToList();
    }

    public List<TargetFacade> GetFirstAndSecondFloorPresetPath()
    {
        string[] hardcodedOrder = new string[]
        {
            "UP Meralco Innovation Hall (Rm 120)", "Lecture Room (Rm 123)",
            "Power Electronics Laboratory (PEL) (Rm 124)", "Electronics Laboratory (ELab) (Rm 126)",
            "Electric Machines Instructional Laboratory (Rm 127)",
            "Fabrications Laboratory (Rm 128)",
            "Motors Drive Instructional Laboratory (Rm 129)",

            "PLDT Multimedia Lecture Hall (VLC)", 
            "ASTEC Multimedia Lecture Hall (LC2)", 
            "MERALCO Multimedia Lecture Hall (LC1)",

            "Samsung Laboratory (Rm 204)",
            "Communications Electronics and Embedded Systems Laboratory (CEESL) (Rm 205)",
            "Computer Networks Laboratory (CNL) (Rm 207)",
            "Ubiquitous Computing Laboratory (UCL) (Rm 208)",

            "EEE Administrative Office", "UP OpenRAN Laboratory (Rm 227)",
            "PEL Instructional Laboratory (Rm 228)", "Small Classroom 6 (SCL) (Rm 229)"
        };

        return currentTargetItems
            .Where(x => x.Purpose == "POI" && System.Array.Exists(hardcodedOrder, poi => poi == x.Name))
            .OrderBy(x => System.Array.IndexOf(hardcodedOrder, x.Name))
            .ToList();
    }

    

    public List<TargetFacade> GetTransitionPOIs()
    {
        return currentTargetItems
            .Where(x => x.Purpose == "POI" && 
                        (x.Name.ToLower().Contains("stairs")))
            .ToList();
    }

    public Vector3 GetTourStartingPoint()
    {
        return tourStartingPoint;
    }

    public void ShowPins(string categoryName,int floorNumber)
    {
        // First hide all pins

        
        // Check if any targets have the specified category
        bool foundCategory = false;
        
        foreach (var target in currentTargetItems)
        {
            if (target.Category == categoryName && target.Floor == floorNumber)
            {
                foundCategory = true;
                Vector3 targetPosition = target.transform.position + Vector3.up * 0.5f;
                Debug.Log($"Found target: {target.Name} at position: {targetPosition}");
                GameObject catPin = Instantiate(categoryPinPrefab, targetPosition, Quaternion.identity);
                spawnedCategoryPins.Add(catPin);
            }
        }
        Debug.Log($"[TargetHandler] Spawned {spawnedCategoryPins.Count} pins for category: {categoryName}");
        if (!foundCategory)
        {
            Debug.LogWarning($"[TargetHandler] No targets found with category: {categoryName}");
        }
    }

    public void clearCatPins()
    {
        foreach (var pin in spawnedCategoryPins){
            Destroy(pin);
        }
        spawnedCategoryPins.Clear();
    }

    public List<TargetFacade> CategoryPOIs(string categoryName, int floorNumber){
        return currentTargetItems
            .Where(x => x.Category == categoryName && x.Floor == floorNumber)
            .ToList();
    }

    public List<TargetFacade> CategoryPOIsNoFloorNo(string categoryName){
        return currentTargetItems
            .Where(x => x.Category == categoryName)
            .ToList();
    }

}
