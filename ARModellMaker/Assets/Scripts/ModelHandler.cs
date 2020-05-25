
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using HuaweiARInternal;
using HuaweiARUnitySDK;

[RequireComponent(typeof(PlacementHandler), typeof(PointCloudHandler))]
[RequireComponent(typeof(Communication), typeof(ImageHandler))]
public class ModelHandler : MonoBehaviour
{
    PlacementHandler placementHandler;
    PointCloudHandler pointCloudHandler;
    Communication communication;
    ImageHandler imageHandler;

    Plane plane = new Plane();

    /*UI elements*/
    public Button optionsButton;
    public Button placeButton;
    public GameObject morePanel;
    public Button wallButton;
    public Button floorButton;
    public Button settingsButton;
    public Button saveButton;
    public Button positionButton;
    public Text featurePointCount;
    public Text trackingStatus;
    private List<Vector3> points = new List<Vector3>();

    /*Settings*/
    public GameObject settingsPanel;
    public InputField ipInput;
    private string ipNumber;
    public InputField portInput;
    private int portNumber;
    public Button connectButton;
    public Toggle pointCloudVisualizationToggle;
    public Toggle planeVisualizationToggle;
    public Button backFromSettingsBUtton;

    /*Save */
    public GameObject savePanel;
    public InputField saveNameInputField;
    private string saveName;
    public Button confirmSaveButton;
    public Button backFromSaveBUtton;

    /* Wall panel */
    public GameObject wallPanel;
    public Button saveWallsButton;
    public Button newWallButton;
    public Button backFromWallPanel;
    public List<Vector3> walls = new List<Vector3>();
    private List<Vector3> realWalls = new List<Vector3>();

    /*Position panel */
    public GameObject positionPanel;
    public Text positionText;
    public Button backFromPositionButton;

    /* Notification panel */
    public GameObject notificationPanel;
    private Text notificationText;
    public Button notificationButton;

    public GameObject circlePanel;
    public Image loadingCircle;
    public RectTransform rotateImage;
    private float rotateSpeed = 200f;

    private bool placementFinished = false;
    private bool updatePCflag = false;

    // Start is called before the first frame update
    void Start()
    {
        circlePanel.SetActive(false);

        placementHandler = GetComponent<PlacementHandler>();
        pointCloudHandler = GetComponent<PointCloudHandler>();
        communication = GetComponent<Communication>();
        imageHandler = GetComponent<ImageHandler>();

        savePanel.SetActive(false);
        settingsPanel.SetActive(false);
        morePanel.SetActive(false);
        notificationPanel.SetActive(false);
        wallPanel.SetActive(false);
        positionPanel.SetActive(false);

        optionsButton.onClick.AddListener(OptionsPanelHandler);
        saveButton.onClick.AddListener(SetActiveSavePanel);
        placeButton.onClick.AddListener(placementHandler.PlaceObject_async);
        settingsButton.onClick.AddListener(SettingsClicked);
        pointCloudVisualizationToggle.onValueChanged.AddListener(delegate { SetPointCloudVisualization(pointCloudVisualizationToggle); });
        // planeVisualizationToggle.onValueChanged.AddListener(delegate { planeHandler.SetPlaneVisulization(planeVisualizationToggle.isOn); });
        floorButton.onClick.AddListener(StartFloorMeasure);
        wallButton.onClick.AddListener(StartWallMeasure);

        ipInput.onEndEdit.AddListener(SetIp);
        portInput.onEndEdit.AddListener(SetPort);
        connectButton.onClick.AddListener(CallConnection);

        saveNameInputField.onEndEdit.AddListener(SetSaveName);
        confirmSaveButton.onClick.AddListener(SaveMap);
        backFromSaveBUtton.onClick.AddListener(BackFromSave);
        backFromSettingsBUtton.onClick.AddListener(BackFromSettings);

        backFromWallPanel.onClick.AddListener(BackFromWallPanel);
        newWallButton.onClick.AddListener(StartWallMeasure);
        saveWallsButton.onClick.AddListener(SaveWalls);

        positionButton.onClick.AddListener(PositionHandler);
        backFromPositionButton.onClick.AddListener(BackFromPositionPanel);

        notificationButton.onClick.AddListener(NotificationButton);

        confirmSaveButton.interactable = false;

    }

