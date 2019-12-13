using System.Collections;
using System.Collections.Generic;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using UnityEngine;
using System;

public class Communication : MonoBehaviour
{
    private string brokerHostname = "";
    private int brokerPort = 0;
    public string uplinkTopicBin = "feature_points";
    public string uplinkTopic = "request";
    public string downlinkTopic = "command";
    public bool communicationFlag = false;
    public const string broker = "broker";
    public const string port = "port";
    private MqttClient client;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (communicationFlag)
        {
            if (client == null)
            {
                Debug.Log("No MQTT client");
            }
        }
    }

    private bool Connect()
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
            communicationFlag = true;
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

    void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        string msg = System.Text.Encoding.UTF8.GetString(e.Message);
        string[] splitted = msg.Split(',');
    }

    public void Publish(string msg)
    {
        client.Publish(uplinkTopic, System.Text.Encoding.UTF8.GetBytes(msg), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
    }

    public void Publish(byte[] msg)
    {
        client.Publish(uplinkTopicBin, msg, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
    }

    public bool CallConnection(string broker, int port)
    {
        brokerHostname = broker;
        brokerPort = port;
        if (Connect())
        {
            return true;
        }
        return false;
    }
}
