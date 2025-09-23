import re
from swe_sharp_bench.constants import MAP_REPO_TO_INSTALL_CS, MAP_REPO_VERSION_TO_SPECS_CS, MAP_REPO_TO_EXT
from swebench.harness.constants import SWEbenchInstance, NON_TEST_EXTS
from swebench.harness.test_spec.javascript import (
    make_repo_script_list_js,
    make_env_script_list_js,
    make_eval_script_list_js,
)
from swebench.harness.test_spec.python import (
    make_repo_script_list_py,
    make_env_script_list_py,
    make_eval_script_list_py,
)

def make_repo_script_list_cs(
    specs, repo, repo_directory, base_commit, env_name
) -> list:
    setup_commands = [
        f"git clone -o origin https://github.com/{repo} {repo_directory}",
        f"cd {repo_directory}",
        f"git reset --hard {base_commit}",
        f"chmod -R 777 {repo_directory}",  # So nonroot user can run tests
        # Remove the remote so the agent won't see newer commits.
        "git remote remove origin",
    ]
    
    if repo in MAP_REPO_TO_INSTALL_CS:
        setup_commands.append(MAP_REPO_TO_INSTALL_CS[repo])
    
    if "pre_install" in specs:
        for pre_install_step in specs["pre_install"]:
            setup_commands.append(pre_install_step) 

    if "install" in specs:
        setup_commands.append(specs["install"])
    return setup_commands

def make_env_script_list_cs(instance, specs, env_name) -> list:
    repo = instance["repo"]
    version = instance.get("version", "0.1")
    # Extract pull_number from instance_id if not present
    if "pull_number" in instance:
        pull_number = instance["pull_number"]
    else:
        instance_id = instance.get("instance_id", "")
        pull_number = instance_id.split("-")[-1] if instance_id else "0"
    if not specs:
        specs = MAP_REPO_VERSION_TO_SPECS_CS[repo][version]
    
    spec_default_dotnet_version = specs.get("dotnet", {}).get("default", "")
    instance_dotnet_version = specs.get("dotnet", {}).get(str(pull_number), "")
    spec_dotnet_version = instance_dotnet_version if instance_dotnet_version else spec_default_dotnet_version
    
    commands = []
    commands.append("mkdir /agent")
    commands.append("mkdir /output")
    commands.append("mkdir /drop")
    
    # Download and install .NET SDK if version is specified
    if spec_dotnet_version:
        commands.append("# Download and install .NET SDK")
        commands.append("wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh")
        commands.append("chmod +x dotnet-install.sh")
        commands.append(f"./dotnet-install.sh --version {spec_dotnet_version} --install-dir /usr/share/dotnet")
        
        # Get major version to determine which runtimes to install
        major_version = spec_dotnet_version.split('.')[0]
        
        # Install common ASP.NET Core runtime versions for compatibility
        commands.append("# Install ASP.NET Core runtimes")
        commands.append("./dotnet-install.sh --runtime aspnetcore --channel 6.0 --install-dir /usr/share/dotnet")
        commands.append("./dotnet-install.sh --runtime aspnetcore --channel 7.0 --install-dir /usr/share/dotnet")
        
        # Install .NET runtime versions for compatibility
        commands.append("# Install .NET runtimes")
        commands.append("./dotnet-install.sh --runtime dotnet --channel 6.0 --install-dir /usr/share/dotnet")
        commands.append("./dotnet-install.sh --runtime dotnet --channel 7.0 --install-dir /usr/share/dotnet")
        
        # Create symlink for dotnet CLI
        commands.append("# Create symlink for dotnet CLI")
        commands.append("ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet")
        
        # Clean up installation script
        commands.append("rm dotnet-install.sh")
        
        # Set up environment variables
        commands.append("# Set up environment variables")
        commands.append(f"export DOTNET_SDK_VERSION='{spec_dotnet_version}'")
        commands.append("export PATH=/usr/share/dotnet:$PATH")
        commands.append("export DOTNET_ROOT=/usr/share/dotnet")
    
    return commands

def get_test_directives(instance: SWEbenchInstance) -> list:
    """
    Get test directives from the test_patch of a task instance

    Args:
        instance (dict): task instance
    Returns:
        directives (list): List of test directives
    """
    if instance["repo"] in ["chocolatey/choco", "dotnet/BenchmarkDotNet", "ardalis/CleanArchitecture", "AvaloniaUI__Avalonia"]:
        return []
    
    if 'dotnet' in instance:
        all_tests_to_run = instance.get("FAIL_TO_PASS", []) + instance.get("PASS_TO_PASS", [])
        filter_subset = "\"" + " | ".join([f"FullyQualifiedName~{test}" for test in all_tests_to_run]) + "\""
        return [filter_subset]

    if instance["repo"] == "swe-bench/humaneval":
        return ["test.py"]

    # Get test directives from test patch and remove non-test files
    diff_pat = r"diff --git a/.* b/(.*)"
    test_patch = instance["test_patch"]
    directives = re.findall(diff_pat, test_patch)
    directives = [
        d for d in directives if not any(d.endswith(ext) for ext in NON_TEST_EXTS)
    ]

    # For Django tests, remove extension + "tests/" prefix and convert slashes to dots (module referencing)
    if instance["repo"] == "django/django":
        directives_transformed = []
        for d in directives:
            d = d[: -len(".py")] if d.endswith(".py") else d
            d = d[len("tests/") :] if d.startswith("tests/") else d
            d = d.replace("/", ".")
            directives_transformed.append(d)
        directives = directives_transformed

    return directives

