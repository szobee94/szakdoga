#!/usr/bin/env python

import os, sys
import pygame
from pygame.locals import *

import rospy
from mavros.utils import *
from mavros_msgs.msg import ManualControl, State
from geometry_msgs.msg import Vector3
from mavros_msgs.srv import CommandBool, CommandLong
from drone_flight_contorl.msg import Arucodata
from sensor_msgs.msg import Range

import paho.mqtt.client as mqtt

current_state = State()

client_name = "ARRadioAppBackend"
broker_address = "192.168.1.100"
broker_port = 1883
topic_receive = "command"
topic_publish_oculus = "visualize"

position_Flag = False
forward_flag = False
right_flag = False
left_flag = False
takeoff_flag = False
land_flag = False
stop_flag = True

def state_cb(state):
    global current_state
    current_state = state

def on_message(client: mqtt.Client, userdata, message: mqtt.MQTTMessage):
    global position_flag
    if message.topic == topic_receive:
        decoded = str(message.payload.decode("utf-8"))
        splitted = decoded.split(',')

        request = splitted[0]

        if request == "start":
            position_flag = True

        elif request == "forward":
            forward_flag = True

        elif request == "stop":
            stop_flag = True
	    forward_flag = False
	    right_flag = False
	    left_flag = False

        elif request == "right":
            if left_flag == True
		stop_flag = True
		left_flag = False
	    else
		right_flag = True

        elif request == "left":
            if right_flag == True
		stop_flag = True
		right_flag = False
	    else
		left_flag = True

        elif request == "take_off":
            takeoff_flag = True

        elif request == "land":
            land_flag = True


class Ros:
    def __init__(self):
        self.publisher = rospy.Publisher('/mavros/manual_control/send', ManualControl, queue_size=10)
        rospy.init_node("InfineonSensorData", anonymous=True)

    def update(self, throttle, yaw, pitch, roll):
        data = ManualControl()
        data.header.stamp = rospy.Time.now()
        data.z = throttle
        data.x = pitch
        data.y = roll
        data.r = yaw
        data.buttons = 0
        self.publisher.publish(data)


