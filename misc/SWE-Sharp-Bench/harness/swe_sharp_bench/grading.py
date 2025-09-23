from typing import Any

from swebench.harness.constants import (
    KEY_INSTANCE_ID, 
    KEY_PREDICTION, 
    FAIL_TO_PASS, 
    PASS_TO_PASS, 
    ResolvedStatus,
    APPLY_PATCH_FAIL,
    RESET_FAILED,
    TESTS_ERROR,
    TESTS_TIMEOUT,
    FAIL_ONLY_REPOS,
    EvalType,
)
from swebench.harness.grading import get_eval_tests_report, get_resolution_status

from swe_sharp_bench.test_spec import TestSpec
from swe_sharp_bench.constants import MAP_REPO_VERSION_TO_SPECS
from swe_sharp_bench.log_parsers import MAP_REPO_TO_PARSER, parse_log_dotnet_test

def get_eval_type(test_spec: TestSpec) -> str:
    if test_spec.repo in FAIL_ONLY_REPOS:
        return EvalType.FAIL_ONLY
    return EvalType.PASS_AND_FAIL

def get_logs_eval(test_spec: TestSpec, log_fp: str) -> tuple[dict[str, str], bool]:
    """
    Retrieve evaluation results for a task instance from its corresponding log file

    Args:
        log_fp (str): path to log file
    Returns:
        bool: whether the patch applied successfully
        dict: status map
    """
    repo = test_spec.repo
    version = test_spec.version
    log_parser = MAP_REPO_TO_PARSER.get(repo, None)
    if not log_parser:
        log_parser = parse_log_dotnet_test
    
    test_cmd = MAP_REPO_VERSION_TO_SPECS.get(repo, {}).get(version, {}).get("test_cmd", None)
    if not test_cmd:
        eval_script = test_spec.eval_script
        temp_lines = eval_script.split("\n")
        test_cmd = temp_lines[-4]
        print(f"Using test_cmd from eval_script: {test_cmd}")
    
    
    if isinstance(test_cmd, list):
        test_cmd = test_cmd[-1]

    with open(log_fp) as f:
        content = f.read()
        bad_codes = list(
            filter(
                lambda x: x in content,
                [
                    APPLY_PATCH_FAIL,
                    RESET_FAILED,
                    TESTS_ERROR,
                    TESTS_TIMEOUT,
                ],
            )
        )
        if bad_codes:
            return {}, False

        # Get status map of evaluation results
        content = content.split(test_cmd)[-1]
        return log_parser(content, test_spec), True

def get_eval_report(
    test_spec: TestSpec,
    prediction: dict[str, str],
    test_log_path: str,
    include_tests_status: bool,
) -> dict[str, Any]:
    """
    Generate a report of model evaluation results from a prediction, task instance,
    and evaluation log.

    Args:
        test_spec (dict): test spec containing keys "instance_id", "FAIL_TO_PASS", and "PASS_TO_PASS"
        prediction (dict): prediction containing keys "instance_id", "model_name_or_path", and "model_patch"
        log_path (str): path to evaluation log
        include_tests_status (bool): whether to include the status of each test in the returned report
    Returns:
        report (dict): report of metrics
    """
    report_map = {}

    instance_id = prediction[KEY_INSTANCE_ID]
    report_map[instance_id] = {
        "patch_is_None": False,
        "patch_exists": False,
        "patch_successfully_applied": False,
        "resolved": False,
    }

    # Check if the model patch exists
    if prediction[KEY_PREDICTION] is None:
        report_map[instance_id]["patch_is_None"] = True
        return report_map
    report_map[instance_id]["patch_exists"] = True

    # Get evaluation logs
    eval_status_map, found = get_logs_eval(test_spec, test_log_path)

    if not found:
        return report_map
    report_map[instance_id]["patch_successfully_applied"] = True

    eval_ref = {
        KEY_INSTANCE_ID: test_spec.instance_id,
        FAIL_TO_PASS: test_spec.FAIL_TO_PASS,
        PASS_TO_PASS: test_spec.PASS_TO_PASS,
    }

    report = get_eval_tests_report(
        eval_status_map, eval_ref, eval_type=get_eval_type(test_spec)
    )
    if get_resolution_status(report) == ResolvedStatus.FULL.value:
        report_map[instance_id]["resolved"] = True

    if include_tests_status:
        report_map[instance_id]["tests_status"] = report  # type: ignore

    return report_map