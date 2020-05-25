#!/usr/bin/env python

import paho.mqtt.client as mqtt
import os, sys
import pygame
from pygame.locals import *

import rospy
from mavros.utils import *
from mavros_msgs.msg import ManualControl
from geometry_msgs.msg import PoseStamped
from mavros_msgs.srv import CommandBool, SetMode
from mavros_msgs.msg import State

if not pygame.font: print "Warning, fonts disabled"
if not pygame.mixer: print "Warning, sound disabled"

current_state = State()

client_name = "Drone_backend"
broker_address = "10.42.0.1"
broker_port = 1883
topic_receive = "control"

armed, disarmed, up, down, left, right = False, False, False, False, False, False
forward, backward, go_left, go_right = False, False, False, False
keep_position = False

pose_data = PoseStamped()


def on_message(client, userdata, message):
    global armed, disarmed, up, down, left, right
    global forward, backward, go_left, go_right
    global keep_position
    global pose_data
    if message.topic == topic_receive:
        decoded = str(message.payload.decode("utf-8"))
        splitted = decoded.split(',')

        request = splitted[0]

        if request == "arm":
            armed = True
            keep_position, disarmed, up, down, left, right = False, False, False, False, False, False
            forward, backward, go_left, go_right = False, False, False, False
        elif request == "disarm":
            disarmed = True
            armed, keep_position, up, down, left, right = False, False, False, False, False, False
            forward, backward, go_left, go_right = False, False, False, False
        elif request == "up":
            up = True
            armed, disarmed, keep_position, down, left, right = False, False, False, False, False, False
            forward, backward, go_left, go_right = False, False, False, False
        elif request == "down":
            down = True
            armed, disarmed, up, keep_position, left, right = False, False, False, False, False, False
            forward, backward, go_left, go_right = False, False, False, False
        elif request == "right":
            right = True
            armed, disarmed, up, down, left, keep_position = False, False, False, False, False, False
            forward, backward, go_left, go_right = False, False, False, False
        elif request == "left":
            left = True
            armed, disarmed, up, down, keep_position, right = False, False, False, False, False, False
            forward, backward, go_left, go_right = False, False, False, False
        elif request == "forward":
            forward = True
            armed, disarmed, up, down, left, right = False, False, False, False, False, False
            keep_position, backward, go_left, go_right = False, False, False, False
        elif request == "backward":
            backward = True
            armed, disarmed, up, down, left, right = False, False, False, False, False, False
            forward, keep_position, go_left, go_right = False, False, False, False
        elif request == "go_left":
            go_left = True
            armed, disarmed, up, down, left, right = False, False, False, False, False, False
            forward, backward, keep_position, go_right = False, False, False, False
        elif request == "go_right":
            go_right = True
            armed, disarmed, up, down, left, right = False, False, False, False, False, False
            forward, backward, go_left, keep_position = False, False, False, False
        elif request == "keep_position":
            armed, disarmed, up, down, left, right = False, False, False, False, False, False
            forward, backward, go_left, go_right = False, False, False, False

        response = "actual_pose" + ',' + str(pose_data.pose.position.x) + ',' + str(pose_data.pose.position.y) + ',' + \
                   str(pose_data.pose.position.z) + ',' + str(pose_data.pose.orientation.x) + ',' + \
                   str(pose_data.pose.orientation.y) + ',' + str(pose_data.pose.orientation.z) + ',' + \
                   str(pose_data.pose.orientation.w)
        client.publish("pose", response, qos=1)


def send_pose(data):
    global pose_data
    pose_data = data


def state_cb(state):
    global current_state
    current_state = state


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


local_pos_pub = rospy.Publisher('/mavros/manual_control/send', ManualControl, queue_size=10)
arming_client = rospy.ServiceProxy('/mavros/cmd/arming', CommandBool)
set_mode_client = rospy.ServiceProxy('/mavros/set_mode', SetMode)
state_sub = rospy.Subscriber('/mavros/state', State, state_cb)
pose = rospy.Subscriber('/mavros/local_position/pose', PoseStamped, send_pose)


class Visual:
    def __init__(self, screen, width, height):
        self.screen = screen
        self.width = width
        self.height = height
        self.font = pygame.font.SysFont('Arial', 25, 1)

    def update(self, throttle, yaw, pitch, roll, arm_status):
        self.screen.fill((100, 100, 100))

        # left pos
        posLeftX = self.width / 4
        posLeftY = self.height / 2
        sizeLeft = min(self.height, self.width / 2) / 4 * 3

        # right pos
        posRightX = self.width / 4 * 3
        posRightY = self.height / 2
        sizeRight = min(self.height, self.width / 2) / 4 * 3

        # containers
        pygame.draw.rect(self.screen, (200, 200, 200),
                         ((posLeftX - sizeLeft / 2, posLeftY - sizeLeft / 4 / 2), (sizeLeft, sizeLeft / 4)))
        pygame.draw.rect(self.screen, (200, 200, 200),
                         ((posLeftX - sizeLeft / 4 / 2, posLeftY - sizeLeft / 2), (sizeLeft / 4, sizeLeft)))
        pygame.draw.circle(self.screen, (200, 200, 200), (posRightX, posRightY), sizeRight / 2)
        pygame.draw.rect(self.screen, (200, 200, 200),
                         ((posLeftX + 50, self.height - self.height / 10), (self.width / 3, self.height / 5)))

        # controls
        pygame.draw.circle(self.screen, (0, 0, 0),
                           (posRightX + int(roll * sizeRight / 2), posRightY - int(pitch * sizeRight / 2)),
                           sizeRight / 10)

        pygame.draw.rect(self.screen, (0, 0, 0), (
        (posLeftX - sizeLeft / 40 + int(yaw * sizeLeft / 2), posLeftY - sizeLeft / 10), (sizeLeft / 20, sizeLeft / 5)))
        pygame.draw.rect(self.screen, (int(throttle * 180), 0, 0), (
        (posLeftX - sizeLeft / 10, posLeftY - sizeLeft / 40 + sizeLeft / 2 - int(throttle * sizeLeft)),
        (sizeLeft / 5, sizeLeft / 20)))

        # status text
        if arm_status == 1:
            text = self.font.render("Armed", True, (255, 0, 0))
            text_rect = text.get_rect(center=(self.width / 2, self.width / 2 - 15))
            self.screen.blit(text, text_rect)
            rospy.loginfo("Screen updating armed")
        elif arm_status == 0:
            text = self.font.render("Disarmed", True, (255, 0, 0))
            text_rect = text.get_rect(center=(self.width / 2, self.width / 2 - 15))
            self.screen.blit(text, text_rect)
            """rospy.loginfo("Screen updating disarmed")     """

        # update
        pygame.display.flip()