    // Update is called once per frame
    void Update()
    {
        UpdateTracking();
        if (pointCloudHandler.updateCloudFlag)
        {
            UpdatePointCloud();
            pointCloudHandler.updateCloudFlag = false;
        }
        featurePointCount.text = "Points count: " + points.Count.ToString()  /*(pointCloudHandler.pointsCount + pointCloudHandler.FeaturePoints.Count).ToString()*/;
        if (loadingCircle.IsActive())
            rotateImage.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
        if (updatePCflag)
        {
            updatePCflag = false;
            SendPC();
        }
    }

    private void InvokeUpdatePC()
    {
        updatePCflag = true;
    }

    private void UpdateTracking()
    {
        if (ARFrame.GetTrackingState() == ARTrackable.TrackingState.TRACKING)
            trackingStatus.text = "";
        else
        {
            trackingStatus.text = "Tracking Lost!";
            trackingStatus.color = Color.red;
        }
    }

    private void UpdatePointCloud()
    {
        for (int i = 0; i < pointCloudHandler.FeaturePoints.Count; i++)
        {
            points.Add(pointCloudHandler.FeaturePoints[i]);
        }
        pointCloudHandler.CleanUp();
        //points.Sort();
    }

    private void SettingsClicked()
    {
        settingsPanel.SetActive(true);
    }

    /// <summary>
    /// Commented sections take part in ARFoundation post process
    /// </summary>
    private void SaveMap()
    {
        if (communication)
        {
            savePanel.SetActive(false);
            circlePanel.SetActive(true);
            /*Text text = circlePanel.GetComponentInChildren<Text>();

            text.text = "Updating PointCloud";
            UpdatePointCloud();
            text.text = "Delete duplicated points";
            points = DeleteDuplicatetdPoints();
            text.text = "Transform coordinates";
            points = placementHandler.Get3DCoordinates(points);
            */
            // text.text = "Save";
            SaveMessage();
            // text.text = "Sending arucos";
            SendArucos();
            // text.text = "Sending walls";
            SendWalls();
            // text.text = "Sending feature points";
            // SendFeaturePoint();

            circlePanel.SetActive(false);
        }
        else
        {
            Notification("Message won't be sent, due to lack of communication");
        }
    }

    public void PlacementFinished()
    {
        placementFinished = true;
        placeButton.interactable = false;
        saveButton.interactable = true;
        plane = placementHandler.plane;
        InvokeRepeating("InvokeUpdatePC", 0.0f, 0.5f);
    }

    private void SendFeaturePoint()
    {
        /*
        string msg = "feature_points";
        for (int i = 0; i < points.Count; i++)
        {
            msg += ',' + placementHandler.ConvertCoordinates(points[i]).x.ToString() +
                   ',' + placementHandler.ConvertCoordinates(points[i]).y.ToString() +
                   ',' + placementHandler.ConvertCoordinates(points[i]).z.ToString();
        }
        communication.Publish(msg);*/
      
        List<byte> byteList = new List<byte>();
        byte msgIdentifier = 1;
        byteList.Add(msgIdentifier);
        foreach (Vector3 element in points)
        {
            byteList.AddRange(BitConverter.GetBytes(element.x));
            byteList.AddRange(BitConverter.GetBytes(element.y));
            byteList.AddRange(BitConverter.GetBytes(element.z));
        }
        byte[] msg_bin = byteList.ToArray();
        communication.Publish(msg_bin);
        points.Clear();
    }

