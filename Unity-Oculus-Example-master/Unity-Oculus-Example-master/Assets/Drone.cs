using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drone : MonoBehaviour
{
    CharacterController characterController;
    public Transform drone;

    // Start is called before the first frame update
    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        drone.position = characterController.transform.position;
        drone.rotation = GetComponent<OVRPlayerController>().transform.rotation;
    }
}
