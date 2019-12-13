using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(OVRCameraRig))]
public class CameraController : MonoBehaviour
{
    OVRCameraRig ovrCamera;

    // Start is called before the first frame update
    void Start()
    {
        ovrCamera.transform.position = new Vector3(0f, 2f, 0f);    //change from script
        ovrCamera.transform.rotation = new Quaternion();       //TODO
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
