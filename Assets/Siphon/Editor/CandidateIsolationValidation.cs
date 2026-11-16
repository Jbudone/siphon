using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Siphon.Editor
{
    // Generates only the opt-in 600m candidate. Run without -quit; exits after runtime checks.
    public static class CandidateIsolationValidation
    {
        private const string Key = "Siphon.CandidateIsolationValidation";
        private static double start;
        private static bool runtimeError;
        public static void Run()
        {
            try
            {
                SessionState.SetInt(Key + ".phase", 0);
                SessionState.SetString(Key + ".buildSettings", Hash("ProjectSettings/EditorBuildSettings.asset"));
                var original = SourceHashes();
                AssaultSceneBuilder.BuildCandidate();
                EditorSceneManager.OpenScene(AssaultSceneBuilder.CandidateScenePath);
                foreach (var pair in original)
                    Require(File.Exists(pair.Key) && Hash(pair.Key) == pair.Value, "Source bytes unchanged: " + pair.Key);
                var war = UnityEngine.Object.FindAnyObjectByType<WarDirector>();
                Require(war.layoutId == AssaultLayout.CandidateId && war.Layout.Size == 600, "Serialized 600m candidate identity");
                var mesh = GameObject.Find("Rolling battlefield").GetComponent<MeshFilter>().sharedMesh;
                Require(AssetDatabase.GetAssetPath(mesh) == AssaultSceneBuilder.CandidateMeshPath
                    && mesh != AssetDatabase.LoadAssetAtPath<Mesh>(AssaultSceneBuilder.MeshPath)
                    && mesh.vertexCount == 10201 && mesh.bounds.size.x == 600, "Distinct candidate terrain, unchanged dimensions");
                Require(GameObject.Find("Rolling battlefield").GetComponent<MeshCollider>().sharedMesh == mesh, "Candidate collider mesh");
                var nav = UnityEngine.Object.FindAnyObjectByType<NavigationWorld>().field;
                Require(AssetDatabase.GetAssetPath(nav) == NavigationBaker.CandidatePath
                    && nav != AssetDatabase.LoadAssetAtPath<NavigationField>(NavigationBaker.DefaultPath)
                    && nav.Count == 22801, "Distinct candidate navigation");
                var weapon = Camera.main.transform.Find("First person weapon");
                Require(weapon != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(weapon.gameObject) == RifleAssetBuilder.PrefabPath
                    && weapon.GetComponent<WeaponPresentation>() != null, "Accepted rifle prefab survives candidate generation");
                Require(UnityEngine.Object.FindAnyObjectByType<PlaygroundPlayer>().tuning ==
                    AssetDatabase.LoadAssetAtPath<MovementTuning>("Assets/Siphon/Settings/Movement.asset"), "Accepted movement asset reference");
                Debug.Log("Candidate isolation generation passed: all existing Siphon source asset bytes unchanged; separate 600m scene/mesh/navigation; rifle and movement references preserved.");
                start = EditorApplication.timeSinceStartup;
                EditorApplication.update += Begin;
            }
            catch (Exception error) { Debug.LogException(error); Finish(1); }
        }
        private static Dictionary<string, string> SourceHashes()
        {
            var result = new Dictionary<string, string>();
            foreach (string path in Directory.GetFiles("Assets/Siphon", "*", SearchOption.AllDirectories))
            {
                if (path == AssaultSceneBuilder.CandidateScenePath || path == AssaultSceneBuilder.CandidateScenePath + ".meta"
                    || path == AssaultSceneBuilder.CandidateMeshPath || path == AssaultSceneBuilder.CandidateMeshPath + ".meta"
                    || path == NavigationBaker.CandidatePath || path == NavigationBaker.CandidatePath + ".meta") continue;
                result[path] = Hash(path);
            }
            return result;
        }
        private static string Hash(string path)
        { using (var sha = SHA256.Create()) using (var file = File.OpenRead(path)) return Convert.ToBase64String(sha.ComputeHash(file)); }
        private static void Begin()
        {
            if (EditorApplication.timeSinceStartup - start < 2 || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= Begin;
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool(Key, false)) return;
            start = EditorApplication.timeSinceStartup;
            Application.logMessageReceived += Log;
            EditorApplication.update += Tick;
        }
        private static void Log(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) runtimeError = true; }
        private static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup - start > 60) throw new Exception("Candidate isolation validation timeout");
                // Scene reload does not reset Time.time. Allow new Start coroutines to consume requests.
                if (!EditorApplication.isPlaying || Time.time < 0.3f || EditorApplication.timeSinceStartup - start < 0.5) return;
                if (!CheckReloadSequence()) return;
                Require(Hash("ProjectSettings/EditorBuildSettings.asset") == SessionState.GetString(Key + ".buildSettings", ""), "Default build settings unchanged");
                Require(!runtimeError, "No runtime errors");
                Debug.Log("Candidate isolation runtime passed: distinct save paths; legacy no-ID v3 default restore; bidirectional cross-layout rejection before mutation; candidate checkpoint and backup roundtrip; wrong-layout primary/backup rejection; candidate editor checkpoint reload, consumed pending request and fresh Restart.");
                Finish(0);
            }
            catch (Exception error) { Debug.LogException(error); Finish(1); }
        }
        private static bool CheckReloadSequence()
        {
            var war = WarDirector.Instance;
            int phase = SessionState.GetInt(Key + ".phase", 0);
            if (phase == 0)
            {
                CheckPersistence();
                Require(!Array.Exists(EditorBuildSettings.scenes, scene => scene.path == AssaultSceneBuilder.CandidateScenePath), "Candidate exercises editor reload outside default build settings");
                Debug.Log("Candidate editor scene: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + ", observed buildIndex " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                BackupCandidateSave();
                var checkpoint = FieldSaveSystem.Capture(); checkpoint.blue.ferrite = 137;
                FieldSaveSystem.WriteSnapshot(checkpoint, FieldSaveSystem.SavePath);
                war.blueResources = 999;
                NextReload(1, war);
                Require(war.GetComponent<FieldSaveSystem>().RequestLoad(), "F9 checkpoint reload request accepted");
                return false;
            }
            Require(war.layoutId == AssaultLayout.CandidateId && war.GetEntityId().ToString() != SessionState.GetString(Key + ".war", ""), "Reload creates a new candidate world");
            Require(!FieldSaveSystem.FreshWar, "FreshWar consumed after scene startup");
            if (phase == 1)
            {
                Require(war.blueResources == 137, "Editor checkpoint reload restores candidate stock");
                NextReload(2, war);
                WarDirector.ReloadActiveScene();
                return false;
            }
            if (phase == 2)
            {
                Require(war.blueResources == 100, "Pending checkpoint request consumed; ordinary editor reload starts fresh");
                war.blueResources = 222;
                NextReload(3, war);
                war.Restart();
                return false;
            }
            Require(phase == 3 && war.blueResources == 100, "Restart creates fresh candidate instead of loading saved stock");
            return true;
        }
        private static void NextReload(int phase, WarDirector war)
        {
            SessionState.SetInt(Key + ".phase", phase);
            SessionState.SetString(Key + ".war", war.GetEntityId().ToString());
            start = EditorApplication.timeSinceStartup;
        }
        private static void BackupCandidateSave()
        {
            string folder = Path.Combine(Path.GetTempPath(), "siphon-reload-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            SessionState.SetString(Key + ".saveBackup", folder);
            string path = FieldSaveSystem.PathFor(AssaultLayout.Candidate);
            foreach (string suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(path + suffix)) File.Copy(path + suffix, Path.Combine(folder, "checkpoint" + suffix));
        }
        private static void RestoreCandidateSave()
        {
            string folder = SessionState.GetString(Key + ".saveBackup", "");
            if (string.IsNullOrEmpty(folder)) return;
            string path = FieldSaveSystem.PathFor(AssaultLayout.Candidate);
            foreach (string suffix in new[] { "", ".bak", ".tmp" })
            {
                string backup = Path.Combine(folder, "checkpoint" + suffix);
                if (File.Exists(backup)) File.Copy(backup, path + suffix, true);
                else if (File.Exists(path + suffix)) File.Delete(path + suffix);
            }
            Directory.Delete(folder, true);
            SessionState.EraseString(Key + ".saveBackup");
        }
        private static void CheckPersistence()
        {
            var war = WarDirector.Instance;
            var candidate = FieldSaveSystem.Capture();
            Require(candidate.layoutId == AssaultLayout.CandidateId && candidate.version == 4, "Capture explicit identity");
            string candidatePath = FieldSaveSystem.SavePath;
            war.layoutId = "";
            Require(war.Layout == AssaultLayout.Default && FieldSaveSystem.SavePath == FieldSaveSystem.PathFor(AssaultLayout.Default)
                && candidatePath != FieldSaveSystem.SavePath, "Old scene fallback and distinct paths");
            var legacy = Clone(candidate); legacy.version = 3; legacy.layoutId = null;
            legacy.blue.ferrite = 123; FieldSaveSystem.Apply(Clone(legacy));
            Require(war.blueResources == 123, "Legacy no-ID v3 restores on default");
            RejectWithoutMutation(() => FieldSaveSystem.Apply(candidate, 120), candidate, "Candidate rejected by default");
            var defaultSave = FieldSaveSystem.Capture();
            war.layoutId = AssaultLayout.CandidateId;
            RejectWithoutMutation(() => FieldSaveSystem.Apply(defaultSave, 120), defaultSave, "Default rejected by candidate");
            RejectWithoutMutation(() => FieldSaveSystem.Apply(legacy, 120), legacy, "Legacy rejected by candidate");
            string folder = Path.Combine(Path.GetTempPath(), "siphon-isolation-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(folder, "checkpoint.json");
            Directory.CreateDirectory(folder);
            try
            {
                FieldSaveSystem.WriteSnapshot(candidate, path);
                var read = FieldSaveSystem.ReadSnapshot(path, out bool recovered);
                Require(!recovered && read.layoutId == AssaultLayout.CandidateId, "Candidate checkpoint roundtrip");
                FieldSaveSystem.Apply(read);
                Require(war.blueResources == candidate.blue.ferrite, "Candidate checkpoint restores stock");
                var second = Clone(candidate); second.blue.ferrite++;
                FieldSaveSystem.WriteSnapshot(second, path);
                File.WriteAllText(path, "damaged");
                read = FieldSaveSystem.ReadSnapshot(path, out recovered);
                Require(recovered && read.blue.ferrite == candidate.blue.ferrite, "Candidate backup roundtrip");
                FieldSaveSystem.Apply(read);
                // A valid incompatible primary must not be hidden by a compatible backup.
                File.WriteAllText(path, JsonUtility.ToJson(defaultSave));
                RejectWithoutMutation(() => FieldSaveSystem.ReadSnapshot(path, out _), defaultSave, "Wrong-layout primary fails closed");
                File.WriteAllText(path, "damaged");
                File.WriteAllText(path + ".bak", JsonUtility.ToJson(defaultSave));
                RejectWithoutMutation(() => FieldSaveSystem.ReadSnapshot(path, out _), defaultSave, "Wrong-layout backup fails closed");
                File.WriteAllText(path, JsonUtility.ToJson(legacy).Replace("\"layoutId\":\"\",", "").Replace("\"layoutId\":null,", ""));
                File.Delete(path + ".bak");
                war.layoutId = "";
                read = FieldSaveSystem.ReadSnapshot(path, out recovered);
                FieldSaveSystem.Apply(read);
                Require(!recovered && war.blueResources == 123, "Legacy file without ID reads and restores on default");
                war.layoutId = AssaultLayout.CandidateId;
                RejectWithoutMutation(() => FieldSaveSystem.ReadSnapshot(path, out _), legacy, "Legacy file rejected by candidate");
            }
            finally { war.layoutId = AssaultLayout.CandidateId; Directory.Delete(folder, true); }
        }
        private static FieldSaveSystem.Snapshot Clone(FieldSaveSystem.Snapshot snapshot) =>
            JsonUtility.FromJson<FieldSaveSystem.Snapshot>(JsonUtility.ToJson(snapshot));
        private static void RejectWithoutMutation(Action action, FieldSaveSystem.Snapshot input, string reason)
        {
            var before = FieldSaveSystem.Capture();
            string inputBefore = JsonUtility.ToJson(input);
            bool rejected = false;
            try { action(); } catch (InvalidDataException) { rejected = true; }
            var after = FieldSaveSystem.Capture(); after.utcTicks = before.utcTicks;
            Require(rejected && JsonUtility.ToJson(before) == JsonUtility.ToJson(after)
                && inputBefore == JsonUtility.ToJson(input), reason + " before world/snapshot mutation");
        }
        private static void Require(bool pass, string reason)
        { if (!pass) throw new Exception("Candidate isolation failed: " + reason); }
        private static void Finish(int code)
        {
            try { RestoreCandidateSave(); }
            catch (Exception error) { Debug.LogException(error); code = 1; }
            SessionState.SetBool(Key, false);
            EditorApplication.update -= Begin;
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= Log;
            EditorApplication.Exit(code);
        }
    }
}