class PyMain:
    def __init__(self, width=600, height=300):
        # Init pygame
        pygame.init()
        # Set window size
        self.width = width
        self.height = height
        # Create screen
        self.screen = pygame.display.set_mode((self.width, self.height))
        pygame.display.set_caption("Quadcopter Keyboard Control")
        # Create Visual
        self.font = pygame.font.SysFont('Arial', 25, 1)
        self.visual = Visual(self.screen, self.width, self.height)
        # Ros

        self.ros = Ros()

    def gotoDefault(self, value, step):
        if value > 0.0:
            return max(0.0, value - step)
        else:
            return min(0.0, value + step)

    def gotoDefaultThrottle(self, value, step, arm_status):
        if value > 0.54 and arm_status:
            return max(0.54, value - step)
        elif value <= 0.54 and arm_status:
            return min(0.54, value + step)
        else:
            return min(0.15, value + step)

    def MainLoop(self):
        throttle = 0.0
        yaw = 0.0  # rudder
        pitch = 0.0  # elevator
        roll = 0.0  # aileron
        step = 0.015
        stepBack = 0.03
        arm_status = current_state.armed
        pygame.key.set_repeat(30, 30)
        i = 0
        loop_freq = 100
        rate = rospy.Rate(loop_freq)  # 100 hz

        while not current_state.connected:
            rate.sleep()
        rospy.loginfo("Current mode: %s" % current_state.mode)
        last_request = rospy.get_rostime()
        rospy.loginfo("Vehicle armed: %r" % current_state.armed)
        while not rospy.is_shutdown():
            now = rospy.get_rostime()
            if not (
                    current_state.mode == "POSCTL" or current_state.mode == "CMODE(196608)" or current_state.mode == "MODE(0x 0)") and (
                    now - last_request > rospy.Duration(5.)):
                set_mode_client(base_mode=0, custom_mode="POSCTL")
                last_request = now
                rospy.loginfo("Current mode: %s" % current_state.mode)
                rospy.loginfo("Mode changed")

            for event in pygame.event.get():
                if event.type == pygame.QUIT:
                    sys.exit()

            pressed = pygame.key.get_pressed()
            if pressed[K_t] or armed and not arm_status:
                arming_client(True)
                arm_status = 1
                last_request = rospy.get_rostime()
                while not current_state.armed:
                    rate.sleep()

            if pressed[K_z] or disarmed and arm_status:
                arming_client(False)
                arm_status = 0
                last_request = rospy.get_rostime()
                while current_state.armed:
                    rate.sleep()

            if pressed[K_w] or up:
                throttle = min(1.0, throttle + step * 0.8)
            elif pressed[K_s] or down:
                throttle = max(0.15, throttle - step * 0.8)
            else:
                throttle = self.gotoDefaultThrottle(throttle, stepBack * 0.5, arm_status)

            if pressed[K_a] or left:
                yaw = max(-1.0, yaw - step)
                yaw_real = yaw * 1000
            elif pressed[K_d] or right:
                yaw = min(1.0, yaw + step)
                yaw_real = yaw * 1000
            else:
                yaw = self.gotoDefault(yaw, stepBack)
                yaw_real = yaw * 1000

            if pressed[K_UP] or forward:
                pitch = min(0.6, pitch + step)
                pitch_real = pitch * 1000
            elif pressed[K_DOWN] or backward:
                pitch = max(-0.6, pitch - step)
                pitch_real = pitch * 1000
            else:
                pitch = self.gotoDefault(pitch, stepBack)
                pitch_real = pitch * 1000
            if pressed[K_LEFT] or go_left:
                roll = max(-0.6, roll - step)
                roll_real = roll * 1000
            elif pressed[K_RIGHT] or go_right:
                roll = min(0.6, roll + step)
                roll_real = roll * 1000
            else:
                roll = self.gotoDefault(roll, stepBack)
                roll_real = roll * 1000

            self.visual.update(throttle, yaw, pitch, roll, arm_status)
            self.ros.update(throttle * 1000, yaw_real, pitch_real, roll_real)
            rate.sleep()


def main():
    print ("start...")
    client = mqtt.Client(client_name)
    client.on_message = on_message
    client.connect(host="10.42.0.1", port=1883)
    print (client_name + " connected to " + "10.42.0.1")
    client.subscribe("control", qos=1)
    print (client_name + " subscribed to topic: " + "control")
    client.loop_start()
    try:
        MainWindow = PyMain()
        MainWindow.MainLoop()
    except rospy.ROSInterruptException:
        pass


if __name__ == '__main__':
    main()