    private void SaveWalls()
    {
        foreach(Vector3 wall in walls)
            realWalls.Add(wall);
        walls.Clear();
    }

    private void SendWalls()
    {
        if (walls.Count > 0)
        {
            string msg = "walls";
            foreach (Vector3 wall in realWalls)
            {
                msg += ',' + placementHandler.ConvertCoordinates(wall).x.ToString() +
                       ',' + placementHandler.ConvertCoordinates(wall).y.ToString() +
                       ',' + placementHandler.ConvertCoordinates(wall).z.ToString();
            }
            communication.Publish(msg);
            wallPanel.SetActive(false);
        }
    }

    private void SaveMessage()
    {
        string msg = "save" + ',' + saveName.ToString();
        communication.Publish(msg);
    }

    private void SetPointCloudVisualization(Toggle toggle)
    {
        pointCloudHandler.SetPointCloudVisualization(toggle.isOn);
    }

    private void SetSaveName(string arg) {
        saveName = arg;
        if (arg.Length > 0)
            confirmSaveButton.interactable = true;
    }

    private void SetActiveSavePanel() {
        savePanel.SetActive(true);
    }

    private void BackFromSave()
    {
        savePanel.SetActive(false);
    }

    private void BackFromSettings()
    {
        settingsPanel.SetActive(false);
    }

    private void OptionsPanelHandler()
    {
        if (morePanel.activeInHierarchy)
        {
            morePanel.SetActive(false);
            optionsButton.GetComponentInChildren<Text>().text = "Options";
        }
        else
        {
            morePanel.SetActive(true);
            optionsButton.GetComponentInChildren<Text>().text = "Close Options";
        }
    }

    private void StartFloorMeasure()
    {
        OptionsPanelHandler();
        placeButton.interactable = true;
        placementHandler.FloorMeasure();
    }

    private void SetIp(string arg)
    {
        ipNumber = arg;
    }

    private void SetPort(string arg)
    {
        portNumber = int.Parse(arg);
        connectButton.interactable = true;
    }

    private void CallConnection()
    {
        if (communication.CallConnection(ipNumber, portNumber))
            connectButton.GetComponentInChildren<Text>().text = "Disconnect";
        else
            Notification("Can't connect to broker: \n" + ipNumber.ToString() + " at " + portNumber.ToString());
        settingsPanel.SetActive(false);
    }

    private void Notification(string arg)
    {
        notificationText.text = arg;
        notificationPanel.SetActive(true);
    }

    private void NotificationButton()
    {
        notificationPanel.SetActive(false);
    }

    private void StartWallMeasure()
    {
        OptionsPanelHandler();
        wallPanel.SetActive(true);
        placeButton.interactable = true;
        placementHandler.WallMeasure();
    }

    private void BackFromWallPanel()
    {
        placementHandler.Interrupt();
        placeButton.interactable = false;
        wallPanel.SetActive(false);
    }

    private void PositionHandler()
    {
        if (imageHandler.isPosition)
        {
            positionText.text = "ID\t\t\tx\t\t\ty\t\t\tz";

            foreach (KeyValuePair<int, Vector3> position in imageHandler.codes)
            {
                positionText.text += "\n" + position.Key.ToString()
                                   + "\t\t" + Math.Round(position.Value.x, 3).ToString()
                                   + "\t\t" + Math.Round(position.Value.y, 3).ToString()
                                   + "\t\t" + Math.Round(position.Value.z, 3).ToString();
            }
            positionPanel.SetActive(true);
        }
    }

    private void SendArucos()
    {
        if (imageHandler.codes.Count > 0)
        {
            string msg = "arucos";
            foreach (KeyValuePair<int, Vector3> position in imageHandler.codes)
            {
                msg += ',' + position.Key.ToString() + ',' + placementHandler.ConvertCoordinates(position.Value).x.ToString()
                                                     + ',' + placementHandler.ConvertCoordinates(position.Value).y.ToString()
                                                     + ',' + placementHandler.ConvertCoordinates(position.Value).z.ToString();
            }
            communication.Publish(msg);
        }
    }

