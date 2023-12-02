# Preprocessing utils
import difflib
import json


def get_diff_chunks(prev_file_lines, curr_file_lines, max_num_chunks=1):
    # get diff chunks
    diff_chunks = []
    diff = list(
        difflib.unified_diff(
            prev_file_lines, curr_file_lines, fromfile="v0", tofile="v1", lineterm=""
        )
    )[2:]
    for line in diff:
        # print(line)
        if line.startswith("@@"):
            if len(diff_chunks) >= max_num_chunks:
                break
            if len(diff_chunks) > 0:
                diff_chunks[-1]["before"] = diff_chunks[-1]["before"].strip("\n")
                diff_chunks[-1]["after"] = diff_chunks[-1]["after"].strip("\n")
            diff_chunks.append({"before": "", "after": ""})
        if line.startswith("-"):
            diff_chunks[-1]["before"] += line[1:]
        elif line.startswith("+"):
            diff_chunks[-1]["after"] += line[1:]

    if len(diff_chunks) > 0:
        diff_chunks[-1]["before"] = diff_chunks[-1]["before"].strip("\n")
        diff_chunks[-1]["after"] = diff_chunks[-1]["after"].strip("\n")
    return diff_chunks[:max_num_chunks]


def get_prefix(curr_file_lines, target_line_number, num_lines_in_context=3):
    start_line = max(0, target_line_number - num_lines_in_context)
    return "".join(curr_file_lines[start_line:target_line_number]).strip("\n")


def get_suffix(curr_file_lines, target_line_number, num_lines_in_context=3):
    end_line = min(len(curr_file_lines), target_line_number + num_lines_in_context + 1)
    return "".join(curr_file_lines[target_line_number + 1 : end_line]).strip("\n")


def save_processed_example_json(
    processed_example_content, processed_example_json_file_path, id
):
    processed_example_data = [{"id": id, "data": [processed_example_content]}]

    with open(processed_example_json_file_path, "w") as f:
        json.dump(processed_example_data, f, indent=4)

    print("Saved processed example to:", processed_example_json_file_path)
