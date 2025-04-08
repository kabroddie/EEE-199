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

            string rawInfo = $"FILTERED:\n" +
                             $"Pos: ({filteredPos.x:F2}, {filteredPos.y:F2}, {filteredPos.z:F2})\n" +
                             $"Rot: ({filteredRot.x:F2}, {filteredRot.y:F2}, {filteredRot.z:F2})\n\n";

            string filteredInfo = $"RAW:\n{kalman.GetDebugInfo()}";

            debugText.text = rawInfo + filteredInfo;

            yield return new WaitForSeconds(updateInterval);
        }
    }
}
