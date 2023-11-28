# Printing utils
import difflib

def print_diff(prev_lines, curr_lines, fromfile='prev', tofile='curr', lineterm=''):
    for line in difflib.unified_diff(prev_lines, curr_lines, fromfile=fromfile, tofile=tofile, lineterm=lineterm):
        if line.startswith('+'):
            print("\033[1;32m" + line + "\033[0m")
        elif line.startswith('-'):
            print("\033[1;31m" + line + "\033[0m")
        else:
            print(line)

def highlight_lines(lines, target_line_idxs, cursor_moved_to_new_line=False, color_code='31m', print_line_numbers=True):
    idx = 0
    for line in lines:
        if print_line_numbers:
            print(idx, "    ", end="")
        if idx in target_line_idxs:
            if cursor_moved_to_new_line:
                print(line.rstrip())
                idx += 1
                print(idx, "    ", end="")
                print("\033[1;"+ color_code + "|" + "\033[0m")
            else:
                print("\033[1;"+ color_code + line.rstrip() + "\033[0m")
        else:
            print(line.rstrip())
        idx += 1

def get_file_contents(filename):
    with open(filename, 'r') as f:
        return f.readlines()
    
def print_code(file_contents, print_line_numbers=True):
    for idx, line in enumerate(file_contents):
        if print_line_numbers:
            print(idx, "    ", line.rstrip())
        else:
            print(line.rstrip())