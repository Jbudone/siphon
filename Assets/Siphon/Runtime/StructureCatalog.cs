using UnityEngine;

namespace Siphon
{
    public static class StructureCatalog
    {
        public static readonly EntityKind[] Buildable = { EntityKind.Turret, EntityKind.Generator, EntityKind.Outpost,
            EntityKind.Wall, EntityKind.Processor, EntityKind.Trap, EntityKind.Radar, EntityKind.ForceField, EntityKind.SiegeCannon };
        public static string Name(EntityKind kind) => kind == EntityKind.ForceField ? "Force field" : kind == EntityKind.Processor ? "Smelter"
            : kind == EntityKind.Outpost ? "Forward outpost" : kind == EntityKind.Wall ? "Armor wall" : kind == EntityKind.Trap ? "Shock trap"
            : kind == EntityKind.SiegeCannon ? "Siege battery" : kind.ToString();
        public static ResourceBag Cost(EntityKind kind)
        {
            switch (kind)
            {
                case EntityKind.Turret: return new ResourceBag(30, 8);
                case EntityKind.Generator: return new ResourceBag(25, 8);
                case EntityKind.Outpost: return new ResourceBag(50, 15);
                case EntityKind.Wall: return new ResourceBag(15, 10);
                case EntityKind.Processor: return new ResourceBag(35, 15);
                case EntityKind.Trap: return new ResourceBag(20, 5);
                case EntityKind.Radar: return new ResourceBag(25, crystal: 2, alloy: 2);
                case EntityKind.ForceField: return new ResourceBag(35, crystal: 2, alloy: 4);
                case EntityKind.SiegeCannon: return new ResourceBag(60, 15, alloy: 6);
                default: return default;
            }
        }
        public static Vector3 Size(EntityKind kind)
        {
            switch (kind)
            {
                case EntityKind.Outpost: return new Vector3(5, 4, 5);
                case EntityKind.Wall: return new Vector3(8, 3, 1);
                case EntityKind.ForceField: return new Vector3(10, 4, 1);
                case EntityKind.Generator: return new Vector3(3, 3, 3);
                case EntityKind.Trap: return new Vector3(2, 0.5f, 2);
                case EntityKind.Processor: return new Vector3(4, 3, 3);
                case EntityKind.Radar: return new Vector3(3, 5, 3);
                case EntityKind.Core: return new Vector3(5, 11, 5);
                case EntityKind.SiegeCannon: return new Vector3(6, 4, 6);
                default: return new Vector3(3, 3, 3);
            }
        }
        public static string Description(EntityKind kind)
        {
            switch (kind)
            {
                case EntityKind.Turret: return "Directional projectile defense. Flank its 150° firing arc. Needs power.";
                case EntityKind.Generator: return "Powers structures within 35m and core shielding within 40m. Exposed weak point.";
                case EntityKind.Outpost: return "A powered forward respawn. Choose a position with cover and a safe exit.";
                case EntityKind.Wall: return "Physical cover. Block firing lanes; leave room for friendly guns. No power required.";
                case EntityKind.Processor: return "Every 12s: 4 ferrite + 2 timber → 1 alloy. Needs power and leaves a supply reserve.";
                case EntityKind.Trap: return "Powered proximity burst. 50 damage and knockback; rearms after 8s.";
                case EntityKind.Radar: return "Powered sensor. Reports last-seen hostile contacts every 3s within 110m.";
                case EntityKind.SiegeCannon: return "Marks → arcing shells, 35–230m. 2 Fe + 1 alloy per shot. Strong against structures; exposed while charging.";
                default: return "Blocks hostile fire while charged. Walk or jet through it; flank the side control unit or hack it.";
            }
        }
        public static float Health(EntityKind kind) => kind == EntityKind.Core ? 1200 : kind == EntityKind.Generator ? 180
            : kind == EntityKind.Outpost ? 240 : kind == EntityKind.Wall ? 220 : kind == EntityKind.Processor ? 180 : kind == EntityKind.SiegeCannon ? 200 : 120;

