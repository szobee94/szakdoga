using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HuaweiARInternal;
using HuaweiARUnitySDK;

public class AugmentedVisualizer : MonoBehaviour
{
    /// <summary>
    /// The AugmentedImage to visualize.
    /// </summary>
    public ARAugmentedImage Image;

    /// <summary>
    /// A model for the lower left corner of the frame to place when an image is detected.
    /// </summary>
    public GameObject FrameLowerLeft;

    /// <summary>
    /// A model for the lower right corner of the frame to place when an image is detected.
    /// </summary>
    public GameObject FrameLowerRight;

    /// <summary>
    /// A model for the upper left corner of the frame to place when an image is detected.
    /// </summary>
    public GameObject FrameUpperLeft;

    /// <summary>
    /// A model for the upper right corner of the frame to place when an image is detected.
    /// </summary>
    public GameObject FrameUpperRight;

    /// <summary>
    /// The Unity Update method.
    /// </summary>
    public void Update()
    {
        if (Image == null || Image.GetTrackingState() != ARTrackable.TrackingState.TRACKING)
        {
            FrameLowerLeft.SetActive(false);
            FrameLowerRight.SetActive(false);
            FrameUpperLeft.SetActive(false);
            FrameUpperRight.SetActive(false);
            return;
        }

        float halfWidth = Image.GetExtentX() / 2;
        float halfHeight = Image.GetExtentZ() / 2;
        FrameLowerLeft.transform.localPosition =
            (halfWidth * Vector3.left) + (halfHeight * Vector3.back);
        FrameLowerRight.transform.localPosition =
            (halfWidth * Vector3.right) + (halfHeight * Vector3.back);
        FrameUpperLeft.transform.localPosition =
            (halfWidth * Vector3.left) + (halfHeight * Vector3.forward);
        FrameUpperRight.transform.localPosition =
            (halfWidth * Vector3.right) + (halfHeight * Vector3.forward);

        FrameLowerLeft.SetActive(true);
        FrameLowerRight.SetActive(true);
        FrameUpperLeft.SetActive(true);
        FrameUpperRight.SetActive(true);
    }
}
