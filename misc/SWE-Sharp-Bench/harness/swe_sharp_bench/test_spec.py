import json
import platform
import hashlib
import requests
import base64
import os
import pickle
from typing import Any, Union, cast
from dataclasses import dataclass

from swebench.harness.constants import SWEbenchInstance
from swebench.harness.constants import (
    DEFAULT_DOCKER_SPECS,
    KEY_INSTANCE_ID,
    LATEST,
    USE_X86,
)

from swe_sharp_bench.dockerfiles import (
    get_dockerfile_base,
    get_dockerfile_env,
    get_dockerfile_instance,
)
from swe_sharp_bench.create_scripts import (
    make_repo_script_list,
    make_env_script_list,
    make_eval_script_list,
)
from swe_sharp_bench.constants import (
    MAP_REPO_VERSION_TO_SPECS,
    MAP_REPO_TO_EXT,
)

@dataclass
class TestSpec:
    """
    A dataclass that represents a test specification for a single instance of SWE-bench.
    """

    instance_id: str
    repo: str
    version: str
    repo_script_list: list[str]
    eval_script_list: list[str]
    env_script_list: list[str]
    arch: str
    FAIL_TO_PASS: list[str]
    PASS_TO_PASS: list[str]
    language: str
    docker_specs: dict
    namespace: str
    base_image_tag: str = LATEST
    env_image_tag: str = LATEST
    instance_image_tag: str = LATEST

    @property
    def setup_env_script(self):
        return (
            "\n".join(["#!/bin/bash", "set -euxo pipefail"] + self.env_script_list)
            + "\n"
        )

    @property
    def eval_script(self):
        return (
            "\n".join(["#!/bin/bash", "set -uxo pipefail"] + self.eval_script_list)
            + "\n"
        )
        # Don't exit early because we need to revert tests at the end

    @property
    def install_repo_script(self):
        return (
            "\n".join(["#!/bin/bash", "set -euxo pipefail"] + self.repo_script_list)
            + "\n"
        )

    @property
    def base_image_key(self):
        ext = MAP_REPO_TO_EXT.get(self.repo, "cs")
        return (
            f"sweb.base.{ext}.{self.arch}:{self.base_image_tag}"
        )

    @property
    def env_image_key(self):
        """
        The key for the environment image is based on the hash of the environment script list.
        If the environment script list changes, the image will be rebuilt automatically.

        Note that old images are not automatically deleted, so consider cleaning up old images periodically.
        """
        hash_key = str(self.env_script_list)
        if self.docker_specs != {}:
            hash_key += str(self.docker_specs)
        hash_object = hashlib.sha256()
        hash_object.update(hash_key.encode("utf-8"))
        hash_value = hash_object.hexdigest()
        val = hash_value[:22]  # 22 characters is still very likely to be unique
        ext = MAP_REPO_TO_EXT.get(self.repo, "cs")
        return f"sweb.env.{ext}.{self.arch}.{val}:{self.env_image_tag}"

    @property
    def instance_image_key(self):
        key = f"sweb.eval.cs.{self.arch}.{self.instance_id.lower()}:{self.instance_image_tag}"
        if self.is_remote_image:
            key = f"{self.namespace}/{key}".replace("__", "_1776_")
        return key

    @property
    def is_remote_image(self):
        return self.namespace is not None

    def get_instance_container_name(self, run_id=None):
        if not run_id:
            return f"sweb.eval.{self.instance_id}"
        return f"sweb.eval.{self.instance_id.lower()}.{run_id}"

    @property
    def base_dockerfile(self):
        return get_dockerfile_base(
            self.platform,
            self.arch,
            self.language,
            **{**DEFAULT_DOCKER_SPECS, **self.docker_specs},
        )

    @property
    def env_dockerfile(self):
        return get_dockerfile_env(
            self.platform,
            self.arch,
            self.language,
            self.base_image_key,
            **{**DEFAULT_DOCKER_SPECS, **self.docker_specs},
        )

    @property
    def instance_dockerfile(self):
        return get_dockerfile_instance(self.platform, self.language, self.env_image_key)

    @property
    def platform(self):
        if self.arch == "x86_64":
            return "linux/x86_64"
        elif self.arch == "arm64":
            return "linux/arm64/v8"
        else:
            raise ValueError(f"Invalid architecture: {self.arch}")


