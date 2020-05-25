using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArucoHandler : MonoBehaviour
{
    public List<int> _numbers = new List<int>();
    public List<Vector3> _arucos = new List<Vector3>();


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePositions();
    }

    private void UpdatePositions()
    {

    }
}