    private void BackFromPositionPanel()
    {
        positionPanel.SetActive(false);
    }

    private Texture2D YuvToRbg(ARCameraImageBytes image, Rect r)
    {
        int sx = (int)r.x;
        int sy = (int)r.y;
        int width = (int)r.width;
        int height = (int)r.height;

        IntPtr ptrY = image.Y;
        IntPtr ptrU = image.U;
        IntPtr ptrV = image.V;

        int iwidth = image.Width;
        int iheight = image.Height;

        byte[] rgba = new byte[width * height * 4];

        if (image.UVPixelStride == 1)
        {
            byte[] bufferY = new byte[iwidth * iheight];
            byte[] bufferU = new byte[iwidth / 2 * iheight / 2];
            byte[] bufferV = new byte[iwidth / 2 * iheight / 2];

            System.Runtime.InteropServices.Marshal.Copy(ptrY, bufferY, 0, iwidth * iheight);
            System.Runtime.InteropServices.Marshal.Copy(ptrU, bufferU, 0, iwidth / 2 * iheight / 2);
            System.Runtime.InteropServices.Marshal.Copy(ptrV, bufferV, 0, iwidth / 2 * iheight / 2);

            for (int i = sy; i < sy + height; i++)
            {
                for (int j = sx; j < sx + width; j++)
                {
                    int yp = i * iwidth + j;
                    int uvp = (int)(i / 2) * (int)(iwidth / 2) + (int)(j / 2);
                    var y = bufferY[yp];
                    var u = bufferU[uvp];
                    var v = bufferV[uvp];

                    int p = (width - (j - sx) - 1) * height * 4 + (height - (i - sy) - 1) * 4;

                    float yf = (float)y - 16.0f;
                    float uf = (float)u - 128f;
                    float vf = (float)v - 128f;
                    rgba[p + 0] = (byte)clip((int)((298 * yf + 409 * vf + 128) / 256f)); // R
                    rgba[p + 1] = (byte)clip((int)((298 * yf - 100 * uf - 208 * vf + 128) / 256f)); // G
                    rgba[p + 2] = (byte)clip((int)((298 * yf + 516 * uf + 128) / 256f)); // B
                    rgba[p + 3] = 255; // A
                }
            }
        }

        else if (image.UVPixelStride == 2)
        {
            bool isNV21 = true;
            if ((ulong)ptrU < (ulong)ptrV)
            {
                isNV21 = false;
            }

            byte[] bufferY = new byte[iwidth * iheight];
            byte[] bufferUV = new byte[iwidth * iheight / 2];

            System.Runtime.InteropServices.Marshal.Copy(ptrY, bufferY, 0, iwidth * iheight);
            System.Runtime.InteropServices.Marshal.Copy(ptrU, bufferUV, 0, iwidth * iheight / 2);

            for (int i = sy; i < sy + height; i++)
            {
                for (int j = sx; j < sx + width; j++)
                {
                    int yp = i * iwidth + j;
                    var y = bufferY[yp];

                    int uvp = (int)(i / 2) * iwidth + (int)(j / 2) * 2;

                    var u = bufferUV[uvp + (isNV21 ? 0 : 1)];
                    var v = bufferUV[uvp + (isNV21 ? 1 : 0)];

                    int p = (width - (j - sx) - 1) * height * 4 + (height - (i - sy) - 1) * 4;

                    float yf = (float)y - 16.0f;
                    float uf = (float)u - 128f;
                    float vf = (float)v - 128f;

                    rgba[p + 0] = (byte)clip((int)((298 * yf + 409 * vf + 128) / 256f)); // R
                    rgba[p + 1] = (byte)clip((int)((298 * yf - 100 * uf - 208 * vf + 128) / 256f)); // G
                    rgba[p + 2] = (byte)clip((int)((298 * yf + 516 * uf + 128) / 256f)); // B
                    rgba[p + 3] = 255; // A
                }
            }
        }

        Texture2D tex = new Texture2D(height, width, TextureFormat.RGBA32, false, false);
        tex.LoadRawTextureData(rgba);
        tex.Apply();

        return tex;
    }