def make_eval_script_list_cs(instance, specs, env_name, repo_directory, base_commit, test_patch):
    """
    Applies the test patch and runs the tests.
    """
    HEREDOC_DELIMITER = "EOF_114329324912"
    DIFF_MODIFIED_FILE_REGEX = r"--- a/(.*)"
    
    test_files = re.findall(DIFF_MODIFIED_FILE_REGEX, test_patch)
    # Reset test files to the state they should be in before the patch.
    reset_tests_command = f"git checkout {base_commit} {' '.join(test_files)}"
    apply_test_patch_command = (
        f"git apply -v - <<'{HEREDOC_DELIMITER}'\n{test_patch}\n{HEREDOC_DELIMITER}"
    )
    
      # Add fallback if first attempt fails
    apply_test_patch_with_fallback = f"""
echo "First attempt to apply patch..."
{apply_test_patch_command}
if [ $? -ne 0 ]; then
    echo "First patch attempt failed. Retrying with --reject option to see detailed failures..."
    # If first attempt failed, try with --reject option to see what failed
    git apply --reject -v - <<'{HEREDOC_DELIMITER}'
{test_patch}
{HEREDOC_DELIMITER}
    echo "Second attempt completed. Check above output and .rej files for details."
fi
"""
    specs_test_command = specs.get("test_cmd", None)
    if not specs_test_command:
        specs_test_command = MAP_REPO_VERSION_TO_SPECS_CS[instance["repo"]][instance["version"]]["test_cmd"]

    test_command = " ".join(
        [
            specs_test_command,
            *get_test_directives(instance),
        ]
    )
    
    if instance["instance_id"] == "ardalis__cleanarchitecture-805":
        test_command = test_command.replace("./Clean.Architecture.sln", "./sample/NimblePros.SampleToDo.sln")
    
    
    eval_commands = [
        "source /opt/miniconda3/bin/activate",
        f"conda activate {env_name}",
        f"cd {repo_directory}",
    ]
    if "eval_commands" in specs:
        eval_commands += specs["eval_commands"]
    eval_commands += [
        f"git config --global --add safe.directory {repo_directory}",  # for nonroot user
        f"cd {repo_directory}",
        # This is just informational, so we have a record
        "git status",
        "git show",
        f"git diff {base_commit}",
        "source /opt/miniconda3/bin/activate",
        f"conda activate {env_name}",
    ]
    if "install" in specs:
        eval_commands.append(specs["install"])

    eval_commands += [
        reset_tests_command,
        apply_test_patch_with_fallback,
        test_command]
    
    if "dotnet" in specs:
        eval_commands.append("cat /testbed/test_results/* && rm -rf /testbed/test_results/")

    eval_commands += [reset_tests_command] 

    return eval_commands

def make_repo_script_list(specs, repo, repo_directory, base_commit, env_name) -> list:
    """
    Create a list of bash commands to set up the repository for testing.
    This is the setup script for the instance image.
    """
    ext = MAP_REPO_TO_EXT.get(repo, "cs")
    func = {
        "js": make_repo_script_list_js,
        "py": make_repo_script_list_py,
        "cs": make_repo_script_list_cs,
    }[ext]
    return func(specs, repo, repo_directory, base_commit, env_name)

def make_env_script_list(instance, specs, env_name) -> list:
    """
    Creates the list of commands to set up the environment for testing.
    This is the setup script for the environment image.
    """
    ext = MAP_REPO_TO_EXT.get(instance["repo"], "cs")
    func = {
        "js": make_env_script_list_js,
        "py": make_env_script_list_py,
        "cs": make_env_script_list_cs,
    }[ext]
    return func(instance, specs, env_name)

def make_eval_script_list(
    instance, specs, env_name, repo_directory, base_commit, test_patch
) -> list:
    """
    Applies the test patch and runs the tests.
    """
    ext = MAP_REPO_TO_EXT.get(instance["repo"], "cs")
    func = {
        "js": make_eval_script_list_js,
        "py": make_eval_script_list_py,
        "cs": make_eval_script_list_cs,
    }[ext]
    return func(instance, specs, env_name, repo_directory, base_commit, test_patch)