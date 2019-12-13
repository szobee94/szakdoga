using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

[RequireComponent(typeof(WallHandler), typeof(ArucoHandler), typeof(PointCloudHandler))]
public class ObjectInitializer : MonoBehaviour
{
    private WallHandler wallHandler;
    private ArucoHandler arucoHandler;
    private PointCloudHandler pointCloudHandler;
    
    private OVRCameraRig playerCamera;
    private OVRPlayerController ovrPlayerController;
    private OVRManager ovrManager;

    /* Communication parameters */
    private string brokerHostname;
    private int brokerPort;
    private string droneBrokerHostname = "192.168.42.1";
    private int droneBrokerPort = 14557;
    private string uplinkTopic = "command";
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

    public OVRPlayerController playerController;

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

    private bool connectToDrone = false;

    // Start is called before the first frame update
    void Start()
    {
        wallHandler = GetComponent<WallHandler>();
        arucoHandler = GetComponent<ArucoHandler>();
        pointCloudHandler = GetComponent<PointCloudHandler>();

        selectionPanel.SetActive(false);
        ipInput.text = "192.168.0.17";
        portInput.text = "1883";

        ipInput.onEndEdit.AddListener(SetIp);
        portInput.onEndEdit.AddListener(SetPort);
        resolutionInputField.onEndEdit.AddListener(SetResolution);
        connectButton.onClick.AddListener(TryConnect);
        mapDropdown.onValueChanged.AddListener(delegate { LoadMap(mapDropdown); });
        viewButton.onClick.AddListener(ViewMap);

        playerController = FindObjectOfType<OVRPlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) || forwardFlag || OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
            GoForward();
        if (Input.GetKeyDown(KeyCode.S) || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
            Stop();
        if (Input.GetKeyDown(KeyCode.D) || OVRInput.GetDown(OVRInput.Button.SecondaryThumbstickRight) || OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickRight))
            TurnRight();
        if (Input.GetKeyDown(KeyCode.A) || OVRInput.GetDown(OVRInput.Button.SecondaryThumbstickLeft) || OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickLeft))
            TurnLeft();
        if (Input.GetKeyDown(KeyCode.L) || landFlag || OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three))
            Land();
        if (Input.GetKeyDown(KeyCode.T) || takeoffFlag || OVRInput.GetDown(OVRInput.Button.Two) || OVRInput.GetDown(OVRInput.Button.Four))
            TakeOff();
        if (positionFlag)
        {
            playerController.transform.localPosition = new Vector3(dronePosition.x, dronePosition.y, dronePosition.z);
            if (!lastQuaternion.Equals(droneQuaternion))
                playerController.transform.localRotation = new Quaternion(droneQuaternion.w, droneQuaternion.x, droneQuaternion.y, droneQuaternion.z);
            lastQuaternion = new Quaternion(droneQuaternion.w, droneQuaternion.x, droneQuaternion.y, droneQuaternion.z);
            positionFlag = false;
        }
        if (defaultYaw != Mathf.NegativeInfinity)
            currentYaw = droneQuaternion.y - defaultYaw;
        if(connectToDrone)
        {
            while (!CheckTcpConnection(droneBrokerHostname, droneBrokerPort)) //need to know port and ip
            {
                System.Threading.Thread.Sleep(1000);
                if (Input.GetKeyDown(KeyCode.Escape))
                    Application.Quit();
            }
            if (Connect(droneBrokerHostname, droneBrokerPort))
                SendStartMessage();
            connectToDrone = false;
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
            client.Subscribe(new string[] { downlinkTopic }, qosLevels);
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
            ParseMessage(splitted, out pointCloudHandler.rawPoints);
        }
        else if (splitted[0].Equals("arucos"))
        {
            ParseArucoMessage(splitted, out Vector3[] output, out int[] numbers);
            arucoHandler._arucos.AddRange(output);
            arucoHandler._numbers.AddRange(numbers);
        }
        else if (splitted[0].Equals("position"))
        {
            ParsePositionMessage(splitted,out arucoIndex, out dronePosition, out droneQuaternion);
            positionFlag = true;
            if (defaultYaw == Mathf.NegativeInfinity)
                defaultYaw = droneQuaternion.y; //not sure
            takeoffFlag = false;
        }
    }

    private void ParseMessage(string[] msg, out Vector3[] output)
    {
        int length = (int)((msg.Length - 1) / 3);
        output = new Vector3[length];
        for(int i = 0; i < length; i++)
        {
            output[i] = new Vector3(float.Parse(msg[(i * 3) + 1], CultureInfo.InvariantCulture), float.Parse(msg[(i * 3) + 2], CultureInfo.InvariantCulture), float.Parse(msg[(i * 3) + 3], CultureInfo.InvariantCulture));
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
        client.Publish(uplinkTopic, System.Text.Encoding.UTF8.GetBytes(msg), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
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

    private void SendStartMessage()
    {
        string msg = "start";
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
            UpdateResolution();
        }
        pointCloudHandler.SetPointCloud();
        pointCloudHandler.GetFloor(out wallHandler.minVector, out wallHandler.maxVector);
        wallHandler.Draw();
        directionalLight.transform.localPosition = new Vector3((wallHandler.maxVector.y + wallHandler.minVector.y) / 2f, 10.0f, (wallHandler.maxVector.x + wallHandler.minVector.x) / 2f);
        externalCamera.GetComponent<MiniMap>().cameraPosition = new Vector3(wallHandler.minVector.y + 0.1f, wallHandler.maxVector.z + 0.1f, wallHandler.minVector.x + 0.1f);
        selectionPanel.SetActive(false);
        playerController.transform.position = new Vector3(0.1f, 0.26f, 0.1f);
        takeoffFlag = true;
        client.Disconnect();
    }

    private void ConnectToDrone()
    {
        while (!CheckTcpConnection(droneBrokerHostname, droneBrokerPort)) //future check
        {
            System.Threading.Thread.Sleep(1000);
        }
        if (Connect(droneBrokerHostname, droneBrokerPort))
            SendStartMessage();
    }

    private void UpdateResolution()
    {
        float resolution = resolution_cm / 100f;
        List<Vector3> scaledPoints = new List<Vector3>();
        foreach (Vector3 point in pointCloudHandler.rawPoints)
        {
            Vector3 new_point = new Vector3(Mathf.RoundToInt(point.x / resolution) * resolution, Mathf.RoundToInt(point.y /resolution) * resolution, Mathf.RoundToInt(point.z / resolution) * resolution);
            if (!scaledPoints.Contains(new_point))
                scaledPoints.Add(new_point);
        }
        pointCloudHandler.rawPoints = scaledPoints.ToArray();
    }

    private void GoForward()
    {
        Publish("forward");
        forwardFlag = true;
        playerController.transform.Translate(0, 0, 0.02f, Space.Self);
    }

    private void Stop()
    {
        Publish("stop");
        forwardFlag = false;
        playerController.transform.Translate(0, 0, 0, Space.Self);
    }

    private void TurnRight()
    {
        Publish("right");
        playerController.transform.Rotate(0, 5, 0);
    }

    private void TurnLeft()
    {
        Publish("left");
        playerController.transform.Rotate(0, -5, 0);
    }

    private void TakeOff()
    {
        if (playerController.transform.position.y < 1.5f && !landFlag)
        {
            Publish("take_off");
            playerController.transform.Translate(0, 0.02f, 0f, Space.Self);
            takeoffFlag = true;
        }
        else
            takeoffFlag = false;
    }

    private void Land()
    {
        if (playerController.transform.position.y > 0.26f && !takeoffFlag)
        {
            Stop();
            Publish("land");
            playerController.transform.Translate(0, -0.02f, 0f, Space.Self);
            landFlag = true;
        }
        else
            landFlag = false;
    }
}
