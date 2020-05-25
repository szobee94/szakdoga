using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Pcx;
using UnityEngine.Networking;
using System.IO;

[RequireComponent(typeof(WallHandler), typeof(ArucoHandler), typeof(PointCloudHandler))]
public class ObjectInitializer : MonoBehaviour
{
    private WallHandler wallHandler;
    private ArucoHandler arucoHandler;
    private PointCloudHandler pointCloudHandler;

    private OVRCameraRig playerCamera;
    private OVRManager ovrManager;

    /* Communication parameters */
    private string brokerHostname;
    private int brokerPort;
    private string uplinkTopic = "command";
    private string droneTopic = "control";
    private string downlinkTopic = "visualize";
    private bool communication = false;
    private MqttClient client;

    /* Connection panel */
    public GameObject connectionPanel;
    public InputField ipInput;
    public InputField portInput;
    public Button connectButton;
    public Text errorText;

    /* Selection panel */
    public GameObject selectionPanel;
    public Dropdown mapDropdown;
    private List<string> savedMaps = new List<string>();
    public Button viewButton;
    public InputField resolutionInputField;
    private int resolution_cm;

    /* Load from *.csv*/
    private Vector3[] arucos;
    private int[] arucoNumber;
    private Vector3[] walls;

    public Transform playerController;

    public Light directionalLight;

    private bool positionFlag = false;
    private int arucoIndex;
    private Vector3 dronePosition = new Vector3();
    private Quaternion droneQuaternion = new Quaternion();

    private float defaultYaw = Mathf.NegativeInfinity;
    private float currentYaw = 0.0f;

    public Camera externalCamera;

    private Quaternion lastQuaternion = new Quaternion(0f, 0f, 0f, 0f);

    private float speed = 0.1f;
    private bool forwardFlag = false;
    private bool landFlag = false;
    private bool takeoffFlag = false;

    private bool connectedToDrone = false;

    public GameObject coloredPointCloud;
    private Color32[] colors;

    public RawImage realCameraRaw;
    string url = "http://172.18.0.100:9091/snapshot?topic=/iris/usb_cam/image_raw";     // snapshot
    // string url = "http://172.18.0.100:9091/stream?topic=/iris/usb_cam/image_raw";    // stream
    private Texture2D webTexture;
    public string dronePoseTextFile = "";
    private Pose dronePose;
    private Vector3 drone_position;
    private Quaternion drone_rotation;

    private Vector3 basePosition;
    private Quaternion baseRotation;