class PyMain:
    def __init__(self, width=200, height=30):
        # Init pygame
        pygame.init()
        # Set window size
        self.width = width
        self.height = height
        # Create screen
        self.screen = pygame.display.set_mode((self.width, self.height))
        pygame.display.set_caption("Quadcopter Keyboard Control")

        self.aruco_data = dict()
        self.distance = Range()
        #self.current_state = State()
        self.flying = False
        self.mission_start = False
        self.mission_end = False
        self.ros = Ros()

    def set_data(self, data):
        rospy.loginfo_once("Arucodata data received")
        if data.id != -1:  # Ilyenkor van ervenyes adat.
            # Itt ha van adat felulirom, ha nincs akkor beleteszem
            self.aruco_data[data.id] = (rospy.get_time(), data.pos.linear)

    def set_distance(self, data):
        rospy.loginfo_once("Distance data received")
        self.distance = data
    """
    def set_state(self, state):
        rospy.loginfo_once("State data received")
        self.current_state = state
        """

    # Ez a fuggvany automatikusan leszallitja a dron-t
    def leszall(self, rate):
        rospy.loginfo("Stop!")
        # Akkor lepjen ki ha vege a programnak vagy mar nincs armolva
        while current_state.armed and (not rospy.is_shutdown()):
            # Ha ugyanaz a magassag akkor az csak azt jelenti, hogy nincs uj adat
            rospy.loginfo_once("Going down height")
            throttle = 0.15
            self.ros.update(throttle * 1000, 0.0, 0.0, 0.0)
            rate.sleep()
        self.flying = False
        self.mission_start = False

    # A max id-t adja vissza, ha nincs -1
    def calc_aruco_data(self, ttl):
        maxid = -1  # Az id mindig pozitiv
        for key, value in self.aruco_data.items():
            if value[0] + ttl < rospy.get_time():  # Lejart az idobelyeg
                rospy.loginfo("%d id expired delet", key)
                del self.aruco_data[key]
            else:
                if key > maxid:
                    maxid = key
        return maxid

    def MainLoop(self):
        global position_flag, topic_publish_oculus

        throttle = 0.0
        yaw = 0.0       # rudder
        pitch = 0.0     # elevator
        roll = 0.0      # aileron
        max_roll = 0.46

        step = 0.03
        stepBack = 0.08

        ttl = 0.5  # Ha tul kicsi a parhuzamos futas miatt lehet baj

        take_off_MAX = 1.5
        min_dist = 0.05
        lin_dist = 0.22

        stop_id = 144

        try:
            arming_client = rospy.ServiceProxy('/mavros/cmd/arming', CommandBool)
            set_camera = rospy.ServiceProxy('/mavros/cmd/command', CommandLong)

            rospy.Subscriber('pos', Arucodata, self.set_data)
            rospy.Subscriber('/mavros/px4flow/ground_distance', Range, self.set_distance)
            rospy.Subscriber('/mavros/state', State, state_cb)

        except rospy.ROSInterruptException as a:
            rospy.loginfo("There is some problem with connecting to the nodes: %s", a)

        pygame.key.set_repeat(30, 30)
        loop_freq = 50  # 100Hz, majd ellenorizni kell
        rate = rospy.Rate(loop_freq)
        rospy.loginfo("Connecting...")
        while not (current_state.connected or rospy.is_shutdown):
            rate.sleep()
        rospy.loginfo("Connected to the drone!")
        rospy.loginfo("Set camera...")
        result = set_camera(command=205, param1=-90.0, param7=2.0)
        rospy.loginfo("Camera set result: %s", str(result.success))
        rospy.loginfo("Current mode: %s" % current_state.mode)
        rospy.loginfo("Vehicle armed: %r" % current_state.armed)
        rospy.loginfo("Starting main loop")

        while not rospy.is_shutdown():
            # Biztonsag kedveert
            if not current_state.armed:
                self.flying = False
            if not self.flying:
                self.mission_start = False

            id = self.calc_aruco_data(ttl)
            if id != -1:
                rospy.loginfo("current id: %d and pos_x: %f pos_y: %f pos_z: %f", id, self.aruco_data[id][1].x, self.aruco_data[id][1].y, self.aruco_data[id][1].z)
		if position_flag:
                    id = self.calc_aruco_data(ttl)
                    response = "position"
                    response += ',' + str(id) + ',' + self.aruco_data[id][1].x + ',' + self.aruco_data[id][1].y + ',' + \
                                self.aruco_data[id][1].z + ',' + throttle + ',' + yaw+ ',' + pitch + ',' + roll
                    client.publish(topic_publish_oculus, response, qos=1)

            for event in pygame.event.get():
                if event.type == pygame.QUIT:
                    rospy.loginfo("Exit, going down")
                    self.leszall(rate)
                    rospy.loginfo("Bye")
                    sys.exit()
            pressed = pygame.key.get_pressed()

            if takeoff_flag and not current_state.armed:
                rospy.loginfo("START")
                arm_result = arming_client(True)
                rospy.loginfo("Arm set result: %s", str(arm_result.success))
                rospy.loginfo("Vehicle armed: %r" % current_state.armed)
                rospy.loginfo("Waiting for arming (in case of automatic disarm quit)")
                while not (current_state.armed or rospy.is_shutdown()):  # Akkor lepjen ki ha vege a programnak vagy ha armolva van
                    rate.sleep()
                rospy.loginfo("Armed")
                rospy.loginfo("Starting to fly")
                while current_state.armed and self.distance.range < take_off_MAX and (not rospy.is_shutdown()):
                    throttle = 0.72
                    self.ros.update(throttle * 1000, 0.0, 0.0, 0.0)
                    rospy.loginfo("Take off, height: %f", self.distance.range)
                rospy.loginfo_once("Takeoff end, I am in the air")
                # Felszallas utan lebeges
                throttle = 0.55
                rospy.loginfo("Hovering")
                self.ros.update(throttle * 1000, 0.0, 0.0, 0.0)
                self.flying = True

            """if pressed[K_k] and self.flying:
                rospy.loginfo("Starting mission")
                self.mission_start = True"""

            if land_flag:
                rospy.loginfo("ABORT")
                self.leszall(rate)

	    if right_flag:
		throttle = 0.55
		yaw = 0.10
		rospy.loginfo("Turning right")
	
	    if left_flag:
		throttle = 0.55
		yaw = -0.10
		rospy.loginfo("Turning left")
	
	    if forward_flag:
		throttle = 0.55
		pitch = 0.26

	    if stop_flag:
		throttle = 0.55
		yaw = 0.0
		pitch = 0.0
	
            """if self.mission_start:
                throttle = 0.55
                if self.mission_end:
                    rospy.loginfo("Mission end, going down")
                    pitch = 0
                    roll = 0
                    self.leszall(rate)
                else:
                    if id > 0:
                        if id == stop_id:
                            rospy.loginfo("Got the, code mission end")
                            self.mission_end = True
                        else:
                            position = self.aruco_data[id][1]
                            # Y iranyu mozgas
                            if position.y < -min_dist:
                                # Lassan megy elore
                                pitch = 0.26
                                rospy.loginfo("Going forward slowly")
                            else:
                                # Azert kell, hogy az utolso felett megaljak
                                # Ha tobb kod van latok nagyobbat es igy megyek elore
                                rospy.loginfo("Stop going forward")
                                pitch = 0
                            # X iranyu mozgas
                            if position.x < min_dist and position.x > -min_dist:
                                roll = 0
                                rospy.loginfo("Above a code no side going")
                            elif min_dist <= position.x and position.x < (min_dist + lin_dist):
                                roll = -max_roll * position.x/(min_dist + lin_dist)
                                rospy.loginfo("Current roll: %f", roll)
                            elif -min_dist >= position.x and position.x > -(min_dist + lin_dist):
                                #position.x itt negative -> figyelin kell a roll-ra
                                roll = -max_roll * position.x/(min_dist + lin_dist)
                                rospy.loginfo("Current roll: %f", roll)
                            elif position.x >= (min_dist + lin_dist):
                                roll = -max_roll
                                rospy.loginfo("MAX roll: %f", roll)
                            elif position.x <= -(min_dist + lin_dist):
                                roll = max_roll
                                rospy.loginfo("MAX roll: %f", roll)
                            # mozgas kuldese:
                            self.ros.update(throttle*1000, yaw*1000, pitch*1000, roll*1000)
                    else:
                        rospy.loginfo("No data from aruco, waiting")
                        pitch = 0
                        roll = 0
                        self.ros.update(throttle*1000, yaw*1000, pitch*1000, roll*1000)"""
            self.ros.update(throttle*1000, yaw*1000, pitch*1000, roll*1000)
            rate.sleep()


if __name__ == "__main__":

    client = mqtt.Client(client_name)
    client.on_message = on_message
    client.connect(host=broker_address, port=broker_port)
    client.subscribe(topic_receive, qos=1)

    client.loop_start()
    try:
        MainWindow = PyMain()
        MainWindow.MainLoop()
    except rospy.ROSInterruptException as a:
        rospy.loginfo("Something was not working....:()")
