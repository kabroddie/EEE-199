using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;


[RequireComponent(typeof(ARTrackedImageManager))]
public class ImageTracking : MonoBehaviour
{
    
    [SerializeField]
    private GameObject[] placeablePrefabs;

    private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();
    private ARTrackedImageManager trackedImageManager;

    private void Awake()
    {
        trackedImageManager = FindObjectOfType<ARTrackedImageManager>();

        foreach(GameObject prefab in placeablePrefabs)
        {
            GameObject newPrefab = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            newPrefab.name = prefab.name;
            spawnedPrefabs.Add(prefab.name, newPrefab);
        }
    }

    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += ImageChanged;   
    }

    private void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= ImageChanged;   
    }

    private void ImageChanged(ARTrackedImagesChangedEventArgs eventArgs)
{
    foreach (ARTrackedImage trackedImage in eventArgs.added)
    {
        UpdateImage(trackedImage);
    }

    foreach (ARTrackedImage trackedImage in eventArgs.updated)
    {
        if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
        {
            UpdateImage(trackedImage);
        }
        else
        {
            spawnedPrefabs[trackedImage.referenceImage.name].SetActive(false);
        }
    }

    foreach (ARTrackedImage trackedImage in eventArgs.removed)
    {
        if (spawnedPrefabs.ContainsKey(trackedImage.referenceImage.name))
        {
            spawnedPrefabs[trackedImage.referenceImage.name].SetActive(false);
        }
    }
}

    private void UpdateImage(ARTrackedImage trackedImage)
    {
        string name = trackedImage.referenceImage.name;
        Vector3 position = trackedImage.transform.position;
        Quaternion rotation = trackedImage.transform.rotation;

        GameObject prefab = spawnedPrefabs[name];
        prefab.transform.position = position;
        prefab.SetActive(true);
    }
}
