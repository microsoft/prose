import pandas
import os
import numpy as np
from itertools import combinations
from typing import Any


def metric_pass_k(expected_answer, answer: Any, k: int) -> float:
    if not isinstance(answer, list):
        raise ValueError("Answer should be a list for pass@k metric.")

    # Convert the test.expect to a list if it's not already
    expected_values = expected_answer if isinstance(
        expected_answer, list) else [expected_answer]
    expected_values = list(map(str, expected_values))
    boolean_answers = [
        answer[i] in expected_values for i in range(len(answer))]
    combinations_k = list(combinations(boolean_answers, k))
    passed_at_k = 0
    # Calculate the pass@k metric
    for comb in combinations_k:
        if any(comb):
            passed_at_k += 1
    pass_at_k_percentage = (passed_at_k / len(combinations_k))*100

    return pass_at_k_percentage
