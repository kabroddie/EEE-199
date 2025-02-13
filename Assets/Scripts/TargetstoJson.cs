using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class TargetData
{
    public string name;
    public Vector3 position;
    public Vector3 rotation;
}

[System.Serializable]
public class TargetContainer
{
    public List<TargetData> targets = new List<TargetData>();
}

public class TargetstoJson : MonoBehaviour
{
    [SerializeField]
    private string outputFileName = "TargetList.json";

    void Start()
    {
        SaveTargetsToJson();
    }

    public void SaveTargetsToJson()
    {
        TargetContainer targetsContainer = new TargetContainer();
        GameObject[] targetObjects = GameObject.FindGameObjectsWithTag("Targets"); // Find all walls by tag

        foreach (GameObject target in targetObjects)
        {
            TargetData targetData = new TargetData
            {
                name = target.name,
                position = target.transform.position,
                rotation = target.transform.eulerAngles,
            };

            targetsContainer.targets.Add(targetData);
        }

        string json = JsonUtility.ToJson(targetsContainer, true); // Pretty format JSON
        string path = Path.Combine(Application.persistentDataPath, outputFileName);

        File.WriteAllText(path, json);
        Debug.Log($"Targets exported to JSON: {path}");
    }
}
