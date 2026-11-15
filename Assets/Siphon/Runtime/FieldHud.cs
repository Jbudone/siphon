using UnityEngine;

namespace Siphon
{
    [RequireComponent(typeof(PlayerCombat))]
    public sealed class FieldHud : MonoBehaviour
    {
        private PlayerCombat combat;
        private PlaygroundPlayer motor;
        private GUIStyle small;
        private GUIStyle medium;
        private GUIStyle large;
        private bool help;
        private bool tactical;
        private float nextContact;
        private Vector2 researchScroll, spawnScroll;
        private void Awake() { combat = GetComponent<PlayerCombat>(); motor = GetComponent<PlaygroundPlayer>(); }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) help = !help;
            if (Input.GetKeyDown(KeyCode.Tab)) tactical = !tactical;
            if (Time.time >= nextContact && combat.Entity != null && combat.Entity.Alive && WarDirector.Instance != null)
            {
                nextContact = Time.time + 0.5f;
                foreach (var actor in Combatant.All)
                    if (actor.Alive && actor.team == Team.Red && (actor.transform.position - transform.position).sqrMagnitude < 12100
                        && (actor.GetComponent<FieldGear>() == null || actor.GetComponent<FieldGear>().VisibleFrom(motor.view.transform.position))
                        && Combatant.CanSee(motor.view.transform.position, actor)) WarDirector.Instance.ReportContact(Team.Blue, actor);
            }
        }
        private void OnGUI()
        {
            var war = WarDirector.Instance;
            if (war == null || combat.Entity == null) return;
            if (small == null)
            {
                small = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
                medium = new GUIStyle(small) { fontSize = 18, fontStyle = FontStyle.Bold };
                large = new GUIStyle(medium) { fontSize = 30 };
            }
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            Panel(new Rect(20, 20, 255, 70));
            Text(new Rect(34, 27, 260, 26), "SIPHON / FRONTIER", medium, Color.white);
            Text(new Rect(34, 53, 260, 22), $"F1 controls   TAB tactics   {combat.Kills} takedowns", small, new Color(0.6f, 0.75f, 0.85f));
            CoreBar(war.blueCore, new Rect(width * 0.5f - 245, 24, 210, 16));
            CoreBar(war.redCore, new Rect(width * 0.5f + 5, 24, 210, 16));
            Panel(new Rect(width - 355, 20, 335, 92));
            Text(new Rect(width - 340, 26, 315, 24), $"STOCK   {war.blueResources} Fe / {war.blueTimber} wood", medium, Color.cyan);
            Text(new Rect(width - 340, 51, 315, 22), $"{war.blueCrystal} crystal / {war.blueAlloy} alloy    ·    V build / T craft", small, Color.white);
            Text(new Rect(width - 340, 77, 315, 22), $"CARGO {combat.Cargo}/60   {(combat.CarryingRelic ? "CAPACITOR + " : "")}{combat.CargoBag}", small, Color.white);

            Panel(new Rect(20, height - 115, 280, 95));
            Text(new Rect(34, height - 105, 250, 25), $"HEALTH  {combat.Entity.health:0}", medium, Color.white);
            Bar(new Rect(34, height - 77, 250, 7), combat.Entity.health / combat.Entity.maximumHealth, Color.cyan);
            Text(new Rect(34, height - 63, 250, 25), $"JET FUEL   {motor.FuelFraction:P0}", small, Color.white);
            Bar(new Rect(34, height - 36, 250, 6), motor.FuelFraction, new Color(0.4f, 1, 0.6f));

            Panel(new Rect(width * 0.5f - 112, height - 104, 248, 84));
            Text(new Rect(width * 0.5f - 80, height - 94, 200, 40), $"{motor.Velocity.magnitude:0} m/s", large, Color.white);
            Text(new Rect(width * 0.5f - 100, height - 52, 230, 24),
                $"{(motor.Skiing ? "SKI" : "RUN")}  {(motor.Grappling ? "GRAPPLE" : "")}  PEAK {motor.PeakSpeed:0}", small, Color.cyan);
            Panel(new Rect(width - 300, height - 100, 280, 80));
            Text(new Rect(width - 282, height - 90, 270, 28), combat.Weapon == 0 ? "01 / PULSE RIFLE" : "02 / EXPLOSIVE DISC", medium, Color.white);
            Text(new Rect(width - 282, height - 60, 270, 26), "1 / 2 switch    V build menu    T equipment", small, Color.cyan);

            float cx = width * 0.5f, cy = height * 0.5f;
            Color cross = Time.time < combat.KillUntil ? new Color(1, 0.3f, 0.15f) : Time.time < combat.HitUntil ? Color.yellow : Color.white;
            Fill(new Rect(cx - 10, cy - 1, 6, 2), cross);
            Fill(new Rect(cx + 4, cy - 1, 6, 2), cross);
            Fill(new Rect(cx - 1, cy - 10, 2, 6), cross);
            Fill(new Rect(cx - 1, cy + 4, 2, 6), cross);
            if (Time.time < combat.DamageUntil)
            {
                Color damage = new Color(1, 0.12f, 0, 0.45f);
                Fill(new Rect(0, 0, width, 5), damage); Fill(new Rect(0, height - 5, width, 5), damage);
                Fill(new Rect(0, 0, 5, height), damage); Fill(new Rect(width - 5, 0, 5, height), damage);
                Vector3 local = transform.InverseTransformDirection(combat.Entity.LastDamageOrigin - transform.position);
                float bearing = Mathf.Atan2(local.x, local.z);
                Fill(new Rect(cx + Mathf.Sin(bearing) * 80 - 6, cy - Mathf.Cos(bearing) * 80 - 6, 12, 12), Color.red);
            }
            if (combat.Menu == 0 && !string.IsNullOrEmpty(combat.Context))
            {
                Panel(new Rect(cx - 390, cy + 58, 780, 38));
                Text(new Rect(cx - 376, cy + 65, 752, 25), combat.Context, small, Color.white);
            }
            if (combat.Menu == 0 && Time.time < war.MessageUntil)
            {
                Panel(new Rect(cx - 285, 120, 570, 52));
                Text(new Rect(cx - 270, 127, 540, 40), war.Message, small, Color.white);
            }
            if (combat.Menu == 0)
            {
                DrawLabels(scale);
                if (war.RallyActive)
                {
                    Panel(new Rect(cx - 285, 180, 570, 30));
                    Text(new Rect(cx - 272, 185, 550, 25), $"TARGET {war.RallyLabel} · {(war.RallyPoint - transform.position).magnitude:0}m · {war.RallyUntil - Time.time:0}s · MMB retarget", small, new Color(1, 0.65f, 0.2f));
                    Vector3 marker = motor.view.WorldToScreenPoint(war.RallyPoint);
                    if (marker.z > 0)
                    {
                        float mx = marker.x / scale, my = (Screen.height - marker.y) / scale;
                        Fill(new Rect(mx - 8, my - 8, 16, 2), Color.yellow); Fill(new Rect(mx - 8, my + 6, 16, 2), Color.yellow);
                        Fill(new Rect(mx - 8, my - 8, 2, 16), Color.yellow); Fill(new Rect(mx + 6, my - 8, 2, 16), Color.yellow);
                    }
                }
            }
            if (combat.Menu == 0 && !tactical)
                for (int i = 0; i < war.BattleEvents.Count; i++)
                {
                    var item = war.BattleEvents[i];
                    Panel(new Rect(width - 355, 120 + i * 24, 335, 23));
                    Text(new Rect(width - 342, 120 + i * 24, 315, 23), item.text, small, Combatant.TeamColor(item.team));
                }
            if (combat.Menu == 0 && !tactical && (help || Time.time < 9))
            {
                Panel(new Rect(20, 110, 300, 350));
                Text(new Rect(36, 120, 280, 330),
                    "DESTROY THE RED CORE\n\n" +
                    "WASD · move    Mouse · aim\nShift · ski    Space · jump\nRMB · jets    Hold E · grapple\nLMB · fire    1 / 2 · weapons\nHold F · harvest / repair / deposit\nG · turret    H · generator    B · spawn\nV · all structures    T · equipment\nMMB · mark assault / siege target\nQ · rotate build    RMB · cancel\nC · cloak (requires crafted weave)\nF5 · save    F9 · resume checkpoint\nR · return to start    Esc · cursor\n\nPower range 35m; core shielding 40m.\nTrees → wood; purple nodes → crystal.\nSmelters → alloy for advanced tech.", small, Color.white);
            }
            if (tactical && combat.Menu == 0) { Tactical(war, width); Map(war, new Rect(20, height * 0.5f - 130, 290, 310)); }
            if (combat.Gear != null)
            {
                if (combat.Gear.cloakWeave)
                {
                    Panel(new Rect(width - 300, height - 134, 280, 30));
                    Text(new Rect(width - 282, height - 126, 275, 25), $"CLOAK {combat.Gear.CloakEnergy:P0} / {(combat.Gear.Cloaked ? "ACTIVE / RADAR DETECTS" : "C TO TOGGLE")}", small, combat.Gear.Cloaked ? new Color(0.85f, 0.5f, 1) : Color.cyan);
                }
                Panel(new Rect(20, height - 154, 400, 30));
                Text(new Rect(34, height - 148, 360, 25), combat.Gear.Biome + (combat.Gear.thermalLining ? " / INSULATED" : ""), small, Color.white);
                if (combat.Gear.Exposure > 0)
                {
                    Panel(new Rect(20, height - 180, 410, 30));
                    Text(new Rect(34, height - 173, 380, 25), $"FROST EXPOSURE {combat.Gear.Exposure:P0} · return to shelter or craft insulation", small, Color.cyan);
                    Bar(new Rect(34, height - 150, 250, 4), combat.Gear.Exposure, Color.cyan);
                }
            }
            if (combat.Menu != 0) Menu(war, width, height);
            if (war.Finished || !combat.Entity.Alive && combat.Menu != 3)
            {
                Panel(new Rect(cx - 280, cy - 80, 560, 120));
                Text(new Rect(cx - 250, cy - 65, 510, 45), war.Finished ? war.Winner.ToString().ToUpperInvariant() + " WINS" : "DOWN", large, Color.white);
                Text(new Rect(cx - 250, cy - 10, 510, 30), war.Finished ? "Enemy core destroyed. Enter to start a fresh war."
                    : $"Respawn in {Mathf.Max(0, combat.RespawnsAt - Time.time):0.0}s · N selects a forward base", small, Color.cyan);
            }
            GUI.matrix = Matrix4x4.identity;
        }
        private void DrawLabels(float scale)
        {
            foreach (var entity in Combatant.All)
            {
                if (!entity.Alive || entity == combat.Entity) continue;
                float distance = Vector3.Distance(transform.position, entity.transform.position);
                bool knownCore = entity.kind == EntityKind.Core;
                if (distance > (knownCore ? 150 : 110)) continue;
                var gear = entity.GetComponent<FieldGear>();
                if (gear != null && !gear.VisibleFrom(motor.view.transform.position)) continue;
                bool movingActor = entity.kind == EntityKind.Bot || entity.kind == EntityKind.Creature;
                if (movingActor && (distance > 65 || Vector3.Dot(motor.view.transform.forward,
                    (entity.AimPoint - motor.view.transform.position).normalized) < 0.975f)) continue;
                if (!movingActor && !knownCore && Vector3.Dot(motor.view.transform.forward,
                    (entity.AimPoint - motor.view.transform.position).normalized) < 0.97f) continue;
                if (!knownCore && entity.team != combat.Entity.team && !Combatant.CanSee(motor.view.transform.position, entity)) continue;
                Vector3 labelPoint = entity.GetComponent<DefenseStructure>() != null
                    ? entity.transform.position + Vector3.up * (StructureCatalog.Size(entity.kind).y + 0.7f) : entity.AimPoint + Vector3.up * 1.7f;
                Vector3 screen = motor.view.WorldToScreenPoint(labelPoint);
                if (screen.z <= 0) continue;
                float x = screen.x / scale, y = (Screen.height - screen.y) / scale;
                string label = StructureCatalog.Name(entity.kind).ToUpperInvariant();
                var bot = entity.GetComponent<BotBrain>();
                if (bot != null) label = bot.role.ToString().ToUpperInvariant();
                var warden = entity.GetComponent<WardenBrain>();
                if (warden != null) label = "WARDEN / " + (warden.Venting ? "REACTOR EXPOSED" : "ARMORED FRONT");
                if (knownCore) label += entity.Shielded ? " / SHIELDED" : " / EXPOSED";
                if (entity.kind == EntityKind.Turret || entity.kind == EntityKind.Outpost)
                    label += WarDirector.Instance.IsPowered(entity.team, entity.transform.position, 35) ? " / POWERED" : " / OFFLINE";
                Panel(new Rect(x - 110, y - 17, 250, 25));
                Text(new Rect(x - 100, y - 15, 230, 24), $"{label}  {distance:0}m", small, Combatant.TeamColor(entity.team));
                if (distance < 80) Bar(new Rect(x - 35, y + 10, 70, 4), entity.health / entity.maximumHealth, Combatant.TeamColor(entity.team));
                if (warden != null && warden.Telegraphing)
                {
                    Panel(new Rect(x - 170, y + 18, 340, 27));
                    Text(new Rect(x - 160, y + 20, 320, 24), warden.Phase, small, new Color(1, 0.55f, 0.1f));
                }
            }
        }
        public void FocusResearch(Upgrade upgrade)
        { int index = System.Array.IndexOf(GearCatalog.Research, upgrade); researchScroll.y = Mathf.Max(0, index * 102 - 150); }
        public void ShowTactical(bool value) { tactical = value; help = false; }
        private void Tactical(WarDirector war, float width)
        {
            Panel(new Rect(width - 360, 150, 340, 380));
            Text(new Rect(width - 342, 162, 310, 25), "BLUE CAPTAIN / " + war.BlueOrder.ToString().ToUpperInvariant(), medium, Color.cyan);
            int row = 0;
            foreach (var bot in BotBrain.All)
            {
                if (bot.Entity.team != Team.Blue) continue;
                Text(new Rect(width - 342, 199 + row * 27, 310, 25), $"{bot.role}: {(bot.Entity.Alive ? bot.CombatState == "Moving" ? bot.Task : bot.CombatState : "Respawning")}", small, Color.white);
                row++;
            }
            Text(new Rect(width - 342, 405, 310, 120),
                "J · cycle assault / defend / escort / balanced\nWorkers repair before gathering.\nScouts establish forward spawns\nand investigate audible enemy activity.\n\nWest: timber and the forest relay.\nEast: crystals, frost and the Warden.", small, Color.white);
        }
        private void Map(WarDirector war, Rect bounds)
        {
            Panel(bounds);
            Text(new Rect(bounds.x + 12, bounds.y + 8, bounds.width - 20, 25), "CONTACT MAP / 600m", medium, Color.cyan);
            Rect map = new Rect(bounds.x + 12, bounds.y + 42, bounds.width - 24, bounds.height - 62);
            Fill(map, new Color(0.12f, 0.2f, 0.23f, 0.95f));
            Fill(new Rect(map.x, map.y, map.width * 0.3f, map.height), new Color(0.12f, 0.28f, 0.19f));
            Fill(new Rect(map.x + map.width * 0.7f, map.y, map.width * 0.3f, map.height), new Color(0.3f, 0.42f, 0.48f));
            for (int i = 1; i < 6; i++)
            {
                Fill(new Rect(map.x + i * map.width / 6, map.y, 1, map.height), new Color(1, 1, 1, 0.08f));
                Fill(new Rect(map.x, map.y + i * map.height / 6, map.width, 1), new Color(1, 1, 1, 0.08f));
            }
            void Pip(Vector3 position, Color color, float size)
            {
                float x = map.x + Mathf.Clamp01((position.x + 300) / 600) * map.width;
                float y = map.y + (1 - Mathf.Clamp01((position.z + 300) / 600)) * map.height;
                Fill(new Rect(x - size / 2, y - size / 2, size, size), color);
            }
            foreach (var actor in Combatant.All)
                if (actor.Alive && (actor.team == Team.Blue || actor.kind == EntityKind.Core))
                    Pip(actor.transform.position, Combatant.TeamColor(actor.team), actor.kind == EntityKind.Core ? 10 : 4);
            foreach (var contact in war.SensorContacts)
                if (contact.observer == Team.Blue)
                    Pip(contact.position, new Color(1, 0.35f, 0.1f, Mathf.Clamp01((contact.expires - Time.time) / 5)), 5);
            if (combat.Gear.surveyOptics)
                foreach (var node in ResourceNode.All)
                    if (node.remaining > 0) Pip(node.transform.position,
                        node.kind == ResourceKind.Crystal ? new Color(0.85f, 0.4f, 1) : node.kind == ResourceKind.Timber ? Color.green : Color.yellow, 2);
            Pip(new Vector3(228, 0, 92), new Color(0.85f, 0.4f, 1), 8);
            Pip(new Vector3(-225, 0, -20), new Color(0.85f, 0.4f, 1), 6);
            if (war.RallyActive) Pip(war.RallyPoint, Color.yellow, 10);
            Pip(transform.position, Color.white, 7);
        }
        private void Menu(WarDirector war, float width, float height)
        {
            float left = width * 0.5f - 410, top = height * 0.5f - 290;
            Fill(new Rect(left, top, 820, 620), new Color(0.025f, 0.05f, 0.08f, 0.98f));
            Text(new Rect(left + 24, top + 20, 700, 35), combat.Menu == 1 ? "FIELD CONSTRUCTION" : combat.Menu == 3 ? "RESPAWN NETWORK" : "EQUIPMENT WORKBENCH", large, Color.cyan);
            if (GUI.Button(new Rect(left + 720, top + 22, 75, 30), "Close")) { combat.CloseMenu(); return; }
            if (combat.Menu == 3) { SpawnMenu(war, left, top); return; }
            Text(new Rect(left + 24, top + 67, 760, 30), combat.Menu == 1
                ? "Uses team stock. Choose a structure, aim at terrain, Q rotates, LMB places."
                : "Uses team stock within 12m of a friendly workbench. Scroll for research; the world continues.", small, Color.white);
            if (combat.Menu == 1)
            {
                for (int i = 0; i < StructureCatalog.Buildable.Length; i++)
                {
                    var kind = StructureCatalog.Buildable[i];
                    float y = top + 112 + i * 52;
                    bool unlocked = combat.Gear.CanBuild(kind);
                    bool affordable = war.StockBag(Team.Blue).Covers(StructureCatalog.Cost(kind));
                    GUI.enabled = unlocked && affordable && combat.Entity.Alive;
                    if (GUI.Button(new Rect(left + 24, y, 165, 40), StructureCatalog.Name(kind))) { combat.BeginBuild(kind); break; }
                    GUI.enabled = true;
                    Text(new Rect(left + 205, y - 1, 570, 23), StructureCatalog.Cost(kind) + (!unlocked ? " / REQUIRES EQUIPMENT" : !affordable ? " / NEED SUPPLIES" : ""), medium,
                        unlocked && affordable ? Color.cyan : new Color(0.85f, 0.65f, 0.4f));
                    Text(new Rect(left + 205, y + 22, 590, 30), StructureCatalog.Description(kind), small, Color.white);
                }
            }
            else
            {
                bool workbench = combat.Gear.AtWorkbench();
                researchScroll = GUI.BeginScrollView(new Rect(left + 18, top + 110, 785, 365), researchScroll,
                    new Rect(0, 0, 760, GearCatalog.Research.Length * 102));
                for (int i = 0; i < GearCatalog.Research.Length; i++)
                {
                    var upgrade = GearCatalog.Research[i];
                    float y = i * 102;
                    bool prerequisites = GearCatalog.RequirementsMet(upgrade, combat.Gear);
                    bool owned = combat.Gear.Has(upgrade), affordable = war.StockBag(Team.Blue).Covers(GearCatalog.Cost(upgrade));
                    Text(new Rect(6, y, 750, 28), GearCatalog.Name(upgrade) + (owned ? " / EQUIPPED" : " / " + GearCatalog.Cost(upgrade)), medium, owned ? Color.green : Color.cyan);
                    Text(new Rect(6, y + 30, 740, 38), GearCatalog.Description(upgrade), small, Color.white);
                    GUI.enabled = workbench && !owned && affordable && prerequisites && combat.Entity.Alive;
                    if (GUI.Button(new Rect(6, y + 68, 180, 30), owned ? "Equipped" : !prerequisites ? "Research prerequisites" : !workbench ? "Need workbench" : !affordable ? "Need supplies" : "Craft")) combat.Gear.Craft(upgrade);
                    GUI.enabled = true;
                }
                GUI.EndScrollView();
                Text(new Rect(left + 24, top + 493, 760, 28), "Warden capacitor / " + (combat.Gear.wardenCapacitor ? "EQUIPPED" : combat.CarryingRelic ? "CARRIED — DEPOSIT TO EQUIP" : "BOSS SALVAGE"), medium, combat.Gear.wardenCapacitor ? Color.green : new Color(0.9f, 0.5f, 1));
                Text(new Rect(left + 24, top + 525, 760, 28), "25% more jet fuel. Flank the vault Warden or strike when its reactor vents; bring the cache home.", small, Color.white);
                Text(new Rect(left + 24, top + 568, 760, 45), workbench
                    ? "Timber: harvest forest trees. Crystals: purple nodes in the icefield. Alloy: powered smelter.\nDeposit with F before crafting. Start with insulation, then survey optics or field engineering."
                    : "NO WORKBENCH NEARBY — return to a friendly core or build a forward outpost.", small, workbench ? Color.white : Color.yellow);
            }
            GUI.enabled = true;
        }
        private void SpawnMenu(WarDirector war, float left, float top)
        {
            Text(new Rect(left + 24, top + 68, 760, 40), combat.Entity.Alive
                ? "Choose where future deaths respawn. Powered outposts need an open exit; unavailable bases fall back safely."
                : $"Respawning in {Mathf.Max(0, combat.RespawnsAt - Time.time):0.0}s. Choose a base now or set your next respawn.", small, Color.white);
            if (GUI.Button(new Rect(left + 24, top + 116, 220, 38), "Automatic")) war.Spawns.ChooseAutomatic();
            if (GUI.Button(new Rect(left + 264, top + 116, 220, 38), "Main base")) war.Spawns.Choose(war.blueCore);
            Text(new Rect(left + 24, top + 172, 760, 28), "PREFERENCE / " + war.Spawns.Preference.ToString().ToUpperInvariant(), medium, Color.cyan);
            int count = 0;
            foreach (var station in Combatant.All) if (station.team == Team.Blue && station.kind == EntityKind.Outpost && station.Alive) count++;
            spawnScroll = GUI.BeginScrollView(new Rect(left + 18, top + 210, 785, 325), spawnScroll, new Rect(0, 0, 760, Mathf.Max(325, count * 84)));
            int row = 0;
            foreach (var station in Combatant.All)
            {
                if (station.team != Team.Blue || station.kind != EntityKind.Outpost || !station.Alive) continue;
                float y = row++ * 84;
                bool powered = war.IsPowered(Team.Blue, station.transform.position, 35), available = war.Spawns.Available(station, out _);
                string status = !powered ? "POWER OFFLINE" : !available ? "EXITS OBSTRUCTED" : "READY";
                Text(new Rect(6, y, 750, 28), $"OUTPOST {station.transform.position.x:0}, {station.transform.position.z:0} / {status}" + (war.Spawns.IsSelected(station) ? " / SELECTED" : ""), medium, available ? Color.cyan : Color.yellow);
                Text(new Rect(6, y + 31, 400, 30), $"{station.health:0}/{station.maximumHealth:0} HP · {(station.transform.position - transform.position).magnitude:0}m away", small, Color.white);
                GUI.enabled = available;
                if (GUI.Button(new Rect(560, y + 30, 180, 32), "Select forward base")) war.Spawns.Choose(station);
                GUI.enabled = true;
            }
            if (count == 0) Text(new Rect(6, 5, 730, 50), "No forward bases yet. Build an outpost with B or follow a scout; add a generator within 35m.", medium, Color.white);
            GUI.EndScrollView();
            Text(new Rect(left + 24, top + 558, 760, 50), "N opens this network during play or while down. Your choice persists when saving. Bots use available forward bases independently.", small, Color.white);
        }
        private void CoreBar(Combatant core, Rect rect)
        {
            if (core == null) return;
            Bar(rect, core.health / core.maximumHealth, Combatant.TeamColor(core.team));
            Text(new Rect(rect.x, rect.y + 22, rect.width, 24),
                $"{core.team.ToString().ToUpperInvariant()} CORE  {core.health:0}  {(core.Shielded ? "SHIELDED" : "EXPOSED")}", small, Color.white);
        }
        private static void Panel(Rect rect) { Fill(rect, new Color(0.025f, 0.05f, 0.08f, 0.85f)); }
        private static void Fill(Rect rect, Color color)
        {
            GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = Color.white;
        }
        private static void Bar(Rect rect, float value, Color color)
        {
            Fill(rect, new Color(0.05f, 0.1f, 0.14f, 0.9f));
            Fill(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height), color);
        }
        private static void Text(Rect rect, string text, GUIStyle style, Color color)
        {
            var previous = GUI.color; GUI.color = color; GUI.Label(rect, text, style); GUI.color = previous;
        }
    }
}
