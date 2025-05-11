using System.Collections;
using UnityEngine;
using TMPro;

public class ArDebug : MonoBehaviour
{
    public Camera arCamera;                 // Assign AR camera in Inspector or defaults to Camera.main
    public TextMeshProUGUI debugText;       // Assign your TMP text here
    public float updateInterval = 3f;

    private KalmanCameraStabilizer kalman;  // Reference to Kalman filter script

    void Start()
    {
        if (arCamera == null)
            arCamera = Camera.main;

        kalman = arCamera.GetComponent<KalmanCameraStabilizer>();

        if (kalman == null)
        {
            Debug.LogWarning("KalmanCameraStabilizer not found on AR Camera.");
        }

        StartCoroutine(UpdateDebugInfo());
    }

    IEnumerator UpdateDebugInfo()
    {
        while (true)
        {
            Vector3 filteredPos = arCamera.transform.position;
            Vector3 filteredRot = arCamera.transform.eulerAngles;

            Vector3 rawPos = kalman != null ? kalman.GetRawPosition() : Vector3.zero;
            Vector3 rawRot = kalman != null ? kalman.GetRawRotation() : Vector3.zero;

            string output = $"POSITION (Filtered | Raw)\n" +
                            $"X: {filteredPos.x:F2} | {rawPos.x:F2}\n" +
                            $"Y: {filteredPos.y:F2} | {rawPos.y:F2}\n" +
                            $"Z: {filteredPos.z:F2} | {rawPos.z:F2}\n\n" +

                            $"ROTATION (Filtered | Raw)\n" +
                            $"X: {filteredRot.x:F2} | {rawRot.x:F2}\n" +
                            $"Y: {filteredRot.y:F2} | {rawRot.y:F2}\n" +
                            $"Z: {filteredRot.z:F2} | {rawRot.z:F2}\n";

            debugText.text = output;

            yield return new WaitForSeconds(updateInterval);
        }
    }
}
