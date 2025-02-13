using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class WallData
{
    public string name;
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
}

[System.Serializable]
public class WallsContainer
{
    public List<WallData> walls = new List<WallData>();
}

public class WallstoJson : MonoBehaviour
{
    [SerializeField]
    private string outputFileName = "walls.json";

    void Start()
    {
        SaveWallsToJson();
    }

    public void SaveWallsToJson()
    {
        WallsContainer wallsContainer = new WallsContainer();
        GameObject[] wallObjects = GameObject.FindGameObjectsWithTag("Wall"); // Find all walls by tag

        foreach (GameObject wall in wallObjects)
        {
            WallData wallData = new WallData
            {
                name = wall.name,
                position = wall.transform.position,
                rotation = wall.transform.eulerAngles,
                scale = wall.transform.localScale
            };

            wallsContainer.walls.Add(wallData);
        }

        string json = JsonUtility.ToJson(wallsContainer, true); // Pretty format JSON
        string path = Path.Combine(Application.persistentDataPath, outputFileName);

        File.WriteAllText(path, json);
        Debug.Log($"Walls exported to JSON: {path}");
    }
}
