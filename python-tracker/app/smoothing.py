import numpy as np

class EMA:
    def __init__(self, beta=0.8):
        self.beta = beta
        self.state = None

    def step(self, vec):
        vec = np.asarray(vec)
        if self.state is None:
            self.state = vec
        else:
            self.state = self.beta*self.state + (1-self.beta)*vec
        return self.state

class BoneSmoother:
    def __init__(self, beta=0.7):
        self.filters = {}
        self.beta = beta

    def smooth_dict(self, bone_dirs):
        out = {}
        for k, v in bone_dirs.items():
            if k not in self.filters:
                self.filters[k] = EMA(self.beta)
            out[k] = self.filters[k].step(v)
        return out
