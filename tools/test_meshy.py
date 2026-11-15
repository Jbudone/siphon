"""Offline tests: python3 -B -m unittest discover -s tools -p 'test_meshy.py' -v."""

import io
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import meshy


class MeshyTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.body = {"mode": "preview", "prompt": "Original test prop", "target_formats": ["fbx"]}
        env = patch.dict(os.environ, {"MESHY_API_KEY": "offline-test-key"})
        env.start()
        self.addCleanup(env.stop)
        # Every test must explicitly mock the network layer it intends to use.
        network = patch.object(meshy.request, "build_opener", side_effect=AssertionError("Real network forbidden"))
        network.start()
        self.addCleanup(network.stop)

    def test_submission_journal_precedes_post_and_persists_task_id(self):
        def post(method, endpoint, body):
            self.assertEqual((method, endpoint, body), ("POST", meshy.TASKS, self.body))
            journal = next((self.root / "requests").glob("*/submission.json"))
            record = json.loads(journal.read_text())
            self.assertEqual(record["state"], "outcome_unknown")
            self.assertEqual(record["request"], self.body)
            self.assertNotIn("offline-test-key", journal.read_text())
            return {"result": "task-123"}

        with patch.object(meshy, "api", side_effect=post) as network:
            first = meshy.submit(self.body, self.root)
            # Ordering in JSON doesn't turn the same paid request into a new one.
            second = meshy.submit(dict(reversed(list(self.body.items()))), self.root)
        self.assertEqual(network.call_count, 1)
        self.assertFalse(first["reused"])
        self.assertTrue(second["reused"])
        self.assertEqual(first["task_id"], second["task_id"])
        record = json.loads(next((self.root / "requests").glob("*/submission.json")).read_text())
        self.assertEqual((record["task_id"], record["state"]), ("task-123", "submitted"))

    def test_uncertain_post_blocks_repeat(self):
        with patch.object(meshy, "api", side_effect=ValueError("Lost response")) as network:
            with self.assertRaisesRegex(ValueError, "Lost response"):
                meshy.submit(self.body, self.root)
            with self.assertRaisesRegex(ValueError, "already reserved"):
                meshy.submit(self.body, self.root)
        self.assertEqual(network.call_count, 1)

    def test_dry_run_without_key_has_no_network_or_writes(self):
        with patch.dict(os.environ, {"MESHY_API_KEY": ""}), patch.object(meshy, "api", side_effect=AssertionError("No API calls")):
            staging = self.root / "not-created"
            result = meshy.submit(self.body, staging, dry_run=True)
        self.assertEqual(result["network_calls"], 0)
        self.assertFalse(staging.exists())

    def test_invalid_keys_do_not_reserve_or_leak(self):
        for key in ("", " ", " secret", "secret\ninvalid"):
            with self.subTest(key_length=len(key)), patch.dict(os.environ, {"MESHY_API_KEY": key}):
                with self.assertRaises(ValueError) as caught:
                    meshy.submit(self.body, self.root)
                self.assertNotIn("secret", str(caught.exception))
        self.assertFalse((self.root / "requests").exists())

    def test_reject_unsafe_ids_modes_and_staging(self):
        for value in ("../escape", "a/b", "a?x=y", "REPLACE_WITH_ACCEPTED_PREVIEW_TASK_ID", ""):
            with self.subTest(value=value), self.assertRaises(ValueError):
                meshy.identifier(value)
        for invalid in ({"mode": "delete"}, dict(self.body, preview_task_id="task-123")):
            with self.assertRaises(ValueError):
                meshy.validate(invalid)
        with self.assertRaises(ValueError):
            meshy.staging_path(self.root / "Assets" / "meshy")
        (self.root / "Assets").mkdir()
        (self.root / "alias").symlink_to(self.root / "Assets", target_is_directory=True)
        with self.assertRaises(ValueError):
            meshy.staging_path(self.root / "alias" / "meshy")
        self.assertEqual(meshy.DEFAULT_STAGING, Path(meshy.__file__).resolve().parent.parent / "artifacts/meshy")

    def task(self):
        return {"id": "task-123", "status": "SUCCEEDED", "consumed_credits": 20,
                "model_urls": {"fbx": "https://assets.meshy.ai/model.fbx?secret=x"},
                "thumbnail_url": "https://assets.meshy.ai/preview.png?secret=x",
                "texture_urls": [{"base_color": "https://assets.meshy.ai/texture.png?secret=x"}]}

    def test_completed_download_provenance(self):
        with patch.object(meshy, "api", return_value={"result": "task-123"}):
            meshy.submit(self.body, self.root)
        calls = []

        class Opener:
            def open(self, url, timeout):
                # A URL string carries no API Authorization header.
                self_test.assertIsInstance(url, str)
                self_test.assertTrue(url.startswith("https://"))
                calls.append(url)
                return io.BytesIO(b"generated-model-test-bytes")

        self_test = self
        with patch.object(meshy, "api", return_value=self.task()), patch.object(meshy.request, "build_opener", return_value=Opener()):
            result = meshy.download("task-123", self.root)
        self.assertEqual((result["files"], len(calls)), (3, 3))
        destination = self.root / "downloads/task-123"
        self.assertEqual({p.name for p in destination.iterdir()},
                         {"model.fbx", "thumbnail.png", "texture_0_base_color.png", "provenance.json"})
        provenance_text = (destination / "provenance.json").read_text()
        provenance = json.loads(provenance_text)
        self.assertEqual(provenance["submission"]["request"], self.body)
        self.assertEqual(provenance["task"]["consumed_credits"], 20)
        self.assertNotIn("offline-test-key", provenance_text)
        for file in provenance["files"]:
            self.assertEqual(len(file["sha256"]), 64)
            self.assertEqual(file["bytes"], len(b"generated-model-test-bytes"))
            self.assertNotIn("?secret=", file["source"])
            self.assertTrue((destination / file["path"]).is_file())

    def test_download_rejects_incomplete_and_unsafe_response(self):
        bad_tasks = [dict(self.task(), status="IN_PROGRESS"),
                     dict(self.task(), model_urls={"../../escape": "https://assets.meshy.ai/file"}),
                     dict(self.task(), model_urls={"fbx": "http://assets.meshy.ai/file"})]
        for task in bad_tasks:
            with patch.object(meshy, "api", return_value=task), self.assertRaises(ValueError):
                meshy.download("task-123", self.root)
        self.assertFalse((self.root / "downloads").exists())


if __name__ == "__main__":
    unittest.main()
