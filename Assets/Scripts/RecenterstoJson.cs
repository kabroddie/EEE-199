using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class RecenterData
{
    public string name;
    public Vector3 position;
    public Vector3 rotation;
}

[System.Serializable]
public class RecentersContainer
{
    public List<RecenterData> recenters = new List<RecenterData>();
}

public class RecenterstoJson : MonoBehaviour
{
    [SerializeField]
    private string outputFileName = "RecenterList.json";

    void Start()
    {
        SaveRecentersToJson();
    }

    public void SaveRecentersToJson()
    {
        RecentersContainer recentersContainer = new RecentersContainer();
        GameObject[] recenterObjects = GameObject.FindGameObjectsWithTag("Recenters"); // Find all walls by tag

        foreach (GameObject recenter in recenterObjects)
        {
            RecenterData recenterData = new RecenterData
            {
                name = recenter.name,
                position = recenter.transform.position,
                rotation = recenter.transform.eulerAngles,
            };

            recentersContainer.recenters.Add(recenterData);
        }

        string json = JsonUtility.ToJson(recentersContainer, true); // Pretty format JSON
        string path = Path.Combine(Application.persistentDataPath, outputFileName);

        File.WriteAllText(path, json);
        Debug.Log($"Recenters exported to JSON: {path}");
    }
}