def make_test_spec(
    instance: SWEbenchInstance,
    namespace: str = None,
    base_image_tag: str = LATEST,
    env_image_tag: str = LATEST,
    instance_image_tag: str = LATEST,
) -> TestSpec:
    if isinstance(instance, TestSpec):
        return instance
    assert base_image_tag is not None, "base_image_tag cannot be None"
    assert env_image_tag is not None, "env_image_tag cannot be None"
    assert instance_image_tag is not None, "instance_image_tag cannot be None"
    instance_id = instance[KEY_INSTANCE_ID]
    repo = instance["repo"]
    version = instance.get("version")
    base_commit = instance["base_commit"]
    problem_statement = instance.get("problem_statement")
    hints_text = instance.get("hints_text")  # Unused
    test_patch = instance["test_patch"]
    
    def _from_json_or_obj(key: str) -> Any:
        """If key points to string, load with json"""
        if key not in instance:
            # If P2P, F2P keys not found, it's a validation instance
            return []
        if isinstance(instance[key], str):
            return json.loads(instance[key])
        return instance[key]
    
    pass_to_pass = _from_json_or_obj("PASS_TO_PASS")
    fail_to_pass = _from_json_or_obj("FAIL_TO_PASS")

    env_name = "testbed"
    repo_directory = f"/{env_name}"
    
    specs = MAP_REPO_VERSION_TO_SPECS.get(repo, {}).get(version, {})
    if not specs:
        sln_files = get_sln_file_from_github(repo, base_commit)
        main_sln_file = find_main_solution_file(sln_files, full_repo_name=repo)
        
        print(f"Main solution file: {main_sln_file}")
        if not main_sln_file:
            raise ValueError(f"Could not find main solution file for {repo}@{base_commit}")
        
        dotnet_version = None
        version_info = find_dotnet_version(repo, base_commit)
        print(f"Version info: {version_info}")
        if version_info:
            dotnet_version = version_info.get("dotnet_version")
        else:
            framework_info = find_dotnet_framework(
                sln_content=sln_files[main_sln_file],
                repo=repo,
                commit_id=base_commit
            )
            dotnet_version = framework_info.get("recommended_dotnet_version")
        
        if not dotnet_version:
            dotnet_version = "8.0.100"
        
        if repo == "microsoft/garnet":
            dotnet_version = "8.0.100"
        

        
        specs = {
            "python": "3.12",
            "test_cmd": f"dotnet test {main_sln_file} --verbosity detailed --results-directory ./test_results --logger trx --filter",
            "pre_install": [
                "dotnet workload list | grep -v \"^---\" | grep -v \"^Workload ID\" | grep -v \"No workloads\" | grep -q . && dotnet workload restore || echo \"No workloads to restore\"",
                f"dotnet restore {main_sln_file}"
            ],
            "dotnet": {
                "default": dotnet_version,
            },
            "install": f"dotnet build {main_sln_file}"
        }
        
    docker_specs = specs.get("docker_specs", {})
    
    repo_script_list = make_repo_script_list(
        specs, repo, repo_directory, base_commit, env_name
    )
    env_script_list = make_env_script_list(instance, specs, env_name)
    eval_script_list = make_eval_script_list(
        instance, specs, env_name, repo_directory, base_commit, test_patch
    )
    
    if platform.machine() in {"aarch64", "arm64"}:
        # use arm64 unless explicitly specified
        arch = "arm64" if instance_id not in USE_X86 else "x86_64"
    else:
        arch = "x86_64"
    
    return TestSpec(
        instance_id=instance_id,
        repo=repo,
        env_script_list=env_script_list,
        repo_script_list=repo_script_list,
        eval_script_list=eval_script_list,
        version=version,
        arch=arch,
        FAIL_TO_PASS=fail_to_pass,
        PASS_TO_PASS=pass_to_pass,
        language=MAP_REPO_TO_EXT.get(repo, "cs"),
        docker_specs=docker_specs,
        namespace=namespace,
        base_image_tag=base_image_tag,
        env_image_tag=env_image_tag,
        instance_image_tag=instance_image_tag,
    )


def get_test_specs_from_dataset(
    dataset: Union[list[SWEbenchInstance], list[TestSpec]],
    namespace: str = None,
    instance_image_tag: str = LATEST,
) -> list[TestSpec]:
    """
    Idempotent function that converts a list of SWEbenchInstance objects to a list of TestSpec objects.
    """
    if isinstance(dataset[0], TestSpec):
        return cast(list[TestSpec], dataset)
    return list(
        map(
            lambda x: make_test_spec(x, namespace, instance_image_tag),
            cast(list[SWEbenchInstance], dataset),
        )
    )

