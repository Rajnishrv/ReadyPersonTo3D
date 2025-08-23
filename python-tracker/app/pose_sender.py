import json, socket

class PoseSender:
    def __init__(self, host="127.0.0.1", port=5056):
        self.addr = (host, port)
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    def send(self, gender_label, event, bones_dict):
        msg = {
            "type": "pose",
            "gender": gender_label,
            "event": event,
            "bones": {k: list(map(float, v)) for k,v in bones_dict.items()}
        }
        data = json.dumps(msg).encode("utf-8")
        self.sock.sendto(data, self.addr)
