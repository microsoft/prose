import json
import os
import tempfile
from collections.abc import Callable
from typing import Any


CacheValueValidator = Callable[[Any], bool]


def load_json_cache(
    cache_file: str,
    value_validator: CacheValueValidator,
) -> dict[str, Any]:
    if not os.path.exists(cache_file):
        return {}

    try:
        with open(cache_file, encoding="utf-8") as cache_stream:
            cache_data = json.load(cache_stream)
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        print(f"Error loading cache {cache_file}: {error}")
        return {}

    if not isinstance(cache_data, dict) or not all(
        isinstance(key, str) and value_validator(value)
        for key, value in cache_data.items()
    ):
        print(f"Error loading cache {cache_file}: invalid cache schema")
        return {}

    return cache_data


def write_json_cache(cache_file: str, cache_data: dict[str, Any]) -> bool:
    cache_directory = os.path.dirname(os.path.abspath(cache_file))
    cache_name = os.path.basename(cache_file)
    temporary_file = None

    try:
        with tempfile.NamedTemporaryFile(
            mode="w",
            encoding="utf-8",
            dir=cache_directory,
            prefix=f".{cache_name}.",
            suffix=".tmp",
            delete=False,
        ) as cache_stream:
            temporary_file = cache_stream.name
            json.dump(cache_data, cache_stream, sort_keys=True)
            cache_stream.write("\n")
            cache_stream.flush()
            os.fsync(cache_stream.fileno())

        os.replace(temporary_file, cache_file)
        return True
    except (OSError, TypeError, ValueError) as error:
        print(f"Error saving cache {cache_file}: {error}")
        if temporary_file is not None:
            try:
                os.unlink(temporary_file)
            except FileNotFoundError:
                pass
            except OSError as cleanup_error:
                print(
                    f"Error cleaning temporary cache file "
                    f"{temporary_file}: {cleanup_error}"
                )
        return False


def is_string(value: Any) -> bool:
    return isinstance(value, str)


def is_string_map(value: Any) -> bool:
    return isinstance(value, dict) and all(
        isinstance(key, str) and isinstance(item, str)
        for key, item in value.items()
    )


def is_dotnet_version_info(value: Any) -> bool:
    if not isinstance(value, dict):
        return False

    dotnet_version = value.get("dotnet_version")
    source = value.get("source")
    raw_data = value.get("raw_data")

    return (
        (dotnet_version is None or isinstance(dotnet_version, str))
        and (source is None or isinstance(source, str))
        and (raw_data is None or isinstance(raw_data, dict))
    )
