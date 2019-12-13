import paho.mqtt.client as mqtt
import os
import csv
import datetime
import keyboard
import numpy as np

client_name = "ARRadioAppBackend"
broker_address = "192.168.1.100"
broker_port = 1883
topic_receive = "command"
topic_publish_mobile = "response"
topic_receive_bin = "feature_points"
topic_publish_oculus = "visualize"

save_dir = "saved_measures"
save_folder = ""

current_refpoints = dict()
current_walls = dict()
current_arucos = dict()
reset_sent = False


def read_config_file(source):
    global client_name, broker_address, broker_port, topic_receive, topic_receive_bin, topic_publish_mobile, topic_publish_oculus
    global save_dir

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
        elif name == "publish_topic_oculus":
            topic_publish_oculus = value
        elif name == "save_dir":
            save_dir = value

        line = config_file.readline()


def print_wt(message):
    print(str(datetime.datetime.now()) + ": " + message)


def on_message(client: mqtt.Client, userdata, message: mqtt.MQTTMessage):
    global save_dir, save_folder, current_refpoints, reset_sent, current_walls, current_arucos
    if message.topic == topic_receive:
        decoded = str(message.payload.decode("utf-8"))
        splitted = decoded.split(',')
        print_wt(message.topic + ", " + decoded)

        request = splitted[0]

        if request == "arucos":
            assert len(splitted) >= 4
            for i in range(1, len(splitted), 3):
                x = float(splitted[i])
                y = float(splitted[i + 1])
                z = float(splitted[i + 2])
                try:
                    current_arucos[(x, y, z)] += 1
                except KeyError:
                    current_arucos[(x, y, z)] = 1
            print_wt("Arucos arrived")
            save_arucos()

        elif request == "feature_points":
            for i in range(1, len(splitted), 3):
                x = float(splitted[i])
                y = float(splitted[i + 1])
                z = float(splitted[i + 2])
                try:
                    current_refpoints[(x, y, z)] += 1
                except KeyError:
                    current_refpoints[(x, y, z)] = 1
            print_wt("Feature points arrived")
            save_refpoints()

        elif request == "walls":
            for i in range(1, len(splitted), 3):
                x = float(splitted[i])
                y = float(splitted[i + 1])
                z = float(splitted[i + 2])
                try:
                    current_walls[(x, y, z)] += 1
                except KeyError:
                    current_walls[(x, y, z)] = 1
            print_wt("Walls arrived")
            save_walls()

        elif request == "save":
            save_folder = save_dir + '/' + splitted[1]
            os.makedirs(save_folder)
            print_wt("Folder has been created: " + str(save_folder))

        elif request == "get_save_list":
            response = "saved_maps_list" + ',' + ','.join(str(e) for e in get_save_list())
            client.publish(topic_publish_oculus, response, qos=1)

        elif request == "map":
            if not splitted[1]:
                print_wt("The 'save_name' string was empty or None.")
                return
            if load_arucos(splitted[1]):
                response = "arucos"
                for row in current_arucos:
                    response += ',' + ','.join(str(e) for e in row)
                client.publish(topic_publish_oculus, response, qos=1)
            if load_walls(splitted[1]):
                response = "walls"
                for row in current_walls:
                    response += ',' + ','.join(str(e) for e in row)
                client.publish(topic_publish_oculus, response, qos=1)
            if load_refpoints(splitted[1]):
                response = "feature_points"
                for index in current_refpoints:
                    response += ',' + ','.join(str(e) for e in index)
                client.publish(topic_publish_oculus, response, qos=1)
                reset_sent = True

    elif message.topic == topic_receive_bin:
        msg_id = np.frombuffer(message.payload[0:1], dtype=np.int8)[0]
        if msg_id == 1:
            refpoints_received = []
            for i in range(1, len(message.payload), 12):
                x = round_to_target(np.frombuffer(message.payload[i:(i + 4)], dtype=np.float32)[0], 0.01)
                y = round_to_target(np.frombuffer(message.payload[(i + 4):(i + 8)], dtype=np.float32)[0], 0.01)
                z = round_to_target(np.frombuffer(message.payload[(i + 8):(i + 12)], dtype=np.float32)[0], 0.01)
                try:
                    current_refpoints[(x, y, z)] += 1
                except KeyError:
                    current_refpoints[(x, y, z)] = 1
                    refpoints_received.append([x, y, z])
        print_wt("Binary pointcloud arrived")
        save_refpoints()


def round_to_target(value: float, target: float):
    return float(round(value / target)) * target