    private List<Vector3> DeleteDuplicatetdPoints()
    {
        List<Vector3> tempPoints = new List<Vector3>();
        foreach(Vector3 point in points)
        {
            if (!tempPoints.Contains(point))
                tempPoints.Add(point);
        }
        return tempPoints;
    }


    private void SendPC()
    {
        if (communication)
        {
            ARPointCloud pointCloud = ARFrame.AcquirePointCloud();
            List<Vector3> rawPoints = new List<Vector3>();
            List<Color32> colors = new List<Color32>();
            List<Vector3> aggregatedPoints = new List<Vector3>();

            ARCameraImageBytes cameraImage = ARFrame.AcquireCameraImageBytes();
            Texture2D screenTexture = YuvToRbg(cameraImage, new Rect(0, 0, cameraImage.Width, cameraImage.Height));
            screenTexture.Apply();

            // Check the the texture
            // System.IO.File.WriteAllBytes(Application.persistentDataPath + "/test.jpg", screenTexture.EncodeToJPG());
            pointCloud.GetPoints(rawPoints);
            for (int i = 0; i < rawPoints.Count; i++)
            {
                float distance = Vector3.Distance(placementHandler.GetCameraPose().position, rawPoints[i]);
                if (distance <= 3f && plane.GetDistanceToPoint(rawPoints[i]) > 1.0f)
                {
                    Vector3 screenPoint = placementHandler.FirstPersonCamera.WorldToScreenPoint(rawPoints[i]);
                    if (screenPoint.x < placementHandler.FirstPersonCamera.pixelWidth && screenPoint.y < placementHandler.FirstPersonCamera.pixelHeight && screenPoint.x >= 0 && screenPoint.y >= 0)
                    {
                        aggregatedPoints.Add(rawPoints[i]);
                        colors.Add(screenTexture.GetPixel((int)(screenPoint.x), (int)(screenPoint.y)));
                        colors.Add(CombineColors(screenPoint, screenTexture, 9));
                    }
                }
            }
            Destroy(screenTexture);
            aggregatedPoints = placementHandler.Get3DCoordinates(aggregatedPoints);

            List<byte> byteList = new List<byte>();
            byte msgIdentifier = 1;
            byteList.Add(msgIdentifier);
            for (int i = 0; i < aggregatedPoints.Count; i++)
            {
                byteList.AddRange(BitConverter.GetBytes(aggregatedPoints[i].x));
                byteList.AddRange(BitConverter.GetBytes(aggregatedPoints[i].y));
                byteList.AddRange(BitConverter.GetBytes(aggregatedPoints[i].z));
                byteList.Add(colors[i].r);
                byteList.Add(colors[i].g);
                byteList.Add(colors[i].b);
            }
            byte[] msg_bin = byteList.ToArray();
            communication.Publish(msg_bin);
            colors.Clear();
            rawPoints.Clear();
            aggregatedPoints.Clear();
        }
    }

    private static int clip(int v)
    {
        return v < 0 ? 0 : (v > 255 ? 255 : v);
    }

    private Color CombineColors(Vector3 screenPoint, Texture2D texture, int pixelBlockSize)
    {
        int nmb = (int)(Mathf.Sqrt(pixelBlockSize));
        Color result = new Color();
        for (int i = 0; i < nmb; i++)
        {
            for (int j = 0; j < nmb; j++)
            {
                result += texture.GetPixel((int)(screenPoint.x - ((nmb - 1) / 2f) + i), (int)(screenPoint.y - ((nmb - 1) / 2f) + j));
            }
        }

        result /= pixelBlockSize;

        return result;
    }
}
