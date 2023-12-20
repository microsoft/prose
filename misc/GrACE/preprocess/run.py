import json
import os
from argparse import ArgumentParser
from utils import processSession


def fetch_file_contents(filename):
    with open(filename, encoding="utf8") as f:
        lines = f.readlines()
    return lines


def process_data(root, repo, collect_unfiltered=False):
    file_suffix = "" if collect_unfiltered else "_filtered"

    # Fetch context edits
    before_ctx_before = fetch_file_contents(
        os.path.join(root, repo, repo + ".before_ctx_before" + file_suffix)
    )
    before_ctx_after = fetch_file_contents(
        os.path.join(root, repo, repo + ".before_ctx_after" + file_suffix)
    )
    after_ctx_before = fetch_file_contents(
        os.path.join(root, repo, repo + ".after_ctx_before" + file_suffix)
    )
    after_ctx_after = fetch_file_contents(
        os.path.join(root, repo, repo + ".after_ctx_after" + file_suffix)
    )

    # Fetch program edits
    program_before_filename = (
        "before.txt" if collect_unfiltered else repo + ".before_filtered"
    )
    program_after_filename = (
        "after.txt" if collect_unfiltered else repo + ".after_filtered"
    )
    program_before = fetch_file_contents(
        os.path.join(root, repo, program_before_filename)
    )
    program_after = fetch_file_contents(
        os.path.join(root, repo, program_after_filename)
    )

    num_examples = max(
        len(program_before),
        len(program_after),
        len(before_ctx_before),
        len(before_ctx_after),
        len(after_ctx_before),
        len(after_ctx_after),
    )

    # Use empty strings to denote a full-line delete/insert edit.
    for l in (
        program_before,
        program_after,
        before_ctx_before,
        before_ctx_after,
        after_ctx_before,
        after_ctx_after,
    ):
        if len(l) != num_examples:
            l.extend(["" for _ in range(num_examples - len(l))])

    print(f"Found {num_examples} for {repo}")

    data_list = [
        {
            "id": f"{repo}.{i}",
            "edits": [
                [before_ctx_before[i], before_ctx_after[i]],
                [program_before[i], program_after[i]],
                [after_ctx_before[i], after_ctx_after[i]],
            ],
        }
        for i in range(num_examples)
    ]
    return data_list


def process_and_store_data(input_root, output_root, collect_unfiltered=False):
    os.makedirs(output_root, exist_ok=True)

    repos = [
        dirname
        for dirname in os.listdir(input_root)
        if os.path.isdir(os.path.join(input_root, dirname))
    ]
    print(f"Found {len(repos)} repos")

    for repo in repos:
        print(f"Generating data for {repo}")
        # Generate json data
        data_list = processSession(
            process_data(input_root, repo, collect_unfiltered=collect_unfiltered)
        )
        json_file = os.path.join(output_root, repo + ".json")

        json.dump(data_list, open(json_file, "w"))

        print(f"Updated {json_file}\n")


if __name__ == "__main__":
    parser = ArgumentParser(description="Preprocess C3PO data for GrACE.")
    parser.add_argument(
        "--input_dir",
        type=str,
        help="Directory containing the input C3PO data. This directory should contain subdirectories, each containing data from one repo.",
    )
    parser.add_argument(
        "--output_dir",
        type=str,
        help="Output Directory to dump the processed data in json format.",
    )
    parser.add_argument(
        "--unfiltered",
        action="store_true",
        help="Use this flag to collect unfiltered samples from C3PO. Default is to only collect filtered samples.",
    )
    args = parser.parse_args()

    input_root = args.input_dir
    output_root = args.output_dir
    collect_unfiltered = args.unfiltered
    process_and_store_data(
        input_root=input_root,
        output_root=output_root,
        collect_unfiltered=collect_unfiltered,
    )
