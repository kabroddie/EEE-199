using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinSpin : MonoBehaviour
{
    [SerializeField]
    private float rotationSpeedY = 50f; // ✅ Adjust speed in Inspector

    private void Update()
    {
        // ✅ Rotate only around the Y-axis
        transform.Rotate(0, rotationSpeedY * Time.deltaTime, 0, Space.World);
    }
}
