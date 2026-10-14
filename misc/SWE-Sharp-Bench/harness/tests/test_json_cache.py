import pickle
import tempfile
import unittest
from pathlib import Path

from swe_sharp_bench.json_cache import (
    is_dotnet_version_info,
    is_string,
    is_string_map,
    load_json_cache,
    write_json_cache,
)


class JsonCacheTests(unittest.TestCase):
    def test_round_trips_valid_cache_atomically(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            cache_file = Path(temporary_directory) / "cache.json"
            cache_data = {"repo_commit": {"repo.sln": "solution contents"}}

            self.assertTrue(write_json_cache(str(cache_file), cache_data))
            self.assertEqual(
                cache_data,
                load_json_cache(str(cache_file), is_string_map),
            )
            self.assertEqual(
                [],
                list(Path(temporary_directory).glob(".cache.json.*.tmp")),
            )

    def test_returns_empty_cache_for_malformed_json(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            cache_file = Path(temporary_directory) / "cache.json"
            cache_file.write_text("{not json", encoding="utf-8")

            self.assertEqual(
                {},
                load_json_cache(str(cache_file), is_string),
            )

    def test_returns_empty_cache_for_invalid_schema(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            cache_file = Path(temporary_directory) / "cache.json"
            cache_file.write_text('{"key": ["not", "a", "string"]}', encoding="utf-8")

            self.assertEqual(
                {},
                load_json_cache(str(cache_file), is_string),
            )

    def test_legacy_pickle_is_never_deserialized(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            cache_file = Path(temporary_directory) / "cache.json"
            legacy_file = Path(temporary_directory) / "cache.pkl"
            sentinel_file = Path(temporary_directory) / "pickle-executed"

            class MaliciousCache:
                def __reduce__(self):
                    payload = (
                        f"open({str(sentinel_file)!r}, 'w', "
                        "encoding='utf-8').write('executed')"
                    )
                    return exec, (payload,)

            with legacy_file.open("wb") as legacy_stream:
                pickle.dump(MaliciousCache(), legacy_stream)

            self.assertEqual(
                {},
                load_json_cache(str(cache_file), is_string),
            )
            self.assertFalse(sentinel_file.exists())

    def test_validates_dotnet_version_cache_shape(self):
        self.assertTrue(is_dotnet_version_info({}))
        self.assertTrue(
            is_dotnet_version_info(
                {
                    "dotnet_version": "8.0.100",
                    "source": "global.json (sdk.version)",
                }
            )
        )
        self.assertFalse(is_dotnet_version_info({"dotnet_version": 8}))
        self.assertFalse(is_dotnet_version_info({"raw_data": "not an object"}))


if __name__ == "__main__":
    unittest.main()
