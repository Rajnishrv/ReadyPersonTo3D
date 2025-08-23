import time

class PersonGate:
    def __init__(self, stable_frames=8, absent_timeout=0.8):
        self.present_counter = 0
        self.last_seen = 0
        self.is_present = False
        self.stable_frames = stable_frames
        self.absent_timeout = absent_timeout

    def step(self, detected):
        now = time.time()
        if detected:
            self.last_seen = now
            self.present_counter += 1
            if not self.is_present and self.present_counter >= self.stable_frames:
                self.is_present = True
                return "entered"
        else:
            self.present_counter = 0
            if self.is_present and (now - self.last_seen) > self.absent_timeout:
                self.is_present = False
                return "left"
        return "none"
