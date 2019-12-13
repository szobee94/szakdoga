using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PointCloudHandler : MonoBehaviour
{
    public Material TransparentMaterial;

    private Mesh rawMesh;
    public Vector3 rawScale = new Vector3(0.01f, 0.01f, 0.01f);
    public Vector3[] rawPoints;

    private bool drawFlag = false;

    private static readonly int[] cubeTriangles =
   {
        0, 1, 3,
        1, 2, 3,
        6, 5, 7,
        5, 4, 7,
        3, 2, 7,
        2, 6, 7,
        5, 1, 4,
        1, 0, 4,
        0, 3, 4,
        3, 7, 4,
        6, 2, 5,
        2, 1, 5
    };

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (drawFlag)
        {
            DrawPointCloud();
            GetComponent<MeshRenderer>().material = TransparentMaterial;
            GetComponent<MeshFilter>().mesh = rawMesh;
            drawFlag = false;
        }
    }

    private void DrawPointCloud()
    {
        rawMesh = new Mesh()
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        Vector3[] rawVertices = new Vector3[rawPoints.Length * 8];
        Color[] rawColors = new Color[rawPoints.Length * 8];
        int[] rawTriangles = new int[rawPoints.Length * 36];

        for (int i = 0; i < rawPoints.Length; i++)
        {
            Vector3[] corners = GetCorners(rawPoints[i], rawScale);
            for (int j = 0; j < 8; j++)
            {
                rawVertices[(i * 8) + j] = corners[j];
                rawColors[(i * 8) + j] = new Color(1, 1, 1, 0.5f);
            }

            for (int j = 0; j < 36; j++)
                rawTriangles[(i * 36) + j] = (i * 8) + cubeTriangles[j];
        }

        rawMesh.vertices = rawVertices;
        rawMesh.colors = rawColors;
        rawMesh.triangles = rawTriangles;

        rawMesh.RecalculateBounds();
    }

    public void SetPointCloud()
    {
        drawFlag = true;
    }

    private static Vector3[] GetCorners(Vector3 input, Vector3 scale)
    {
        Vector3[] result = new Vector3[8];

        float shift_x = scale.x / 2;
        float shift_y = scale.y / 2;
        float shift_z = scale.z / 2;

        result[0] = new Vector3(input.y - shift_y, input.z + shift_z, input.x - shift_x);
        result[1] = new Vector3(input.y + shift_y, input.z + shift_z, input.x - shift_x);
        result[2] = new Vector3(input.y + shift_y, input.z + shift_z, input.x + shift_x);
        result[3] = new Vector3(input.y - shift_y, input.z + shift_z, input.x + shift_x);
        result[4] = new Vector3(input.y - shift_y, input.z - shift_z, input.x - shift_x);
        result[5] = new Vector3(input.y + shift_y, input.z - shift_z, input.x - shift_x);
        result[6] = new Vector3(input.y + shift_y, input.z - shift_z, input.x + shift_x);
        result[7] = new Vector3(input.y - shift_y, input.z - shift_z, input.x + shift_x);

        return result;
    }

    public void GetFloor(out Vector3 minVector, out Vector3 maxVector)
    {
        float percentil = 0.01f;
        List<float> orderedList_x = new List<float>();
        List<float> orderedList_y = new List<float>();
        List<float> orderedList_z = new List<float>();

        for (int i = 0; i < rawPoints.Length; i++)
            orderedList_x.Add(rawPoints[i].x);
        for (int i = 0; i < rawPoints.Length; i++)
            orderedList_y.Add(rawPoints[i].y);
        for (int i = 0; i < rawPoints.Length; i++)
            orderedList_z.Add(rawPoints[i].z);

        orderedList_x.Sort();
        orderedList_y.Sort();
        orderedList_z.Sort();

        minVector.x = orderedList_x[(int)(percentil * (float)orderedList_x.Count)];
        maxVector.x = orderedList_x[(int)((1f - percentil) * (float)orderedList_x.Count)];
        minVector.y = orderedList_y[(int)(percentil * (float)orderedList_y.Count)];
        maxVector.y = orderedList_y[(int)((1f - percentil) * (float)orderedList_y.Count)];
        minVector.z = orderedList_z[(int)(percentil * (float)orderedList_z.Count)];
        maxVector.z = orderedList_z[(int)((1f - percentil) * (float)orderedList_z.Count)];
    }
}
