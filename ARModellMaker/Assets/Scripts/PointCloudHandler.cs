using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.Collections;

[RequireComponent(typeof(PlacementHandler))]
public class PointCloudHandler : MonoBehaviour
{
    ARSessionOrigin arSessionOrigin;
    ARPointCloud arPointCloud;
    ARPointCloudManager arPointCloudManager;
    ARPointCloudParticleVisualizer arVisualizer;

    PlacementHandler placementHandler;

    public bool updateCloudFlag = false;

    private List<Vector3> points = new List<Vector3>();
    public List<Vector3> FeaturePoints
    {
        get { return new List<Vector3>(points); }
        private set { }
    }
    public int pointsCount = 0;

    public bool placementFinished = false;
    private bool processFlag = true;

    // Start is called before the first frame update
    void Start()
    {
        arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
        arPointCloudManager = FindObjectOfType<ARPointCloudManager>();
        arPointCloudManager.pointCloudsChanged += UpdatedCloudPoints;
        arVisualizer = GetComponent<ARPointCloudParticleVisualizer>();
        placementHandler = GetComponent<PlacementHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        if (arPointCloud == null)
            arPointCloud = arSessionOrigin.trackablesParent.GetComponentInChildren<ARPointCloud>();
        if (arVisualizer == null)
            arVisualizer = arSessionOrigin.trackablesParent.GetComponentInChildren<ARPointCloudParticleVisualizer>();
    }

    public void UpdatedCloudPoints(ARPointCloudChangedEventArgs change)
    {
        for (int i = 0; i < arPointCloud.identifiers.Value.Length; i++)
        {
            float distance = Vector3.Distance(placementHandler.GetCameraPose().position, arPointCloud.positions.Value[i]);
            if (arPointCloud.confidenceValues.Value[i] > 0.4f && distance < 4.0f)
            {
                float x = (float)Math.Round(arPointCloud.positions.Value[i].x, 2);
                float y = (float)Math.Round(arPointCloud.positions.Value[i].y, 2);
                float z = (float)Math.Round(arPointCloud.positions.Value[i].z, 2);
                points.Add(new Vector3(x, y, z));
            }
        }
        if (points.Count >= 300)
            updateCloudFlag = true;
    }

    public void CleanUp()
    {
        //pointsCount += points.Count;
        points.Clear();
    }

    public void SetPointCloudVisualization(bool visualization)
    {
        arVisualizer.gameObject.SetActive(visualization);
    }

    public void PointCloudProcessChanged(bool isActive)
    {
        processFlag = isActive;
        SetPointCloudVisualization(isActive);
    }
}
