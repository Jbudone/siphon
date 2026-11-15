#!/usr/bin/env python3
"""Small Meshy Text-to-3D client; Python standard library only.

Docs checked 2026-11-15: https://docs.meshy.ai/en/api/text-to-3d
https://docs.meshy.ai/en/api/balance and /en/api/authentication

Keep the same --staging directory across sessions. A request fingerprint reserves
one submission before POST; a lost response remains reserved, never auto-retried.
Reconcile uncertain submissions in Meshy's Developer Platform Request Logs before
manually removing their journal directory. Changing the JSON can create a paid
new job. No polling, automatic POST retries, key files, or Unity imports.
"""

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import sys
from datetime import datetime, timezone
from urllib import error, parse, request

API = "https://api.meshy.ai"
TASKS = "/openapi/v2/text-to-3d"
DEFAULT_STAGING = Path(__file__).resolve().parent.parent / "artifacts/meshy"
FORMATS = {"glb", "fbx", "obj", "mtl", "stl", "usdz", "3mf"}


def now():
    return datetime.now(timezone.utc).isoformat()


def identifier(value):
    # IDs are opaque; enforce URL/path safety without assuming a UUID version.
    if not isinstance(value, str) or value.startswith("REPLACE_") or not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_-]{0,127}", value):
        raise ValueError("Invalid task ID: use 1-128 letters, digits, underscores or hyphens")
    return value


def staging_path(value):
    path = Path(value).expanduser().resolve()
    if any(part.lower() == "assets" for part in path.parts):
        raise ValueError("Staging must be outside Unity Assets directories")
    return path


def validate(body):
    if not isinstance(body, dict) or body.get("mode") not in ("preview", "refine"):
        raise ValueError("Request mode must be preview or refine")
    common = {"mode", "ai_model", "target_formats", "alpha_thumbnail", "auto_size", "origin_at", "moderation"}
    preview = {"prompt", "model_type", "geometry_resolution", "should_remesh", "topology", "target_polycount", "decimation_mode", "pose_mode"}
    refine = {"preview_task_id", "enable_pbr", "texture_resolution", "texture_prompt", "texture_image_url", "remove_lighting"}
    allowed = common | (preview if body["mode"] == "preview" else refine)
    if set(body) - allowed:
        raise ValueError("Unknown or wrong-mode request fields")
    if body["mode"] == "preview":
        if not isinstance(body.get("prompt"), str) or not 1 <= len(body["prompt"].strip()) <= 800:
            raise ValueError("Preview prompt must contain 1-800 characters")
    else:
        identifier(body.get("preview_task_id"))
    if "texture_prompt" in body and (not isinstance(body["texture_prompt"], str) or len(body["texture_prompt"]) > 800):
        raise ValueError("texture_prompt must be a string of at most 800 characters")
    if "target_formats" in body:
        formats = body["target_formats"]
        if not isinstance(formats, list) or not formats or any(not isinstance(f, str) or f not in FORMATS - {"mtl"} for f in formats):
            raise ValueError("Invalid target_formats")
    return body


def save_json(path, body):
    # Atomic replacement and fsync preserve the ID before success is printed.
    temp = path.with_suffix(".tmp")
    with temp.open("w", encoding="utf-8") as stream:
        json.dump(body, stream, indent=2, sort_keys=True)
        stream.write("\n")
        stream.flush()
        os.fsync(stream.fileno())
    temp.replace(path)
    directory = os.open(path.parent, os.O_RDONLY)
    try:
        os.fsync(directory)
    finally:
        os.close(directory)


