using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class ArrowPathVisualization : MonoBehaviour {

    [SerializeField]
    private NavigationController navigationController;
    [SerializeField]
    private GameObject arrow;
    [SerializeField]
    private float moveOnDistance;

    private NavMeshPath path;
    private float currentDistance;
    private Vector3[] pathOffset;
    private Vector3 nextNavigationPoint = Vector3.zero;

    private void Update() {
        path = navigationController.path;

        AddOffsetToPath();
        SelectNextNavigationPoint();

        arrow.transform.LookAt(nextNavigationPoint);
    }

    private void AddOffsetToPath() {
        pathOffset = new Vector3[path.corners.Length];
        for (int i = 0; i < path.corners.Length; i++) {
            pathOffset[i] = new Vector3(path.corners[i].x, transform.position.y, path.corners[i].z);
        }
    }

    private void SelectNextNavigationPoint() {
        nextNavigationPoint = SelectNextNavigationPointWithinDistance();
    }

    private Vector3 SelectNextNavigationPointWithinDistance() {
        for (int i = 0; i < pathOffset.Length; i++) {
            currentDistance = Vector3.Distance(transform.position, pathOffset[i]);
            if (currentDistance > moveOnDistance) {
                return pathOffset[i];
            }
        }
        return navigationController.targetPosition;
    }
}