def get_save_list():
    files = [os.path.basename(fn) for fn in os.listdir(save_dir)]
    files.sort()
    save_names = list()
    for fname in files:
        save_names.append(fname.split('.')[0])
    print_wt("saved measures: " + str(save_names))
    return save_names


def save_refpoints():
    global current_refpoints
    save_file = open(save_folder + '/' + 'refpoints.csv', 'w')
    save_file.write("pos_x,pos_y,pos_z\n")
    for index in current_refpoints:
        save_file.write(','.join(("%.2f" % e) for e in index) + '\n')
    save_file.close()


def save_arucos():
    global current_arucos
    save_file = open(save_folder + '/' + 'arucos.csv', 'w')
    save_file.write("pos_x,pos_y,pos_z\n")
    for index in current_arucos:
        save_file.write(','.join(str(e) for e in index) + '\n')
    save_file.close()


def save_walls():
    global current_walls
    save_file = open(save_folder + '/' + 'walls.csv', 'w')
    save_file.write("pos_x,pos_y,pos_z\n")
    for index in current_walls:
        save_file.write(','.join(str(e) for e in index) + '\n')
    save_file.close()


def load_refpoints(save_name: str):
    global current_refpoints
    actual_save_name = save_name + "/" + "refpoints.csv"
    """
    files = [os.path.basename(fn) for fn in glob.glob(save_dir + "/" + actual_save_name + "*.csv")]
    if len(files) == 0:
        return False
    files.sort()
    file_name = files[-1]
    """
    source = save_dir + '/' + actual_save_name
    if not os.path.isfile(source):
        return False
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
        print_wt("Loaded " + str(row_count - 1) + " feature points from " + actual_save_name)
        csv_file.close()
    return True


def load_arucos(save_name: str):
    global current_arucos
    actual_save_name = save_name + "/" + "arucos.csv"
    """
    files = [os.path.basename(fn) for fn in glob.glob(save_dir + "/" + actual_save_name + ".csv")]
    if len(files) == 0:
        return False
    files.sort()
    file_name = files[-1]
    """
    source = save_dir + '/' + actual_save_name
    if not os.path.isfile(source):
        return False
    print_wt(str(source))
    with open(source, 'r') as csv_file:
        current_arucos.clear()
        csv_reader = csv.reader(csv_file, delimiter=',')
        row_count = 0
        for row in csv_reader:
            if row_count > 0:
                x, y, z = float(row[0]), float(row[1]), float(row[2])
                if len(row) > 3:
                    aggr_num = int(row[3])
                    current_arucos[(x, y, z)] = aggr_num
                else:
                    current_arucos[(x, y, z)] = 1
            row_count += 1
        print_wt("Loaded " + str(row_count - 1) + " arucos from " + actual_save_name)
        csv_file.close()
    return True


def load_walls(save_name: str):
    global current_walls
    actual_save_name = save_name + "/" + "walls.csv"
    """
    files = [os.path.basename(fn) for fn in glob.glob(save_dir + "/" + actual_save_name + ".csv")]
    if len(files) == 0:
        return False
    files.sort()
    file_name = files[-1]
    """
    source = save_dir + '/' + actual_save_name
    if not os.path.isfile(source):
        return False
    with open(source, 'r') as csv_file:
        current_walls.clear()
        csv_reader = csv.reader(csv_file, delimiter=',')
        row_count = 0
        for row in csv_reader:
            if row_count > 0:
                x, y, z = float(row[0]), float(row[1]), float(row[2])
                if len(row) > 3:
                    aggr_num = int(row[3])
                    current_walls[(x, y, z)] = aggr_num
                else:
                    current_walls[(x, y, z)] = 1
            row_count += 1
        print_wt("Loaded " + str(int((row_count - 1) / 4)) + " wall(s) from " + actual_save_name)
        csv_file.close()
    return True


def main():

    global save_dir, broker_address

    config_source = "settings.conf"
    read_config_file(config_source)

    if not os.path.exists(save_dir):
        os.makedirs(save_dir)

    client = mqtt.Client(client_name)
    client.on_message = on_message
    client.connect(host=broker_address, port=broker_port)
    print_wt(client_name + " connected to " + broker_address)
    client.subscribe(topic_receive, qos=1)
    client.subscribe(topic_receive_bin, qos=1)
    print_wt(client_name + " subscribed to topic: " + topic_receive + " and " + topic_receive_bin)

    client.loop_start()
    while True:
        if keyboard.is_pressed('esc'):
            print_wt("Escape is pressed, terminating...")
            break

    client.loop_stop()
    client.disconnect()


if __name__ == "__main__":
    main()