class NoRedirect(request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        raise ValueError("Redirect refused; no credentials or POST forwarded")


def api_key():
    key = os.environ.get("MESHY_API_KEY", "")
    if not key or any(char.isspace() for char in key):
        raise ValueError("Set MESHY_API_KEY to a nonempty token without whitespace")
    return key


def api(method, path, body=None):
    key = api_key()
    req = request.Request(API + path, method=method,
                          headers={"Authorization": "Bearer " + key, "Content-Type": "application/json"},
                          data=None if body is None else json.dumps(body).encode())
    # No retry loop; redirects are refused, including for POST.
    try:
        with request.build_opener(NoRedirect()).open(req, timeout=60) as response:
            result = json.load(response)
            if not isinstance(result, dict):
                raise ValueError("Unexpected Meshy response")
            return result
    except error.HTTPError as exc:
        raise ValueError(f"Meshy HTTP {exc.code}; inspect account Request Logs before resubmitting") from None
    except (error.URLError, TimeoutError, OSError, ValueError):
        raise ValueError("Meshy connection failed; submission outcome may be unknown") from None


def submit(body, staging, dry_run=False):
    validate(body)
    digest = hashlib.sha256(json.dumps(body, sort_keys=True, separators=(",", ":")).encode()).hexdigest()
    journal = staging / "requests" / digest
    if dry_run:
        return {"dry_run": True, "method": "POST", "endpoint": API + TASKS,
                "request": body, "journal": str(journal), "network_calls": 0}
    # Check credentials before reserving a request that cannot be sent.
    api_key()
    journal.parent.mkdir(parents=True, exist_ok=True)
    try:
        journal.mkdir()  # Exclusive reservation also protects simultaneous runs.
    except FileExistsError:
        record = journal / "submission.json"
        if record.exists():
            previous = json.loads(record.read_text())
            if previous.get("task_id"):
                return {"task_id": previous["task_id"], "reused": True, "journal": str(journal)}
        raise ValueError(f"Submission already reserved at {journal}; reconcile Request Logs before any new POST") from None
    record = {"provider": "Meshy", "endpoint": API + TASKS, "request": body,
              "request_sha256": digest, "submitted_at": now(), "state": "outcome_unknown"}
    save_json(journal / "submission.json", record)
    directory = os.open(journal.parent, os.O_RDONLY)
    try:
        os.fsync(directory)
    finally:
        os.close(directory)
    response = api("POST", TASKS, body)
    task_id = identifier(response.get("result"))
    record.update(task_id=task_id, state="submitted")
    save_json(journal / "submission.json", record)
    return {"task_id": task_id, "reused": False, "journal": str(journal)}


def download(task_id, staging):
    task_id = identifier(task_id)
    task = api("GET", TASKS + "/" + task_id)
    if task.get("id") != task_id or task.get("status") != "SUCCEEDED":
        raise ValueError("Download requires a matching SUCCEEDED task")
    entries = []
    for fmt, url in task.get("model_urls", {}).items():
        if fmt not in FORMATS:
            raise ValueError("Unsupported model format in response")
        if url:
            entries.append(("model." + fmt, url))
    for field in ("thumbnail_url", "alpha_thumbnail_url"):
        if task.get(field):
            entries.append((field.removesuffix("_url"), task[field]))
    for i, textures in enumerate(task.get("texture_urls", [])):
        for channel, url in textures.items():
            if not re.fullmatch(r"[a-z_]{1,40}", channel):
                raise ValueError("Invalid texture channel")
            if url:
                entries.append((f"texture_{i}_{channel}", url))
    if not any(name.startswith("model.") for name, _ in entries):
        raise ValueError("Completed task has no model files")
    files = []
    # Validate all names/URLs before writing. Never use server-provided filenames.
    for name, url in entries:
        parsed = parse.urlsplit(url)
        if parsed.scheme != "https" or not parsed.hostname or parsed.username or parsed.password:
            raise ValueError("Download URL must use HTTPS without user credentials")
        if not name.startswith("model."):
            suffix = Path(parsed.path).suffix.lower()
            name += suffix if suffix in {".png", ".jpg", ".jpeg", ".webp"} else ".bin"
        files.append((name, url))
    destination = staging_path(staging / "downloads" / task_id)
    destination.mkdir(parents=True, exist_ok=True)
    provenance = {"provider": "Meshy", "task_id": task_id, "downloaded_at": now(),
                  "task": {k: v for k, v in task.items() if k not in {"model_urls", "texture_urls", "thumbnail_url", "alpha_thumbnail_url", "texture_image_url"}},
                  "files": []}
    for path in (staging / "requests").glob("*/submission.json"):
        record = json.loads(path.read_text())
        if record.get("task_id") == task_id:
            provenance["submission"] = record
            break
    save_json(destination / "provenance.json", provenance)
    for name, url in files:
        target = destination / name
        temporary = target.with_suffix(target.suffix + ".part")
        try:
            # Asset downloads receive no API Authorization header; refuse redirects.
            with request.build_opener(NoRedirect()).open(url, timeout=60) as response, temporary.open("wb") as output:
                shutil.copyfileobj(response, output)
            temporary.replace(target)
        except (error.URLError, OSError):
            temporary.unlink(missing_ok=True)
            raise ValueError("Asset download failed; rerun download to resume without submitting a job") from None
        provenance["files"].append({"path": name, "source": parse.urlsplit(url)._replace(query="", fragment="").geturl(),
                                    "sha256": hashlib.sha256(target.read_bytes()).hexdigest(), "bytes": target.stat().st_size})
        save_json(destination / "provenance.json", provenance)
    return {"directory": str(destination), "files": len(files)}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--staging", type=staging_path, default=DEFAULT_STAGING, help="durable journal/download root outside Assets")
    commands = parser.add_subparsers(dest="command", required=True)
    commands.add_parser("balance", help="free account balance GET")
    creation = commands.add_parser("submit", help="submit JSON once; identical JSON reuses saved task ID")
    creation.add_argument("request", type=Path)
    creation.add_argument("--dry-run", action="store_true", help="validate locally; no network, key or writes")
    for command in ("status", "download"):
        commands.add_parser(command).add_argument("task_id", type=identifier)
    args = parser.parse_args(argv)
    try:
        args.staging = staging_path(args.staging)
        if args.command == "balance":
            result = api("GET", "/openapi/v1/balance")
        elif args.command == "submit":
            result = submit(json.loads(args.request.read_text()), args.staging, args.dry_run)
        elif args.command == "status":
            result = api("GET", TASKS + "/" + args.task_id)
        else:
            result = download(args.task_id, args.staging)
        print(json.dumps(result, indent=2))
        return 0
    except (ValueError, OSError, KeyError, TypeError):
        # Error bodies can contain credentials or signed URLs: never echo them.
        exc = sys.exc_info()[1]
        message = str(exc) if isinstance(exc, ValueError) and not isinstance(exc, json.JSONDecodeError) else "Invalid response or local file operation failed"
        print("Error: " + message, file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
