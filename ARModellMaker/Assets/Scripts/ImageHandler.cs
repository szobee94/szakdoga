using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HuaweiARInternal;
using HuaweiARUnitySDK;

public class ImageHandler : MonoBehaviour
{
    private AugmentedVisualizer visualizerPrefab;
    private List<ARAugmentedImage> arImages = new List<ARAugmentedImage>();
    private GameObject FitToScanOverlay;
    public ARAugmentedImageDatabase arImageDatabase;
    public Dictionary<int, Vector3> codes = new Dictionary<int, Vector3>();

    public bool isPosition = false;

    // Start is called before the first frame update
    void Start()
    {
        //arManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    // Update is called once per frame
    void Update()
    {
        ARFrame.GetTrackables<ARAugmentedImage>(arImages, ARTrackableQueryFilter.UPDATED);

        foreach (var image in arImages)
        {
            if (image.GetTrackingState() == ARTrackable.TrackingState.TRACKING)
            {
                ARAnchor anchor = image.CreateAnchor(image.GetCenterPose());
                Instantiate(visualizerPrefab, anchor.GetPose().position, anchor.GetPose().rotation);
            }
            else if (image.GetTrackingState() == ARTrackable.TrackingState.STOPPED)
            {
                GameObject.Destroy(visualizerPrefab.gameObject);
            }
        }
    }
    /*
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
    }*/
}
