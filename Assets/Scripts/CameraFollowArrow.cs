using UnityEngine;

public class CameraFollowArrow : MonoBehaviour
{
    public Transform arrowTransform;  // Reference to the arrow (e.g., IndicatorArrow)
    public Vector3 offset = new Vector3(0, 5, -10); // Slightly above and behind
    public float followSpeed = 5f;
    public float rotationSpeed = 5f;
    public float tiltAngle = 30f; // How much the camera tilts down to see the arrow

    void LateUpdate()
    {
        if (arrowTransform == null) return;

        // Calculate target position relative to the arrow's direction
        Vector3 desiredPosition = arrowTransform.position
                                + arrowTransform.up * offset.y     // Upward offset
                                + arrowTransform.forward * offset.z // Behind the arrow
                                + arrowTransform.right * offset.x;  // Lateral offset if needed

        // Smooth follow movement
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);

        // Make the camera look at the arrow's position
        Quaternion lookRotation = Quaternion.LookRotation(arrowTransform.position - transform.position);

        // Apply tilt manually by rotating around the local X axis
        Quaternion tilt = Quaternion.Euler(tiltAngle, lookRotation.eulerAngles.y, 0);

        // Smooth rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, tilt, Time.deltaTime * rotationSpeed);
    }
}
