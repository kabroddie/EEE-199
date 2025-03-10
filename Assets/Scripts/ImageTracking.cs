using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ImageTracking : MonoBehaviour
{
    [SerializeField]
    private GameObject[] placeablePrefabs;

    private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();
    private ARTrackedImageManager trackedImageManager;

    private void Awake()
    {
        // You could also use GetComponent<ARTrackedImageManager>() 
        // if it's on the same GameObject.
        trackedImageManager = FindObjectOfType<ARTrackedImageManager>();

        // Instantiate and store each prefab in a dictionary,
        // keyed by the prefab (and reference image) name.
        foreach (GameObject prefab in placeablePrefabs)
        {
            GameObject newPrefab = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            // Name it to match the prefab (which should match the image name).
            newPrefab.name = prefab.name;
            // Initially disable so it’s hidden until the image is tracked.
            newPrefab.SetActive(false);

            spawnedPrefabs.Add(prefab.name, newPrefab);
        }
    }

    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    private void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // For newly detected images
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            UpdateImageTracking(trackedImage);
        }

        // For updated images (e.g. still in view, or re-detected)
        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                UpdateImageTracking(trackedImage);
            }
            else
            {
                // If tracking is lost or limited, hide the associated prefab
                string imageName = trackedImage.referenceImage.name;
                if (spawnedPrefabs.ContainsKey(imageName))
                {
                    spawnedPrefabs[imageName].SetActive(false);
                }
            }
        }

        // For removed images (no longer detected)
        foreach (ARTrackedImage trackedImage in eventArgs.removed)
        {
            string imageName = trackedImage.referenceImage.name;
            if (spawnedPrefabs.ContainsKey(imageName))
            {
                spawnedPrefabs[imageName].SetActive(false);
            }
        }
    }

    private void UpdateImageTracking(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        // Ensure we have a corresponding prefab
        if (!spawnedPrefabs.ContainsKey(imageName))
            return;

        GameObject prefab = spawnedPrefabs[imageName];

        // Option A: Match the exact position/rotation of the tracked image,
        // without parenting:
        // prefab.transform.position = trackedImage.transform.position;
        // prefab.transform.rotation = trackedImage.transform.rotation;

        // Option B (Recommended): Parent the prefab under the image transform
        // so it follows the image if it moves/rotates in the camera view.
        prefab.transform.SetParent(trackedImage.transform, false);

        // Optional offset (local coordinates).
        // E.g., if you want the prefab 10 cm above the image:
        // prefab.transform.localPosition = new Vector3(0f, 0.1f, 0f);

        // If you want it to keep the image’s rotation, do nothing more.
        // If you want a custom orientation, you can set local rotation:
        // prefab.transform.localEulerAngles = new Vector3(-90f, 0f, 0f);

        // Finally, show the prefab
        prefab.SetActive(true);
    }
}


// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.XR;
// using UnityEngine.XR.ARFoundation;


// [RequireComponent(typeof(ARTrackedImageManager))]
// public class ImageTracking : MonoBehaviour
// {
    
//     [SerializeField]
//     private GameObject[] placeablePrefabs;

//     private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();
//     private ARTrackedImageManager trackedImageManager;

//     private void Awake()
//     {
//         trackedImageManager = FindObjectOfType<ARTrackedImageManager>();

//         foreach(GameObject prefab in placeablePrefabs)
//         {
//             GameObject newPrefab = Instantiate(prefab, Vector3.zero, Quaternion.identity);
//             newPrefab.name = prefab.name;
//             spawnedPrefabs.Add(prefab.name, newPrefab);
//         }
//     }

//     private void OnEnable()
//     {
//         trackedImageManager.trackedImagesChanged += ImageChanged;   
//     }

//     private void OnDisable()
//     {
//         trackedImageManager.trackedImagesChanged -= ImageChanged;   
//     }

//     private void ImageChanged(ARTrackedImagesChangedEventArgs eventArgs)
// {
//     foreach (ARTrackedImage trackedImage in eventArgs.added)
//     {
//         UpdateImage(trackedImage);
//     }

//     foreach (ARTrackedImage trackedImage in eventArgs.updated)
//     {
//         if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
//         {
//             UpdateImage(trackedImage);
//         }
//         else
//         {
//             spawnedPrefabs[trackedImage.referenceImage.name].SetActive(false);
//         }
//     }

//     foreach (ARTrackedImage trackedImage in eventArgs.removed)
//     {
//         if (spawnedPrefabs.ContainsKey(trackedImage.referenceImage.name))
//         {
//             spawnedPrefabs[trackedImage.referenceImage.name].SetActive(false);
//         }
//     }
// }

//     private void UpdateImage(ARTrackedImage trackedImage)
//     {
//         string name = trackedImage.referenceImage.name;
//         Vector3 position = trackedImage.transform.position;
//         Quaternion rotation = trackedImage.transform.rotation;

//         GameObject prefab = spawnedPrefabs[name];
//         prefab.transform.position = position;
//         prefab.SetActive(true);
//     }
// }
