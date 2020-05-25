using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallHandler : MonoBehaviour
{
    public Material TransparentMaterial;

    private bool drawWallsFlag = false;

    private Mesh mesh;
    
    public List<Vector3> wall_list;
    private string[] names;

    private bool drawFloorFlag = false;
    public Vector3 floorCenter;
    private float latitude = 0.0f;
    private float longitude = 0.0f;
    public float height = 0.0f;

    public Vector3 minVector;
    public Vector3 maxVector;

    GameObject cube;

    // Start is called before the first frame update
    void Start()
    {
        cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.GetComponent<Renderer>().material.color = Color.white;
    }

    // Update is called once per frame
    void Update()
    {
        if (drawWallsFlag) {
            drawWallsFlag = false;
            DrawWalls();
        }
        if (drawFloorFlag)
        {
            drawFloorFlag = false;
            SetFloor();
            DrawFloor();
            DrawBoundaries();
        }
    }

    public void SetFloor()
    {
        floorCenter = new Vector3((maxVector.y + minVector.y) / 2f, 0.0f, (maxVector.x + minVector.x) / 2f);
        longitude = Mathf.Abs(maxVector.y) + Mathf.Abs(minVector.y);
        latitude = Mathf.Abs(maxVector.x) + Mathf.Abs(minVector.x);
        height = Mathf.Abs(maxVector.z);      
    }

    private void DrawWalls()
    {
        int wallNumber = wall_list.Count / 4;
        for (int i = 0; i < wallNumber; i = i + 2)
        {

            Vector3 center = new Vector3((wall_list[i].y + wall_list[i + 1].y) / 2f, height / 2f, (wall_list[i].x + wall_list[i + 1].x) / 2f);
            float width = Vector3.Magnitude(wall_list[i] - wall_list[i + 1]);
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.localPosition = center;
            cube.transform.localScale = new Vector3(width, height, 0.001f);
            GameObject instance = Instantiate(cube, cube.transform.position, Quaternion.LookRotation(Vector3.forward));
        }  
    }

    private void DrawFloor()
    {
        cube.transform.localScale = new Vector3(longitude, 0.001f, latitude);
        cube.transform.localPosition = floorCenter;
        GameObject instance = Instantiate(cube, cube.transform.position, Quaternion.LookRotation(Vector3.forward));
    }

    private void DrawBoundaries()
    {
        cube.transform.localScale = new Vector3(longitude, height, 0.01f);
        cube.transform.localPosition = new Vector3((maxVector.y + minVector.y) / 2f, height / 2f, maxVector.x);
        Instantiate(cube, cube.transform.localPosition, Quaternion.LookRotation(Vector3.forward));
        Instantiate(cube, new Vector3((maxVector.y + minVector.y) / 2f, height / 2f, minVector.x), Quaternion.LookRotation(Vector3.forward));
        cube.transform.localScale = new Vector3(0.01f, height, latitude);
        cube.transform.localPosition = new Vector3(maxVector.y, height / 2f, (maxVector.x + minVector.x) / 2f);
        Instantiate(cube, cube.transform.localPosition, Quaternion.LookRotation(Vector3.forward));
        Instantiate(cube, new Vector3(minVector.y, height / 2f, (maxVector.x + minVector.x) / 2f), Quaternion.LookRotation(Vector3.forward));
    }

    public void Draw()
    {
        drawFloorFlag = true;
        drawWallsFlag = true;
    }
}