def create_test_spec_from_metadata(
    metadata: dict
) -> TestSpec:
    instance = metadata
    instance_id = instance.get("instance_id")
    repo = instance.get("repo")
    version = instance.get("version")
    env_script = instance.get("setup_env_script")
    eval_script = instance.get("eval_script")
    install_repo_script = instance.get("install_repo_script")
    arch = instance.get("arch", "x86_64")
    fail_to_pass = instance.get("FAIL_TO_PASS")
    pass_to_pass = instance.get("PASS_TO_PASS")
    language = "cs"
    docker_specs = {}
    namespace = ""
    base_image_tag = instance.get("base_image_tag", "latest")
    env_image_tag = instance.get("env_image_tag", "latest")
    instance_image_tag = instance.get("instance_image_tag", "latest")
    return TestSpec(
        instance_id=instance_id,
        repo=repo,
        version=version,
        repo_script_list=[install_repo_script],
        eval_script_list=[eval_script],
        env_script_list=[env_script],
        arch=arch,
        FAIL_TO_PASS=fail_to_pass,
        PASS_TO_PASS=pass_to_pass,
        language=language,
        docker_specs=docker_specs,
        namespace=namespace,
        base_image_tag=base_image_tag,
        env_image_tag=env_image_tag,
        instance_image_tag=instance_image_tag,
    )
    
def get_sln_file_from_github(
    repo: str,
    commit: str
): 
    
    # Define cache file path
    cache_file = ".sln_file_cache.pkl"
    cache_key = f"{repo}_{commit}"
    cache_data = {}
    
    # Load cache if it exists
    if os.path.exists(cache_file):
        try:
            with open(cache_file, "rb") as f:
                cache_data = pickle.load(f)
            
            # Return cached data if available
            if cache_key in cache_data:
                print(f"Using cached solution files for {repo}@{commit}")
                return cache_data[cache_key]
        except Exception as e:
            print(f"Error loading cache: {e}")
            # Continue with API call if cache loading fails
    
    api_base = "https://api.github.com/repos"
    sln_files = {}
    
    # First check root directory
    content_url = f"{api_base}/{repo}/contents?ref={commit}"
    response = requests.get(content_url)
    if response.status_code != 200:
        raise ValueError(f"Failed to fetch content from {content_url}: {response.status_code}")
    
    content = response.json()
    
    # Check for src directory
    src_path = None
    for item in content:
        if item["type"] == "file" and item["name"].endswith(".sln"):
            file_content_url = item["url"]
            file_response = requests.get(file_content_url)
            
            if file_response.status_code == 200:
                file_data = file_response.json()
                if file_data.get("encoding") == "base64":
                    file_content = base64.b64decode(file_data["content"]).decode("utf-8")
                    sln_files[item["path"]] = file_content
        elif item["type"] == "dir" and item["name"] == "src":
            src_path = item["path"]
    
    # If no .sln files found in root and src directory exists, check there
    if not sln_files and src_path:
        src_url = f"{api_base}/{repo}/contents/{src_path}?ref={commit}"
        src_response = requests.get(src_url)
        
        if src_response.status_code == 200:
            src_content = src_response.json()
            
            for item in src_content:
                if item["type"] == "file" and item["name"].endswith(".sln"):
                    file_content_url = item["url"]
                    file_response = requests.get(file_content_url)
                    
                    if file_response.status_code == 200:
                        file_data = file_response.json()
                        if file_data.get("encoding") == "base64":
                            file_content = base64.b64decode(file_data["content"]).decode("utf-8")
                            sln_files[item["path"]] = file_content
    
    print(f"Found {len(sln_files)} solution files in {repo}@{commit}")
    # Save to cache
    cache_data[cache_key] = sln_files
    try:
        with open(cache_file, "wb") as f:
            pickle.dump(cache_data, f)
        print(f"Cached solution files for {repo}@{commit}")
    except Exception as e:
        print(f"Error saving to cache: {e}")
    
    return sln_files

