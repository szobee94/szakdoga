using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ImageHandler : MonoBehaviour
{
    ARTrackedImageManager arManager;
    public Dictionary<int, Vector3> codes = new Dictionary<int, Vector3>();

    public bool isPosition = false;

    // Start is called before the first frame update
    void Start()
    {
        arManager = FindObjectOfType<ARTrackedImageManager>();
        arManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable()
    {
       arManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        arManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs change)
    {
        isPosition = true;
        foreach (ARTrackedImage trackedImage in change.added)
        {
            trackedImage.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            codes.Add(int.Parse(trackedImage.referenceImage.name), trackedImage.transform.position);      //forgatás?       
        }       

        foreach (ARTrackedImage trackedImage in change.updated)
        {
            codes[int.Parse(trackedImage.referenceImage.name)] = trackedImage.transform.position; //forgatás?
        }

        foreach(ARTrackedImage trackedImage in change.removed)
        {
            //kivülről törölhetŐ?
        }
    }
}
