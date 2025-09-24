from swebench.harness.dockerfiles.javascript import (
    _DOCKERFILE_BASE_JS,
    _DOCKERFILE_ENV_JS,
    _DOCKERFILE_INSTANCE_JS,
)

from swebench.harness.dockerfiles.python import (
    _DOCKERFILE_BASE_PY,
    _DOCKERFILE_ENV_PY,
    _DOCKERFILE_INSTANCE_PY,
)

from swe_sharp_bench.dockerfiles.csharp import (
    _DOCKERFILE_BASE_CS,
    _DOCKERFILE_ENV_CS,
    _DOCKERFILE_INSTANCE_CS,
)

_DOCKERFILE_BASE = {
    "py": _DOCKERFILE_BASE_PY,
    "js": _DOCKERFILE_BASE_JS,
    "cs": _DOCKERFILE_BASE_CS,
}

_DOCKERFILE_ENV = {
    "py": _DOCKERFILE_ENV_PY,
    "js": _DOCKERFILE_ENV_JS,
    "cs": _DOCKERFILE_ENV_CS,
}

_DOCKERFILE_INSTANCE = {
    "py": _DOCKERFILE_INSTANCE_PY,
    "js": _DOCKERFILE_INSTANCE_JS,
    "cs": _DOCKERFILE_INSTANCE_CS,
}


def get_dockerfile_base(platform, arch, language, **kwargs):
    if arch == "arm64":
        conda_arch = "aarch64"
    else:
        conda_arch = arch
    
    # Set default conda_version if not provided
    if "conda_version" not in kwargs:
        kwargs["conda_version"] = "py312_24.7.1-0"
    
    return _DOCKERFILE_BASE[language].format(
        platform=platform, conda_arch=conda_arch, **kwargs
    )


def get_dockerfile_env(platform, arch, language, base_image_key, **kwargs):
    return _DOCKERFILE_ENV[language].format(
        platform=platform,
        arch=arch,
        base_image_key=base_image_key,
        **kwargs,
    )


def get_dockerfile_instance(platform, language, env_image_name):
    return _DOCKERFILE_INSTANCE[language].format(
        platform=platform, env_image_name=env_image_name
    )
