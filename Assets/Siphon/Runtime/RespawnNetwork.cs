using UnityEngine;

namespace Siphon
{
    public enum SpawnPreference { Automatic, Home, Outpost }
    [RequireComponent(typeof(WarDirector))]
    public sealed class RespawnNetwork : MonoBehaviour
    {
        public SpawnPreference Preference { get; private set; }
        public Vector3 SelectedPosition { get; private set; }
        private WarDirector war;
        private void Awake() { war = GetComponent<WarDirector>(); }
        public void ChooseAutomatic() { Preference = SpawnPreference.Automatic; war.Announce("Respawns: any available powered outpost, otherwise home."); }
        public bool Choose(Combatant station)
        {
            if (war.Finished || station == null || !station.Alive || station.team != Team.Blue) return false;
            if (station.kind == EntityKind.Core) { Preference = SpawnPreference.Home; war.Announce("Future respawns return to the main base."); return true; }
            if (!Available(station, out _)) return false;
            Preference = SpawnPreference.Outpost; SelectedPosition = station.transform.position;
            war.Announce("Forward spawn selected. If power or exits fail, another available base will be used.");
            return true;
        }
        public void Restore(SpawnPreference preference, Vector3 position)
        { Preference = System.Enum.IsDefined(typeof(SpawnPreference), preference) ? preference : SpawnPreference.Automatic; SelectedPosition = position; }
        public Vector3 Resolve(Team team, bool player)
        {
            Vector3 home = team == Team.Blue ? war.blueSpawn : war.redSpawn;
            if (player && team == Team.Blue)
            {
                if (Preference == SpawnPreference.Home) return home;
                if (Preference == SpawnPreference.Outpost)
                    foreach (var station in Combatant.All)
                        if (station.team == team && (station.transform.position - SelectedPosition).sqrMagnitude < 0.1f && Available(station, out Vector3 selected)) return selected;
            }
            foreach (var station in Combatant.All)
                if (station.team == team && Available(station, out Vector3 exit)) return exit;
            return home;
        }
        public bool IsSelected(Combatant station) => station != null &&
            (station.kind == EntityKind.Core ? Preference == SpawnPreference.Home : Preference == SpawnPreference.Outpost && (station.transform.position - SelectedPosition).sqrMagnitude < 0.1f);
        public bool Available(Combatant station, out Vector3 exit)
        {
            exit = default;
            return station != null && station.Alive && station.kind == EntityKind.Outpost
                && war.IsPowered(station.team, station.transform.position, 35) && TryExit(station.transform.position, out exit);
        }
        public static bool TryExit(Vector3 origin, out Vector3 exit)
        {
            for (int ring = 0; ring < 2; ring++)
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI / 4;
                    Vector3 point = origin + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (6 + ring * 3);
                    if (Mathf.Abs(point.x) > 295 || Mathf.Abs(point.z) > 295 || !Physics.Raycast(new Vector3(point.x, 150, point.z), Vector3.down,
                        out RaycastHit ground, 300, 1 << 9, QueryTriggerInteraction.Ignore) || ground.normal.y < 0.65f) continue;
                    point = ground.point + Vector3.up * 0.15f;
                    if (Physics.CheckCapsule(point + Vector3.up * 0.5f, point + Vector3.up * 2f, 0.5f, ~(1 << 9), QueryTriggerInteraction.Ignore)) continue;
                    exit = point; return true;
                }
            exit = default; return false;
        }
    }
}
