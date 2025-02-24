using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorSchedOpener : MonoBehaviour
{
    public GameObject[] floors;

    public void switchFloors(int floorID)
    {
      foreach(GameObject go in floors)
        {
            go.SetActive(false);
        }
        floors[floorID].SetActive(true);
    }

    public void close(int floorID)
    {
        floors[floorID].SetActive(false);
    }
    
}
