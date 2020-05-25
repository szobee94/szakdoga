using System;
using System.Collections.Generic;
using UnityEngine;
using HuaweiARInternal;
using HuaweiARUnitySDK;

[RequireComponent(typeof(PlacementHandler))]
public class PointCloudHandler : MonoBehaviour
{
    ARPointCloud arPointCloud;

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
        placementHandler = GetComponent<PlacementHandler>();
        InvokeRepeating("UpdatedCloudPoints", 0f, 1f);
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void UpdatedCloudPoints()
    {
        List<Vector4> pointList = new List<Vector4>();
        arPointCloud = ARFrame.AcquirePointCloud();
        arPointCloud.GetPoints(pointList);
        foreach (Vector4 point in pointList)
            if (point.w >= 0.9f)
            {
                float distance = Vector3.Distance(placementHandler.GetCameraPose().position, new Vector3(point.x, point.y, point.z));
                if (distance <= 3.0f)
                    points.Add(new Vector3(point.x, point.y, point.z));
            }
        if (points.Count >= 500)
            updateCloudFlag = true;
    }

    public void CleanUp()
    {
        points.Clear();
    }

    public void SetPointCloudVisualization(bool visualization)
    {
        FindObjectOfType<PointcloudVisualizer>().gameObject.SetActive(visualization);
    }

    public void PointCloudProcessChanged(bool isActive)
    {
        processFlag = isActive;
        SetPointCloudVisualization(isActive);
    }
}