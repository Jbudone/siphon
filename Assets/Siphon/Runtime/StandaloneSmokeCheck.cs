using System;
using UnityEngine;

namespace Siphon
{
    // Only runs when explicitly requested by the standalone verification command.
    public sealed class StandaloneSmokeCheck : MonoBehaviour
    {
        private bool errors;
        private float began;
        private float duration = 12;
        private int peakBots, totalDeliveries;
        private float worstFrame;
        private string capturePath;
        private bool captureStarted, soakPrepared, encounterCapture, siegeCapture;
        private void Awake()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-siphon-smoke") < 0)
            { enabled = false; return; }
            var args = Environment.GetCommandLineArgs();
            encounterCapture = Array.IndexOf(args, "-siphon-capture-encounters") >= 0;
            siegeCapture = Array.IndexOf(args, "-siphon-capture-siege") >= 0;
            if (encounterCapture || siegeCapture) duration = siegeCapture ? 25 : 20;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-siphon-soak" && float.TryParse(args[i + 1], out float seconds)) duration = Mathf.Clamp(seconds, 12, 600);
                if (args[i] == "-siphon-capture") capturePath = args[i + 1];
            }
            began = Time.realtimeSinceStartup;
            Application.logMessageReceived += Log;
        }
        private void Log(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors = true; }
        private void Update()
        {
            if (duration > 30 && !soakPrepared && WarDirector.Instance != null)
            {
                soakPrepared = true;
                // Keep squads running throughout the performance soak; normal playtest health is unchanged.
                WarDirector.Instance.blueCore.maximumHealth = WarDirector.Instance.blueCore.health = 100000;
                WarDirector.Instance.redCore.maximumHealth = WarDirector.Instance.redCore.health = 100000;
            }
            peakBots = Mathf.Max(peakBots, BotBrain.All.Count);
            worstFrame = Mathf.Max(worstFrame, Time.unscaledDeltaTime);
            totalDeliveries = 0;
            foreach (var bot in BotBrain.All) totalDeliveries += bot.Deliveries;
            float elapsed = Time.realtimeSinceStartup - began;
            if (capturePath != null && !captureStarted && elapsed > 7)
            { captureStarted = true; StartCoroutine(Capture()); }
            if (elapsed < duration) return;
            var war = WarDirector.Instance;
            bool pass = !errors && war != null && war.blueCore != null && war.redCore != null
                && BotBrain.All.Count >= 19 && ResourceNode.All.Count >= 70 && NavigationWorld.Instance != null;
            foreach (var bot in BotBrain.All)
                if (float.IsNaN(bot.transform.position.x) || bot.transform.position.y < -30) pass = false;
            Debug.Log(pass ? "Siphon standalone smoke check passed." : "Siphon standalone smoke check failed.");
            Debug.Log($"Soak evidence: {duration:0}s, {peakBots} bots, {totalDeliveries} worker deliveries, navigation {NavigationWorld.Instance?.Searches} searches / {NavigationWorld.Instance?.FailedSearches} failures, worst frame {worstFrame * 1000:0}ms including startup.");
            Application.Quit(pass ? 0 : 1);
        }
        private System.Collections.IEnumerator Capture()
        {
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
            var motor = player.GetComponent<PlaygroundPlayer>();
            motor.ResetTo(WarDirector.Instance.blueSpawn, Quaternion.identity); motor.enabled = false;
            yield return new WaitForEndOfFrame();
            WriteCapture(capturePath);
            player.ToggleMenu(1);
            yield return null; yield return new WaitForEndOfFrame();
            WriteCapture(capturePath.Replace(".png", "-build.png"));
            player.ToggleMenu(2);
            yield return null; yield return new WaitForEndOfFrame();
            WriteCapture(capturePath.Replace(".png", "-gear.png"));
            player.GetComponent<FieldHud>().FocusResearch(Upgrade.SiegeCalibration);
            yield return null; yield return new WaitForEndOfFrame();
            WriteCapture(capturePath.Replace(".png", "-research.png"));
            player.CloseMenu(); player.GetComponent<FieldHud>().ShowTactical(true);
            yield return null; yield return new WaitForEndOfFrame();
            WriteCapture(capturePath.Replace(".png", "-tactical.png"));
            player.GetComponent<FieldHud>().ShowTactical(false);
            player.ToggleMenu(3);
            yield return null; yield return new WaitForEndOfFrame();
            WriteCapture(capturePath.Replace(".png", "-spawns.png")); player.CloseMenu();
            if (encounterCapture)
            {
                var warden = UnityEngine.Object.FindAnyObjectByType<WardenBrain>();
                if (warden != null)
                {
                    player.Gear.thermalLining = true;
                    motor.ResetTo(WarDirector.GroundPoint(warden.home + Vector3.right * 25) + Vector3.up * 0.15f, Quaternion.identity);
                    motor.view.transform.LookAt(warden.GetComponent<Combatant>().AimPoint); Physics.SyncTransforms();
                    float deadline = Time.realtimeSinceStartup + 5;
                    while (!warden.Telegraphing && Time.realtimeSinceStartup < deadline) yield return null;
                    yield return new WaitForEndOfFrame();
                    WriteCapture(capturePath.Replace(".png", "-warden.png"));
                    warden.GetComponent<Combatant>().Damage(10000, Team.Blue, warden.home - warden.transform.forward * 20, player.Entity);
                    var salvage = DroppedCargo.All.Find(drop => drop.relic);
                    if (salvage != null)
                    {
                        motor.ResetTo(WarDirector.GroundPoint(salvage.transform.position + new Vector3(2, 0, 3)) + Vector3.up * 0.15f, Quaternion.identity);
                        motor.view.transform.LookAt(salvage.transform.position); Physics.SyncTransforms();
                        yield return null; yield return new WaitForEndOfFrame();
                        WriteCapture(capturePath.Replace(".png", "-salvage.png"));
                    }
                }
            }
            if (siegeCapture)
            {
                var war = WarDirector.Instance; war.blueResources = 100; war.blueAlloy = 20;
                var gun = war.CreateStructure(EntityKind.SiegeCannon, Team.Blue, WarDirector.GroundPoint(new Vector3(-15, 0, -90)), 0).GetComponent<SiegeBattery>();
                war.CreateStructure(EntityKind.Generator, Team.Blue, WarDirector.GroundPoint(new Vector3(-28, 0, -90)), 0);
                var target = war.CreateStructure(EntityKind.Wall, Team.Red, WarDirector.GroundPoint(new Vector3(15, 0, 30)), 0);
                war.MarkTarget(target.Entity.AimPoint, target.Entity);
                motor.ResetTo(WarDirector.GroundPoint(new Vector3(-23, 0, -98)) + Vector3.up * 0.15f, Quaternion.identity);
                motor.view.transform.LookAt(gun.transform.position + Vector3.up * 2.8f); Physics.SyncTransforms();
                float deadline = Time.realtimeSinceStartup + 5;
                while (!gun.Charging && Time.realtimeSinceStartup < deadline) yield return null;
                yield return new WaitForEndOfFrame(); WriteCapture(capturePath.Replace(".png", "-siege.png"));
            }
            motor.enabled = true;
        }
        private static void WriteCapture(string path)
        {
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            if (image != null) { System.IO.File.WriteAllBytes(path, image.EncodeToPNG()); Destroy(image); }
        }
        private void OnDestroy() { Application.logMessageReceived -= Log; }
    }
}
