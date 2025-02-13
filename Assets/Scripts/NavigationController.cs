using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using TMPro;

public class NavigationController : MonoBehaviour
{
    public Vector3 targetPosition { get; set; } = Vector3.zero;

    [SerializeField]
    private LineRenderer line;
    
    private NavMeshPath path;

    [SerializeField]
    private TextMeshProUGUI toggleButtonText;

    // Start is called before the first frame update
    private void Start()
    {
        path = new NavMeshPath();
        // disable screen dimming
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    // Update is called once per frame
    private void Update()
    {
        if (line.gameObject.activeSelf && targetPosition != Vector3.zero)
        {
            NavMesh.CalculatePath(transform.position, targetPosition, NavMesh.AllAreas, path);
            line.positionCount = path.corners.Length;
            line.SetPositions(path.corners);
        }
    }

    public void ToggleLineVisibility()
    {
        line.enabled = !line.enabled;
        UpdateToggleButtonText();
    }

    private void UpdateToggleButtonText() 
    {
        if (toggleButtonText != null)
        {
            toggleButtonText.text = line.enabled ? "Line: On" : "Line: Off";
        }
    }
}