def find_main_solution_file(sln_files, full_repo_name=None):
    """
    Identifies the likely main solution file from a list of .sln files.
    
    Args:
        sln_files (dict or list): Dictionary with file paths as keys, or a list of file paths
        full_repo_name (str, optional): The full repository name in format "owner/repo", used as a hint
        
    Returns:
        str: The path to the likely main solution file
    """
    # If given a dictionary, extract just the keys (file paths)
    if isinstance(sln_files, dict):
        file_paths = list(sln_files.keys())
    else:
        file_paths = sln_files
    
    if not file_paths:
        return None
    
    # Extract repo_name from full_repo_name if provided
    repo_name = None
    repo_owner = None
    if full_repo_name:
        parts = full_repo_name.split('/')
        if len(parts) >= 2:
            repo_owner = parts[0]
            repo_name = parts[1]
    
    # Priority scoring system
    scores = {}
    
    for path in file_paths:
        file_name = path.split('/')[-1]
        base_name = file_name.replace('.sln', '')
        
        # Initialize score
        scores[path] = 0
        
        # Heuristic 1: Exact match with repo name is highest priority
        if repo_name and repo_name.lower() in base_name.lower():
            scores[path] += 10
            # Extra points for exact match
            if base_name.lower() == repo_name.lower():
                scores[path] += 5
        
        # Additional check for organization/owner name if available
        if repo_owner and repo_owner.lower() in base_name.lower():
            scores[path] += 3
        
        # Heuristic 2: Simpler names are often the main solution
        # (fewer dots/separators usually means main project)
        separators = base_name.count('.') + base_name.count('-') + base_name.count('_')
        scores[path] -= separators
        
        # Heuristic 3: Solutions containing "dev", "vscode", "test", etc. 
        # are often specialized versions
        lower_base = base_name.lower()
        special_terms = ['dev', 'vscode', 'test', 'sample', 'demo', 'legacy', 'gallery']
        for term in special_terms:
            if term in lower_base:
                scores[path] -= 5
                break
        
        # Heuristic 4: Solutions in the root directory are more likely to be main
        depth = path.count('/')
        scores[path] -= depth
        
        # Heuristic 5: Shortest name is often the main one (after accounting for repo name)
        scores[path] -= len(base_name) * 0.1
    
    # Find the file path with the highest score
    main_solution = max(scores.items(), key=lambda x: x[1])[0]
    
    return main_solution

