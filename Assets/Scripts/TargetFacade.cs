using System;
using UnityEngine;

[Serializable]
public class TargetFacade : MonoBehaviour
{
    public string Name;
    public string Category;
    public int Floor;
    public string Building;
    public string Purpose;

    public Vector3 Position;  // ✅ Store position manually
    public Quaternion Rotation;  // ✅ Store rotation manually
}
