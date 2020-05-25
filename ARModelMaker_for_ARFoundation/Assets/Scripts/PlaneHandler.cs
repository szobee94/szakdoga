using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class PlaneHandler : MonoBehaviour
{
    ARPlaneManager arPlaneManager;

    public List<ARPlane> arPlanes = new List<ARPlane>();

    // Start is called before the first frame update
    void Start()
    {
        arPlaneManager = FindObjectOfType<ARPlaneManager>();
        arPlaneManager.planesChanged += UpdatePlanes;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void UpdatePlanes(ARPlanesChangedEventArgs change)
    {
        foreach (var plane in arPlaneManager.trackables)
        {
            arPlanes.Add(plane);
        }
    }

    public void SetPlaneVisulization(bool visualization)
    {
        if (!visualization)
        {
            arPlaneManager.planesChanged -= UpdatePlanes;
            foreach (var plane in arPlaneManager.trackables)
                plane.gameObject.SetActive(false);
            arPlaneManager.detectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.None;
        }
        else
        {
            arPlaneManager.planesChanged += UpdatePlanes;
            arPlaneManager.detectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal;
            arPlaneManager.detectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Vertical;
        }
    }
}
