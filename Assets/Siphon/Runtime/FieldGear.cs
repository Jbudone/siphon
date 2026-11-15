using UnityEngine;

namespace Siphon
{
    public enum Upgrade { ThermalLining, SurveyOptics, FieldEngineering, CloakWeave, SiegeCalibration }
    public static class GearCatalog
    {
        public static string Name(Upgrade upgrade) => upgrade == Upgrade.ThermalLining ? "Thermal lining" : upgrade == Upgrade.SurveyOptics ? "Survey optics"
            : upgrade == Upgrade.FieldEngineering ? "Field engineering" : upgrade == Upgrade.CloakWeave ? "Cloak weave" : "Siege calibration";
        public static readonly Upgrade[] Research = { Upgrade.ThermalLining, Upgrade.SurveyOptics, Upgrade.FieldEngineering, Upgrade.CloakWeave, Upgrade.SiegeCalibration };
        public static string Prerequisite(Upgrade upgrade) => upgrade == Upgrade.SiegeCalibration ? "Survey optics + field engineering" : "";
        public static bool RequirementsMet(Upgrade upgrade, FieldGear gear) => upgrade != Upgrade.SiegeCalibration || gear.surveyOptics && gear.fieldEngineering;
        public static ResourceBag Cost(Upgrade upgrade) => upgrade == Upgrade.ThermalLining ? new ResourceBag(timber: 8, crystal: 2)
            : upgrade == Upgrade.SurveyOptics ? new ResourceBag(20, crystal: 4) : upgrade == Upgrade.FieldEngineering ? new ResourceBag(crystal: 4, alloy: 5)
            : upgrade == Upgrade.CloakWeave ? new ResourceBag(15, crystal: 6, alloy: 3) : new ResourceBag(20, crystal: 4, alloy: 5);
        public static string Description(Upgrade upgrade) => upgrade == Upgrade.ThermalLining
            ? "Insulation prevents frost exposure in the eastern icefields."
            : upgrade == Upgrade.SurveyOptics ? "Unlock radar construction to reveal short-lived hostile contact snapshots."
            : upgrade == Upgrade.FieldEngineering ? "Unlock force-field construction. Requires alloy from a powered smelter."
            : upgrade == Upgrade.CloakWeave ? "C toggles visual concealment beyond 12m. Radar still detects you; firing, working or taking damage breaks cloak."
            : "Unlock siege batteries. Requires survey optics and field engineering. Mark a target with middle mouse.";
    }
    public sealed class FieldGear : MonoBehaviour
    {
        public bool thermalLining, surveyOptics, fieldEngineering, wardenCapacitor, cloakWeave, siegeCalibration;
        public bool Cloaked { get; private set; }
        public float CloakEnergy { get; private set; } = 1;
        private float revealUntil;
        public float Exposure { get; private set; }
        public string Biome => transform.position.x > 120 ? "ICEFIELD" : transform.position.x < -110 ? "OLD GROWTH" : "FRONTIER";
        public bool Sheltered { get; private set; }
        private PlayerCombat combat;
        private float nextTick;
        private void Awake()
        {
            combat = GetComponent<PlayerCombat>();
            GetComponent<Combatant>().Damaged += amount => BreakCloak();
        }
        public bool Has(Upgrade upgrade) => upgrade == Upgrade.ThermalLining ? thermalLining : upgrade == Upgrade.SurveyOptics ? surveyOptics : upgrade == Upgrade.FieldEngineering ? fieldEngineering : upgrade == Upgrade.CloakWeave ? cloakWeave : upgrade == Upgrade.SiegeCalibration && siegeCalibration;
        public bool CanBuild(EntityKind kind) => kind != EntityKind.Radar && kind != EntityKind.ForceField && kind != EntityKind.SiegeCannon
            || kind == EntityKind.Radar && surveyOptics || kind == EntityKind.ForceField && fieldEngineering || kind == EntityKind.SiegeCannon && siegeCalibration;
        public bool AtWorkbench()
        {
            foreach (var actor in Combatant.All)
                if (actor.Alive && actor.team == combat.Entity.team && (actor.kind == EntityKind.Core || actor.kind == EntityKind.Outpost || actor.kind == EntityKind.Processor)
                    && Vector3.Distance(actor.transform.position, transform.position) < 12) return true;
            return false;
        }
        public bool Craft(Upgrade upgrade)
        {
            if (!System.Enum.IsDefined(typeof(Upgrade), upgrade) || Has(upgrade) || !GearCatalog.RequirementsMet(upgrade, this) || !combat.Entity.Alive || !AtWorkbench()) return false;
            if (!WarDirector.Instance.Spend(combat.Entity.team, GearCatalog.Cost(upgrade))) return false;
            BreakCloak(); WarDirector.Instance.Noise(transform.position, combat.Entity.team, 50);
            if (upgrade == Upgrade.ThermalLining) thermalLining = true;
            else if (upgrade == Upgrade.SurveyOptics) surveyOptics = true;
            else if (upgrade == Upgrade.FieldEngineering) fieldEngineering = true;
            else if (upgrade == Upgrade.CloakWeave) cloakWeave = true;
            else siegeCalibration = true;
            WarDirector.Instance.Announce(GearCatalog.Name(upgrade) + " crafted. Equipment persists through respawns.");
            return true;
        }
        public bool ToggleCloak()
        {
            if (!cloakWeave || !combat.Entity.Alive || WarDirector.Instance == null || WarDirector.Instance.Finished) return false;
            if (Cloaked) { Cloaked = false; return true; }
            if (CloakEnergy < 0.2f || Time.time < revealUntil) return false;
            Cloaked = true; return true;
        }
        public void BreakCloak() { Cloaked = false; revealUntil = Time.time + 3; }
        public bool VisibleFrom(Vector3 observer) => !Cloaked || (observer - transform.position).sqrMagnitude <= 144;
        public void EquipCapacitor()
        {
            if (wardenCapacitor) return;
            wardenCapacitor = true; GetComponent<PlaygroundPlayer>().JetCapacityMultiplier = 1.25f;
            WarDirector.Instance.Announce("Warden capacitor equipped: 25% more jet fuel. Base acceleration and weapon damage are unchanged.", 6);
        }
        public void RestoreEquipment(bool capacitor)
        { wardenCapacitor = capacitor; GetComponent<PlaygroundPlayer>().JetCapacityMultiplier = capacitor ? 1.25f : 1; }
        private void Update()
        {
            if (combat != null && combat.Menu == 0 && combat.GetComponent<PlaygroundPlayer>().ControlsActive && Input.GetKeyDown(KeyCode.C))
            {
                if (!ToggleCloak() && WarDirector.Instance != null)
                    WarDirector.Instance.Announce(cloakWeave ? "Cloak recharging or signature exposed. Wait, then press C." : "Craft cloak weave at a workbench (T).");
            }
            if (Cloaked)
            {
                CloakEnergy = Mathf.Max(0, CloakEnergy - Time.deltaTime / 8);
                if (CloakEnergy <= 0) Cloaked = false;
            }
            else if (Time.time >= revealUntil) CloakEnergy = Mathf.Min(1, CloakEnergy + Time.deltaTime / 12);
            if (combat == null || !combat.Entity.Alive || WarDirector.Instance == null || WarDirector.Instance.Finished)
            { Exposure = 0; Cloaked = false; return; }
            if (Time.time < nextTick) return;
            nextTick = Time.time + 1;
            Sheltered = false;
            foreach (var actor in Combatant.All)
                if (actor.Alive && actor.team == combat.Entity.team && (actor.kind == EntityKind.Core || actor.kind == EntityKind.Outpost)
                    && (actor.transform.position - transform.position).sqrMagnitude < 144) { Sheltered = true; break; }
            bool cold = transform.position.x > 120 && !thermalLining && !Sheltered;
            Exposure = Mathf.Clamp01(Exposure + (cold ? 1f / 90 : -1f / 20));
            if (Exposure >= 1) combat.Entity.Damage(4, Team.Wild, transform.position + Vector3.up * 10);
        }
    }
}
