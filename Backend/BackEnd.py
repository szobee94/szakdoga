import paho.mqtt.client as mqtt
import numpy as np
import csv
import math
import multiprocessing
import os
import glob
import time
import datetime
import keyboard
from queue import Empty
from multiprocessing import Queue

client_name = "ARRadioAppBackend"
broker_address = "192.168.1.100"
broker_port = 1883
topic_receive = "request"
topic_receive_bin = "request_bin"
topic_publish_mobile = "command"
topic_publish_desktop = "visualize"
data_min = np.nan
data_max = np.nan

save_dir = "saved_measures"

radio_buffer_threshold = 10

reading_q = Queue()
clear_complete = multiprocessing.Event()
raw_save_directory = None
raw_save_file = None

measure_ongoing = False

radio_data = None
radio_puffer = []

current_refpoints = dict()
reset_sent = False


def read_config_file(source):
    global client_name, broker_address, broker_port, topic_receive, topic_receive_bin, topic_publish_mobile, topic_publish_desktop
    global source_dir, save_dir, raw_save_dir
    global radio_buffer_threshold

    if os.path.isfile(source):
        print_wt("Reading config file from " + source)
    else:
        print_wt("Cannot read configuration, because " + source + " isn't a file.")
        return

    config_file = open(source, 'r')
    line = config_file.readline()
    while line:
        line = line.strip()

        if not line or line[0] == '#':
            line = config_file.readline()
            continue

        name = line.split('=')[0].strip()
        value = line.split('=')[1].strip()

        if name == "client_name":
            client_name = value
        elif name == "broker_address":
            broker_address = value
        elif name == "broker_port":
            broker_port = int(value)
        elif name == "receive_topic":
            topic_receive = value
        elif name == "receive_topic_bin":
            topic_receive_bin = value
        elif name == "publish_topic_mobile":
            topic_publish_mobile = value
        elif name == "publish_topic_desktop":
            topic_publish_desktop = value
        elif name == "save_dir":
            save_dir = value
        elif name == "radio_buffer_threshold":
            radio_buffer_threshold = int(value)

        line = config_file.readline()


def print_wt(message):
    print(str(datetime.datetime.now()) + ": " + message)


def on_message(client, userdata, message):

    if message.topic == topic_receive_bin:
        timestamp = np.frombuffer(message.payload[0:8], dtype=np.int64)[0]
        msg_id = np.frombuffer(message.payload[8:9], dtype=np.int8)[0]
        if msg_id == 1:
            refpoints_received = []
            for i in range(9, len(message.payload), 12):
                x = round_to_target(np.frombuffer(message.payload[i:(i + 4)], dtype=np.float32)[0], 0.1)
                y = round_to_target(np.frombuffer(message.payload[(i + 4):(i + 8)], dtype=np.float32)[0], 0.1)
                z = round_to_target(np.frombuffer(message.payload[(i + 8):(i + 12)], dtype=np.float32)[0], 0.1)
                try:
                    current_refpoints[(x, y, z)] += 1
                except KeyError:
                    current_refpoints[(x, y, z)] = 1
                    refpoints_received.append([x, y, z])
            if not reset_sent:
                message_desktop = "feature_points_reset"
                reset_sent = True
            else:
                message_desktop = "feature_points"
            for coord in refpoints_received:
                message_desktop += ',' + ','.join(("%.1f" % e) for e in coord)
            client.publish(topic_publish_desktop, message_desktop, qos=1)
            print_wt(message_desktop)


def save_refpoints(timestamp, save_name):
    global current_refpoints
    save_file = open(save_dir + '/' + timestamp + '_' + save_name + '_refpoints.csv', 'w')
    save_file.write("pos_x,pos_y,pos_z,aggr_num\n")
    for index in current_refpoints:
        save_file.write(','.join(("%.1f" % e) for e in index) + ',' + str(current_refpoints[index]) + '\n')
    save_file.close()


def get_save_list(mode):
    files = [os.path.basename(fn) for fn in glob.glob(save_dir + "/*_" + mode + ".csv")]
    files.sort()
    save_names = list()
    for fname in files:
        save_names.append(fname.split('.')[0])
    return save_names


def load_refpoints(save_name):
    global current_refpoints
    split = save_name.split('_')
    split[-1] = "refpoints"
    actual_save_name = '_'.join(split)
    files = [os.path.basename(fn) for fn in glob.glob(save_dir + '/' + actual_save_name + ".csv")]
    if len(files) == 0:
        files = [os.path.basename(fn) for fn in glob.glob(save_dir + "/*" + actual_save_name + ".csv")]
        if len(files) == 0:
            return False
    files.sort()
    file_name = files[-1]
    source = save_dir + '/' + file_name
    with open(source, 'r') as csv_file:
        current_refpoints.clear()
        csv_reader = csv.reader(csv_file, delimiter=',')
        row_count = 0
        for row in csv_reader:
            if row_count > 0:
                x, y, z = float(row[0]), float(row[1]), float(row[2])
                if len(row) > 3:
                    aggr_num = int(row[3])
                    current_refpoints[(x, y, z)] = aggr_num
                else:
                    current_refpoints[(x, y, z)] = 1
            row_count += 1
        print_wt("Loaded " + str(row_count - 1) + " reference points from " + file_name)
        csv_file.close()
    return True


"""def queue_readings():
    global measure_points, reading_q
    for index in measure_points:
        reading_q.put([index[0], index[1],
                       scale_reading(measure_points[index]['value']),
                       measure_points[index]['aggr_num']])
"""


def deg2coord(deg) -> [float]:
    return [np.cos(np.deg2rad(deg)), np.sin(np.deg2rad(deg))]


def coord2deg(x: float, y: float) -> float:
    if x == 0.0:
        if y > 0.0:
            return 90.0
        return 270.0
    deg = np.rad2deg(np.arctan(y / x))
    if x < 0.0:
        deg += 180.0
    elif y < 0.0:
        deg += 360.0
    return deg


def read_log(source):
    global radio_puffer

    sum_samples, burst_len, n_ports, n_rxant, rx_gain_offset, n_rs, pilot_est, rsrp, noise, times = rflog.load_files([source])
    if len(radio_puffer) >= radio_buffer_threshold:
        radio_puffer.clear()
    radio_puffer.append(rsrp[0][0])


def main():

global raw_save_directory, raw_save_file, save_dir, source_dir, broker_address

    config_source = "settings.conf"
    read_config_file(config_source)

    if not os.path.exists(source_dir):
        os.makedirs(source_dir)
    if not os.path.exists(save_dir):
        os.makedirs(save_dir)

    timestamp_str = str(int(time.time() * 1000))
    raw_save_directory = raw_save_dir + "/" + timestamp_str + "_measures_raw"
    if not os.path.exists(raw_save_directory):
        os.makedirs(raw_save_directory)
    raw_save_file = open(raw_save_dir + '/' + timestamp_str + '_position.csv', 'w')
    raw_save_file.write("timestamp\tpos_x\tpos_y\tpos_z\trot_x\trot_y\trot_z\tvalid\tmeasure\n")

    client = mqtt.Client(client_name)
    client.on_message = on_message
    client.connect(host=broker_address, port=broker_port)
    print_wt(client_name + " connected to " + broker_address)
    client.subscribe(topic_receive, qos=1)
    print_wt(client_name + " subscribed to topic: " + topic_receive)
    client.subscribe(topic_receive_bin, qos=1)

    client.loop_start()
    while True:
        if keyboard.is_pressed('esc'):
            print_wt("Escape is pressed, terminating...")
            break


    client.disconnect()
    raw_save_file.close()

if __name__ == "__main__":
    main()