def find_dotnet_framework(sln_content, repo, commit_id):
    """
    Extracts the target .NET framework version and recommends a .NET SDK version.
    
    Args:
        sln_content (str): Content of the solution file
        repo (str): Repository in format 'owner/name' (e.g., 'dotnet/maui')
        commit_id (str): The specific commit hash
        
    Returns:
        dict: Information about detected framework versions and recommended SDK version
    """
    import re
    import requests
    import base64
    from collections import Counter
    
    # Split repo into owner and name
    repo_parts = repo.split('/')
    if len(repo_parts) != 2:
        return {"error": "Invalid repository format. Expected 'owner/name'"}
    
    repo_owner, repo_name = repo_parts
    
    # Extract project file paths from the solution
    project_pattern = r'Project\("\{[A-F0-9\-]+\}"\)\s*=\s*"[^"]+",\s*"([^"]+)"'
    project_matches = re.findall(project_pattern, sln_content)
    
    # Only process .csproj, .vbproj, or .fsproj files
    project_files = [p.replace('\\', '/') for p in project_matches 
                    if p.endswith(('.csproj', '.vbproj', '.fsproj'))]
    
    if not project_files:
        return {"error": "No project files found"}
    
    # Limit to a reasonable number of projects to analyze
    sample_projects = project_files[:5]  # Analyze up to 5 projects
    
    framework_versions = []
    sdk_references = []
    
    # Function to fetch file content from GitHub
    def fetch_file_from_github(file_path):
        
        # Define cache file path
        cache_file = ".github_file_cache.pkl"
        cache_key = f"{repo_owner}/{repo_name}_{commit_id}_{file_path}"
        cache_data = {}
        
        # Load cache if it exists
        if os.path.exists(cache_file):
            try:
                with open(cache_file, "rb") as f:
                    cache_data = pickle.load(f)
                
                # Return cached data if available
                if cache_key in cache_data:
                    print(f"Using cached file content for {file_path}")
                    return cache_data[cache_key]
            except Exception as e:
                print(f"Error loading cache: {e}")
                # Continue with API call if cache loading fails
        
        # Fetch from GitHub if not in cache
        api_url = f"https://api.github.com/repos/{repo_owner}/{repo_name}/contents/{file_path}?ref={commit_id}"
        response = requests.get(api_url)
        
        if response.status_code == 200:
            content = response.json()
            if content.get("encoding") == "base64":
                file_content = base64.b64decode(content["content"]).decode("utf-8")
                
                # Save to cache
                cache_data[cache_key] = file_content
                try:
                    with open(cache_file, "wb") as f:
                        pickle.dump(cache_data, f)
                    print(f"Cached file content for {file_path}")
                except Exception as e:
                    print(f"Error saving to cache: {e}")
                
                return file_content
        return None
    
    # Process each sample project
    for project_path in sample_projects:
        project_content = fetch_file_from_github(project_path)
        
        if not project_content:
            continue
        
        # Check for SDK reference
        sdk_match = re.search(r'<Project\s+Sdk="([^"]+)"', project_content)
        if sdk_match:
            sdk_references.append(sdk_match.group(1))
        
        # Check for TargetFramework (single framework)
        target_framework_match = re.search(r'<TargetFramework>([^<]+)</TargetFramework>', project_content)
        if target_framework_match:
            framework_versions.append(target_framework_match.group(1))
            continue
        
        # Check for TargetFrameworks (multiple frameworks)
        target_frameworks_match = re.search(r'<TargetFrameworks>([^<]+)</TargetFrameworks>', project_content)
        if target_frameworks_match:
            frameworks = target_frameworks_match.group(1).split(';')
            framework_versions.extend(frameworks)
            continue
        
        # Check for .NET Framework version
        framework_version_match = re.search(r'<TargetFrameworkVersion>([^<]+)</TargetFrameworkVersion>', project_content)
        if framework_version_match:
            framework_versions.append(f"net{framework_version_match.group(1).replace('v', '')}")
            continue
        
        # Check for LangVersion as a fallback
        lang_version_match = re.search(r'<LangVersion>([^<]+)</LangVersion>', project_content)
        if lang_version_match:
            lang_version = lang_version_match.group(1)
            if lang_version == "10.0" or lang_version == "10":
                # C# 10 corresponds to .NET 6
                framework_versions.append("net6.0")
            elif lang_version == "11.0" or lang_version == "11":
                # C# 11 corresponds to .NET 7
                framework_versions.append("net7.0")
            elif lang_version == "12.0" or lang_version == "12":
                # C# 12 corresponds to .NET 8
                framework_versions.append("net8.0")
    
    if not framework_versions and not sdk_references:
        return {"error": "Could not determine framework version"}
    
    # Count occurrences of each framework version
    framework_counts = Counter(framework_versions)
    most_common_framework = framework_counts.most_common(1)[0][0] if framework_counts else None
    
    # Determine the recommended .NET SDK version
    sdk_version = determine_sdk_version(framework_counts, sdk_references)
    
    return {
        "most_common_framework": most_common_framework,
        "all_frameworks": dict(framework_counts),
        "sdk_references": sdk_references,
        "recommended_dotnet_version": sdk_version
    }

def determine_sdk_version(framework_counts, sdk_references):
    """
    Determines the required .NET SDK version based on the target frameworks.
    
    Args:
        framework_counts (Counter): Counter of framework versions
        sdk_references (list): List of SDK references found in projects
        
    Returns:
        str: Recommended .NET SDK version
    """
    # Framework to SDK version mapping
    sdk_mapping = {
        "netcoreapp1.0": "1.0.0",
        "netcoreapp1.1": "1.1.0",
        "netcoreapp2.0": "2.0.0",
        "netcoreapp2.1": "2.1.0",
        "netcoreapp2.2": "2.2.0",
        "netcoreapp3.0": "3.0.100",
        "netcoreapp3.1": "3.1.100",
        "net5.0": "5.0.100",
        "net6.0": "6.0.100",
        "net7.0": "7.0.100",
        "net8.0": "8.0.100",
        "net9.0": "9.0.100",
        # Add common mappings for netstandard
        "netstandard1.0": "1.0.0",
        "netstandard1.1": "1.1.0",
        "netstandard1.2": "1.0.0",
        "netstandard1.3": "1.0.0",
        "netstandard1.4": "1.0.0",
        "netstandard1.5": "1.0.0",
        "netstandard1.6": "1.0.0",
        "netstandard2.0": "2.0.0",
        "netstandard2.1": "3.0.100",  # .NET Standard 2.1 is supported from .NET Core 3.0
    }
    
    # Find the highest SDK version required based on actual framework versions
    highest_version = None
    for framework in framework_counts:
        # Skip variable/placeholder frameworks
        if framework.startswith('$(') or framework.startswith('$(_'):
            continue
            
        # Check if framework has a direct mapping
        if framework in sdk_mapping:
            sdk_ver = sdk_mapping[framework]
            if highest_version is None or parse_version(sdk_ver) > parse_version(highest_version):
                highest_version = sdk_ver
        # Check if it's a framework with platform suffix (like net6.0-windows)
        else:
            for prefix in sdk_mapping:
                if framework.startswith(prefix + "-"):
                    sdk_ver = sdk_mapping[prefix]
                    if highest_version is None or parse_version(sdk_ver) > parse_version(highest_version):
                        highest_version = sdk_ver
                    break
    
    # If we couldn't determine a version from frameworks, try to infer from SDK references
    if highest_version is None and sdk_references:
        for sdk in sdk_references:
            if "Microsoft.NET.Sdk" in sdk:
                # If using .NET SDK but version couldn't be determined, default to latest LTS
                return "6.0.100"  # .NET 6 is LTS
    
    # If we still couldn't determine a version, make a reasonable guess
    if highest_version is None:
        # If netstandard2.0 is all we have, suggest .NET Core 3.1 (last LTS before .NET 5+)
        if any(fw.startswith("netstandard2.0") for fw in framework_counts):
            return "3.1.100"
        # Otherwise default to .NET 6 (LTS)
        return "6.0.100"
    
    return highest_version

