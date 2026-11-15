import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

import unity_auth


class UnityAuthTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.state = Path(self.temp.name) / "state.json"
        self.state_patch = patch.object(unity_auth, "STATE", self.state)
        self.state_patch.start()
        self.addCleanup(self.state_patch.stop)

    def result(self, success, code=None, returncode=0):
        return subprocess.CompletedProcess([], returncode, json.dumps({
            "success": success, "errors": [{"code": code}] if code else []}), "")

    def test_real_cloud_probe_not_optimistic_status(self):
        with patch.object(unity_auth.subprocess, "run", return_value=self.result(True)) as run:
            self.assertEqual(unity_auth.check(), 0)
        self.assertEqual(run.call_args.args[0], ["unity", "cloud", "org", "--json", "--non-interactive"])
        self.assertEqual(unity_auth.cached(), 0)

    def test_expiry_fails_even_when_cli_exits_zero(self):
        with patch.object(unity_auth.subprocess, "run", return_value=self.result(False, "NOT_SIGNED_IN")):
            self.assertEqual(unity_auth.check(), 1)
        self.assertEqual(json.loads(self.state.read_text())["status"], "login_required")
        self.assertEqual(unity_auth.cached(), 1)

    def test_network_failure_does_not_claim_login_expired(self):
        with patch.object(unity_auth.subprocess, "run", side_effect=subprocess.TimeoutExpired("unity", 45)):
            self.assertEqual(unity_auth.check(), 1)
        self.assertEqual(json.loads(self.state.read_text())["status"], "unavailable")

    def test_old_success_warns_without_network(self):
        self.state.write_text(json.dumps({"status": "ready", "checked_at": "2020-01-01T00:00:00+00:00"}))
        with patch.object(unity_auth.subprocess, "run", side_effect=AssertionError("no network")):
            self.assertEqual(unity_auth.cached(), 1)

    def test_repeated_failure_limits_alerts(self):
        def fake_run(command, **kwargs):
            if command[0] == "unity":
                return self.result(False, "NOT_SIGNED_IN", 1)
            return subprocess.CompletedProcess(command, 0)
        with patch.object(unity_auth.shutil, "which", return_value="/usr/bin/notify-send"), patch.object(
                unity_auth.subprocess, "run", side_effect=fake_run) as run:
            unity_auth.check(notify=True)
            unity_auth.check(notify=True)
        self.assertEqual(sum(call.args[0][0] == "notify-send" for call in run.call_args_list), 1)


if __name__ == "__main__":
    unittest.main()
