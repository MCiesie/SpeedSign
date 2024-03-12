import pickle
import numpy
import numpy as np
import sklearn
import random

import os

os.environ["PYTHONPATH"]


def get_prediction(ergonomics_data: numpy.ndarray[np.float32]) -> float:
    print(ergonomics_data)
    return numpy.random.randint(0, 21)
    