def parse_version(version_string):
    """
    Parse a version string into a tuple for comparison.
    
    Args:
        version_string (str): Version string like "6.0.100"
        
    Returns:
        tuple: Tuple of integers for comparison
    """
    return tuple(map(int, version_string.split('.')))

def find_dotnet_version(repo, commit_id):
    """
    Determines the required .NET SDK version for a repository by checking global.json.
    Handles multiple global.json formats.
    
    Args:
        repo (str): Repository in format 'owner/name' (e.g., 'dotnet/maui')
        commit_id (str): The specific commit hash
        
    Returns:
        dict: Information about the required .NET SDK version or empty dict if not found
    """
    import requests
    import json
    import base64
    import os
    import pickle
    
    # Define cache file path
    cache_file = ".dotnet_version_cache.pkl"
    cache_key = f"{repo}_{commit_id}"
    cache_data = {}
    
    # Load cache if it exists
    if os.path.exists(cache_file):
        try:
            with open(cache_file, "rb") as f:
                cache_data = pickle.load(f)
            
            # Return cached data if available
            if cache_key in cache_data:
                print(f"Using cached .NET version info for {repo}@{commit_id}")
                return cache_data[cache_key]
        except Exception as e:
            print(f"Error loading cache: {e}")
            # Continue with API call if cache loading fails
    
    # Split repo into owner and name
    repo_parts = repo.split('/')
    if len(repo_parts) != 2:
        return {}
    
    repo_owner, repo_name = repo_parts
    
    # Check for global.json
    api_url = f"https://api.github.com/repos/{repo_owner}/{repo_name}/contents/global.json?ref={commit_id}"
    response = requests.get(api_url)
    
    result = {}
    if response.status_code == 200:
        # global.json exists, parse it
        content = response.json()
        if content.get("encoding") == "base64":
            global_json_content = base64.b64decode(content["content"]).decode("utf-8")
            try:
                global_json = json.loads(global_json_content)
                
                # Check for standard format: sdk.version
                if "sdk" in global_json and "version" in global_json["sdk"]:
                    result = {
                        "dotnet_version": global_json["sdk"]["version"],
                        "source": "global.json (sdk.version)"
                    }
                
                # Check for alternative format: tools.dotnet
                elif "tools" in global_json and "dotnet" in global_json["tools"]:
                    result = {
                        "dotnet_version": global_json["tools"]["dotnet"],
                        "source": "global.json (tools.dotnet)"
                    }
                
                # Return the raw data if we couldn't find a version in a known location
                else:
                    result = {
                        "dotnet_version": None,
                        "source": "global.json (format not recognized)",
                        "raw_data": global_json
                    }
                
            except json.JSONDecodeError:
                result = {}
    
    # Save to cache
    cache_data[cache_key] = result
    try:
        with open(cache_file, "wb") as f:
            pickle.dump(cache_data, f)
        print(f"Cached .NET version info for {repo}@{commit_id}")
    except Exception as e:
        print(f"Error saving to cache: {e}")
    
    return result