        public static Transform Visuals(Transform root, EntityKind kind, Material team, Material dark, bool collision)
        {
            Transform Part(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, bool solid = true)
                => WarDirector.Part(root, type, name, position, scale, material, collision && solid);
            if (kind == EntityKind.Core)
            {
                Part(PrimitiveType.Cylinder, "Core housing", Vector3.up * 2, new Vector3(5, 2, 5), dark);
                Part(PrimitiveType.Sphere, "Core reactor", Vector3.up * 4, Vector3.one * 3, team);
                Part(PrimitiveType.Cylinder, "Core spire", Vector3.up * 8, new Vector3(0.7f, 3, 0.7f), team);
            }
            else if (kind == EntityKind.Generator)
            {
                Part(PrimitiveType.Cube, "Generator", Vector3.up, new Vector3(3, 2, 2.5f), dark);
                Part(PrimitiveType.Cylinder, "Power cell", Vector3.up * 2, new Vector3(1.5f, 0.6f, 1.5f), team);
            }
            else if (kind == EntityKind.Outpost)
            {
                Part(PrimitiveType.Cube, "Spawn pad", Vector3.up * 0.3f, new Vector3(5, 0.6f, 5), dark);
                Part(PrimitiveType.Cylinder, "Spawn beacon", Vector3.up * 2, new Vector3(1, 2, 1), team);
            }
            else if (kind == EntityKind.Wall)
            {
                Part(PrimitiveType.Cube, "Armor wall", Vector3.up * 1.5f, new Vector3(8, 3, 1), dark);
                Part(PrimitiveType.Cube, "Wall trim", Vector3.up * 2.8f, new Vector3(8.1f, 0.2f, 1.1f), team, false);
            }
            else if (kind == EntityKind.ForceField)
            {
                Part(PrimitiveType.Cube, "Control unit", new Vector3(-4.5f, 1, 0), new Vector3(1, 2, 1), dark);
                Part(PrimitiveType.Cube, "Control light", new Vector3(-4.5f, 2.2f, 0), new Vector3(0.7f, 0.4f, 0.7f), team, false);
                Part(PrimitiveType.Cylinder, "Field mast", new Vector3(4.5f, 2, 0), new Vector3(0.3f, 2, 0.3f), team);
                if (!collision) Part(PrimitiveType.Cube, "Barrier preview", Vector3.up * 2, new Vector3(8, 4, 0.05f), team, false);
                if (collision)
                {
                    var field = new GameObject("Barrier surface"); field.transform.SetParent(root, false);
                    var shape = field.AddComponent<BoxCollider>(); shape.isTrigger = true;
                    shape.center = new Vector3(0, 2, 0); shape.size = new Vector3(8, 4, 0.25f);
                }
            }
            else if (kind == EntityKind.SiegeCannon)
            {
                Part(PrimitiveType.Cube, "Siege foundation", Vector3.up * 0.4f, new Vector3(5, 0.8f, 5), dark);
                Part(PrimitiveType.Cylinder, "Siege swivel", Vector3.up * 1.4f, new Vector3(2.5f, 1, 2.5f), team);
                var gun = new GameObject("Siege barrel").transform; gun.SetParent(root, false); gun.localPosition = Vector3.up * 2.8f;
                WarDirector.Part(gun, PrimitiveType.Cube, "Cannon", Vector3.forward * 1.3f, new Vector3(0.8f, 0.8f, 3.2f), dark, collision);
                WarDirector.Part(gun, PrimitiveType.Cylinder, "Charge coil", Vector3.forward * 2.2f, new Vector3(1.1f, 0.3f, 1.1f), team, false).localRotation = Quaternion.Euler(90, 0, 0);
                return gun;
            }
            else if (kind == EntityKind.Processor)
            {
                Part(PrimitiveType.Cube, "Smelter", Vector3.up, new Vector3(4, 2, 3), dark);
                Part(PrimitiveType.Cylinder, "Exhaust", new Vector3(-1, 2.2f, 0), new Vector3(0.8f, 0.8f, 0.8f), team);
                Part(PrimitiveType.Cube, "Conveyor", new Vector3(2.3f, 0.7f, 0), new Vector3(1.5f, 0.3f, 1), team);
            }
            else if (kind == EntityKind.Radar)
            {
                Part(PrimitiveType.Cylinder, "Radar mast", Vector3.up * 2, new Vector3(0.4f, 2, 0.4f), dark);
                var dish = Part(PrimitiveType.Cylinder, "Radar dish", Vector3.up * 4, new Vector3(2.5f, 0.2f, 2.5f), team);
                dish.localRotation = Quaternion.Euler(55, 0, 0);
            }
            else if (kind == EntityKind.Trap)
            {
                Part(PrimitiveType.Cylinder, "Shock plate", Vector3.up * 0.15f, new Vector3(2, 0.15f, 2), dark);
                Part(PrimitiveType.Sphere, "Charge", Vector3.up * 0.4f, new Vector3(0.7f, 0.2f, 0.7f), team, false);
            }
            else
            {
                Part(PrimitiveType.Cylinder, "Turret pedestal", Vector3.up, new Vector3(2, 1, 2), dark);
                var barrel = new GameObject("Turret head").transform;
                barrel.SetParent(root, false); barrel.localPosition = Vector3.up * 2.7f;
                WarDirector.Part(barrel, PrimitiveType.Cube, "Gun", Vector3.forward * 0.6f, new Vector3(0.8f, 0.6f, 2), team, collision);
                return barrel;
            }
            return null;
        }
    }
}