    // Start is called before the first frame update
    void Start()
    {
        wallHandler = GetComponent<WallHandler>();
        arucoHandler = GetComponent<ArucoHandler>();
        pointCloudHandler = GetComponent<PointCloudHandler>();

        coloredPointCloud.SetActive(false);
        selectionPanel.SetActive(false);
        ipInput.text = "10.42.0.1";
        portInput.text = "1883";

        ipInput.onEndEdit.AddListener(SetIp);
        portInput.onEndEdit.AddListener(SetPort);
        resolutionInputField.onEndEdit.AddListener(SetResolution);
        connectButton.onClick.AddListener(TryConnect);
        mapDropdown.onValueChanged.AddListener(delegate { LoadMap(mapDropdown); });
        viewButton.onClick.AddListener(ViewMap);
        
        // InvokeRepeating("UpdateDronePose", 0.0f, 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        if (connectedToDrone)
        {
            if (Input.GetKey(KeyCode.W) || forwardFlag || OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
                Publish("up");
            else if (Input.GetKey(KeyCode.S) || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
                Publish("down");
            else if (Input.GetKey(KeyCode.D) || OVRInput.GetDown(OVRInput.Button.SecondaryThumbstickRight) || OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickRight))
                Publish("right");
            else if (Input.GetKey(KeyCode.A) || OVRInput.GetDown(OVRInput.Button.SecondaryThumbstickLeft) || OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickLeft))
                Publish("left");
            else if (Input.GetKey(KeyCode.T) || landFlag || OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three))
                Publish("arm");
            else if (Input.GetKey(KeyCode.Z) || takeoffFlag || OVRInput.GetDown(OVRInput.Button.Two) || OVRInput.GetDown(OVRInput.Button.Four))
                Publish("disarm");
            else if (Input.GetKey(KeyCode.RightArrow) || OVRInput.GetDown(OVRInput.Button.SecondaryThumbstickRight) || OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickRight))
                Publish("go_right");
            else if (Input.GetKey(KeyCode.LeftArrow) || OVRInput.GetDown(OVRInput.Button.SecondaryThumbstickLeft) || OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickLeft))
                Publish("go_left");
            else if (Input.GetKey(KeyCode.UpArrow) || landFlag || OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three))
                Publish("forward");
            else if (Input.GetKey(KeyCode.DownArrow) || takeoffFlag || OVRInput.GetDown(OVRInput.Button.Two) || OVRInput.GetDown(OVRInput.Button.Four))
                Publish("backward");
            else
                Publish("keep_position");

            realCameraRaw.texture = webTexture;
            /*
if (Input.GetKey(KeyCode.UpArrow) || forwardFlag || OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
    Publish("up");
else if (Input.GetKey(KeyCode.DownArrow) || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
    Publish("down");
else if (Input.GetKey(KeyCode.RightArrow) || OVRInput.GetDown(OVRInput.Button.SecondaryThumbstickRight) || OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickRight))
    Publish("right");
else if (Input.GetKey(KeyCode.LeftArrow) || OVRInput.GetDown(OVRInput.Button.SecondaryThumbstickLeft) || OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickLeft))
    Publish("left");
else if (Input.GetKey(KeyCode.T) || landFlag || OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three))
    Publish("arm");
else if (Input.GetKey(KeyCode.Z) || takeoffFlag || OVRInput.GetDown(OVRInput.Button.Two) || OVRInput.GetDown(OVRInput.Button.Four))
    Publish("disarm");
else if (Input.GetKey(KeyCode.S) || OVRInput.GetDown(OVRInput.Button.SecondaryThumbstickRight) || OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickRight))
    Publish("go_right");
else if (Input.GetKey(KeyCode.W) || OVRInput.GetDown(OVRInput.Button.SecondaryThumbstickLeft) || OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickLeft))
    Publish("go_left");
else if (Input.GetKey(KeyCode.D) || landFlag || OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three))
    Publish("forward");
else if (Input.GetKey(KeyCode.A) || takeoffFlag || OVRInput.GetDown(OVRInput.Button.Two) || OVRInput.GetDown(OVRInput.Button.Four))
    Publish("backward");
else
    Publish("keep_position");*/
            playerController.transform.position = drone_position;
            playerController.transform.rotation = drone_rotation;
        }
    }

    private void SetIp(string arg)
    {
        brokerHostname = arg;
    }

    private void SetPort(string arg)
    {
        brokerPort = int.Parse(arg);
        connectButton.interactable = true;
    }

    private void SetResolution(string arg)
    {
        resolution_cm = int.Parse(arg);
    }

    private void TryConnect()
    {
        if (Connect(brokerHostname, brokerPort))
        {
            Debug.Log("Trying to connect " + brokerHostname.ToString() + " at port " + brokerPort.ToString() + " topic: " + uplinkTopic.ToString());
            GetSaveList();
            connectionPanel.SetActive(false);
            selectionPanel.SetActive(true);
        }
        else
            errorText.text = "Could not connect to: " + brokerHostname.ToString() + " at port " + brokerPort.ToString();
    }

    private bool Connect(string ip, int port)
    {
        try
        {
            client = new MqttClient(brokerHostname, brokerPort, false, null, null, MqttSslProtocols.None);
            string clientId = "UnityClient_" + DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            if (!CheckTcpConnection(brokerHostname, brokerPort))
                return false;
            byte ack = client.Connect(clientId);
            byte[] qosLevels = { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE };
            client.Subscribe(new string[] {downlinkTopic}, qosLevels);
            client.Subscribe(new string[] {"pose"}, qosLevels);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
        return true;
    }

    private bool CheckTcpConnection(string brokerHostname, int brokerPort)
    {
        System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient();
        var result = tcpClient.BeginConnect(brokerHostname, brokerPort, null, null);
        return result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
    }

    private bool ConnectToDrone()
    {
        try
        {
            string clientId = "UnityClient_" + DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            byte[] qosLevels = { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE };
            client.Subscribe(new string[] { }, qosLevels);
            Debug.Log("Connected to: " + droneTopic.ToString());
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
        return true;
    }

    private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        string msg = System.Text.Encoding.UTF8.GetString(e.Message);
        string[] splitted = msg.Split(',');


        if (splitted[0].Equals("saved_maps_list"))
        {
            savedMaps.Clear();
            int current_selected = mapDropdown.value;
            mapDropdown.options.Clear();
            //mapDropdown.ClearOptions();
            savedMaps.Add("No save selected");
            for (int i = 1; i < splitted.Length; i++)
                savedMaps.Add(splitted[i]);
            if (savedMaps.Count != 1)
                mapDropdown.options.AddRange(GetDisplayNameList(savedMaps));
            /*mapDropdown.options.Count > current_selected)
                mapDropdown.SetValueWithoutNotify(current_selected);
            else
                mapDropdown.SetValueWithoutNotify(0);*/
        }
        else if (splitted[0].Equals("walls"))
        {
            Vector3[] output = new Vector3[(splitted.Length - 1) / 3];
            ParseMessage(splitted, out output);
            wallHandler.wall_list.AddRange(output);
        }
        else if (splitted[0].Equals("feature_points"))
        {
            ParseColoredMessage(splitted, out pointCloudHandler.rawPoints, out colors);
            Debug.Log("Number of feature points: " + pointCloudHandler.rawPoints.Length.ToString());
        }
        else if (splitted[0].Equals("arucos"))
        {
            ParseArucoMessage(splitted, out Vector3[] output, out int[] numbers);
            arucoHandler._arucos.AddRange(output);
            arucoHandler._numbers.AddRange(numbers);
        }
        else if (splitted[0].Equals("position"))
        {
            ParsePositionMessage(splitted, out arucoIndex, out dronePosition, out droneQuaternion);
            positionFlag = true;
            if (defaultYaw == Mathf.NegativeInfinity)
                defaultYaw = droneQuaternion.y; //not sure
            takeoffFlag = false;
        }
        else if (splitted[0].Equals("actual_pose"))
        {
            ParsePoseMessage(splitted, out drone_position, out drone_rotation);
        }
    }

    private void ParseMessage(string[] msg, out Vector3[] output)
    {
        int length = (int)((msg.Length - 1) / 3);
        output = new Vector3[length];
        for (int i = 0; i < length; i++)
        {
            output[i] = new Vector3(float.Parse(msg[(i * 3) + 1], CultureInfo.InvariantCulture), float.Parse(msg[(i * 3) + 2], CultureInfo.InvariantCulture), float.Parse(msg[(i * 3) + 3], CultureInfo.InvariantCulture));
        }
    }

    private void ParseColoredMessage(string[] msg, out Vector3[] output, out Color32[] color32)
    {
        float transparentLevel = 0.7f;
        Debug.Log("Points number:" + ((msg.Length - 1) / 6).ToString());
        int length = (int)((msg.Length - 1) / 6);
        output = new Vector3[length];
        Color[] colors = new Color[length];
        color32 = new Color32[length];
        for (int i = 0; i < length; i++)
        {
            output[i] = new Vector3(float.Parse(msg[(i * 6) + 1], CultureInfo.InvariantCulture), float.Parse(msg[(i * 6) + 2], CultureInfo.InvariantCulture), float.Parse(msg[(i * 6) + 3], CultureInfo.InvariantCulture));
            colors[i] = new Color(float.Parse(msg[(i * 6) + 4], CultureInfo.InvariantCulture) / 255f, float.Parse(msg[(i * 6) + 5], CultureInfo.InvariantCulture) / 255f, float.Parse(msg[(i * 6) + 6], CultureInfo.InvariantCulture) / 255f, transparentLevel);
            color32[i] = (Color32)colors[i];
            // Debug.Log("Color32: " + color32[i].ToString());
        }
    }

    private void ParseArucoMessage(string[] msg, out Vector3[] output, out int[] numbers)
    {
        int length = (msg.Length - 1) / 4;
        numbers = new int[length];
        output = new Vector3[length];
        for (int i = 0; i < length; i++)
        {
            numbers[i] = int.Parse(msg[i * 4 + 1]);
            output[i] = new Vector3(float.Parse(msg[i * 4 + 2], CultureInfo.InvariantCulture), float.Parse(msg[i * 4 + 3], CultureInfo.InvariantCulture), float.Parse(msg[i * 4 + 4], CultureInfo.InvariantCulture));
        }
    }

    private void ParsePositionMessage(string[] msg, out int index, out Vector3 output, out Quaternion quaternion)
    {
        index = int.Parse(msg[1]);
        output = new Vector3(float.Parse(msg[2], CultureInfo.InvariantCulture), float.Parse(msg[3], CultureInfo.InvariantCulture), float.Parse(msg[4], CultureInfo.InvariantCulture));
        quaternion = new Quaternion(float.Parse(msg[5], CultureInfo.InvariantCulture), float.Parse(msg[6], CultureInfo.InvariantCulture), float.Parse(msg[7], CultureInfo.InvariantCulture), float.Parse(msg[8], CultureInfo.InvariantCulture));
        int i = 0; //nem kvaternió hanem rotáció
        while (arucoHandler._numbers[i] != index)
            i++;
        output = output + arucoHandler._arucos[i];
    }

    private void ParsePoseMessage(string[] msg, out Vector3 position, out Quaternion rotation)
    {
        position = new Vector3(-1f * float.Parse(msg[2], CultureInfo.InvariantCulture), float.Parse(msg[3], CultureInfo.InvariantCulture), float.Parse(msg[1], CultureInfo.InvariantCulture));
        rotation = new Quaternion(float.Parse(msg[5], CultureInfo.InvariantCulture), -1f * float.Parse(msg[6], CultureInfo.InvariantCulture), float.Parse(msg[4], CultureInfo.InvariantCulture), float.Parse(msg[7], CultureInfo.InvariantCulture));
        if(basePosition == null && baseRotation == null)
        {
            basePosition = position;
            baseRotation = rotation;
        }
        position = position - basePosition;
        rotation = new Quaternion(rotation.x - baseRotation.x, rotation.y - baseRotation.y, rotation.z - baseRotation.z, rotation.w - baseRotation.w);

    }


    private static List<Dropdown.OptionData> GetDisplayNameList(List<string> current_names)
    {
        List<Dropdown.OptionData> display_save_list = new List<Dropdown.OptionData>();
        foreach (string name in current_names)
        {
            string[] splitted_name = name.Split('_');
            if (splitted_name.Length > 1)
                display_save_list.Add(new Dropdown.OptionData(splitted_name[1]));
            else
                display_save_list.Add(new Dropdown.OptionData(name));
        }
        return display_save_list;
    }

    private void Publish(string msg)
    {
        if(!connectedToDrone)
            client.Publish(uplinkTopic, System.Text.Encoding.UTF8.GetBytes(msg), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        else
            client.Publish(droneTopic, System.Text.Encoding.UTF8.GetBytes(msg), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);

    }

    private void GetSaveList()
    {
        string msg = "get_save_list";
        Publish(msg);
    }

    private void GetMap(string mapName)
    {
        string msg = "map" + ',' + mapName.ToString();
        Publish(msg);
    }

    private void LoadMap(Dropdown dropdown)
    {
        GetMap(savedMaps[dropdown.value]);
        viewButton.interactable = true;
    }

    private void ViewMap()
    {
        if (resolution_cm != 1)
        {
            pointCloudHandler.rawScale = new Vector3(resolution_cm / 100f, resolution_cm / 100f, resolution_cm / 100f);
            if (colors == null)
                UpdateResolution();
        }

        if (colors == null)
            pointCloudHandler.SetPointCloud();
        else
        {
            Debug.Log("Initialize pcx...");
            List<Vector3> pointsList = new List<Vector3>();
            foreach (Vector3 point in pointCloudHandler.rawPoints)
                pointsList.Add(new Vector3(point.y, point.z, point.x));
            List<Color32> colorList = new List<Color32>();
            colorList.AddRange(colors);
            PointCloudData pData = new PointCloudData();
            pData.Initialize(pointsList, colorList);
            coloredPointCloud.GetComponent<PointCloudRenderer>().sourceData = pData;
            coloredPointCloud.SetActive(true);
            Debug.Log("Pcx is ready");
        }
        pointCloudHandler.GetFloor(out wallHandler.minVector, out wallHandler.maxVector);
        wallHandler.Draw();
        directionalLight.transform.localPosition = new Vector3((wallHandler.maxVector.y + wallHandler.minVector.y) / 2f, 10.0f, (wallHandler.maxVector.x + wallHandler.minVector.x) / 2f);
        externalCamera.GetComponent<MiniMap>().cameraPosition = new Vector3(wallHandler.minVector.y + 0.4f, wallHandler.maxVector.z + 1f, wallHandler.minVector.x + 0.4f);
        selectionPanel.SetActive(false);
        playerController.transform.position = new Vector3(0.1f, 0.26f, 0.1f);
        // takeoffFlag = true;
        StartCoroutine(DownloadImage());
        //InvokeRepeating("DownloadImage", 0.0f, 3f);

        connectedToDrone = true;
    }

    private void UpdateResolution()
    {
        float resolution = resolution_cm / 100f;
        List<Vector3> scaledPoints = new List<Vector3>();
        foreach (Vector3 point in pointCloudHandler.rawPoints)
        {
            Vector3 new_point = new Vector3(Mathf.RoundToInt(point.x / resolution) * resolution, Mathf.RoundToInt(point.y / resolution) * resolution, Mathf.RoundToInt(point.z / resolution) * resolution);
            if (!scaledPoints.Contains(new_point))
                scaledPoints.Add(new_point);
        }
        pointCloudHandler.rawPoints = scaledPoints.ToArray();
    }

    private IEnumerator DownloadImage()
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        if (request.isNetworkError || request.isHttpError)
            Debug.Log(request.error);
        else
            webTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;
        StartCoroutine(DownloadImage());
    }

    /*private void DownloadImage()
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        //yield return www.SendWebRequest();
        realCameraRaw.texture = DownloadHandlerTexture.GetContent(www);
        Debug.Log("Image updated");
    }*/


    // if positions written in txt file
    private void UpdateDronePose()
    {
        if (connectedToDrone)
        {
            string[] lines = File.ReadAllLines(Directory.GetCurrentDirectory() + "/" + dronePoseTextFile);
            string lastLine = lines[lines.Length - 1];
            string[] splitted = lastLine.Split(',');

            Debug.Log("Altitude: " + splitted[6].ToString());
            Vector3 position = new Vector3(float.Parse(splitted[5]), float.Parse(splitted[6]), float.Parse(splitted[4]));
            Quaternion rotation = new Quaternion(float.Parse(splitted[7]), float.Parse(splitted[8]), float.Parse(splitted[9]), float.Parse(splitted[10]));
            // dronePose = new Pose(position, rotation);

            playerController.transform.position = position;
            playerController.transform.rotation = rotation;
        }
    }
}
