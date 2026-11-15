using System;

namespace Siphon
{
    public enum ResourceKind { Ferrite, Timber, Crystal, Alloy }
    [Serializable]
    public struct ResourceBag
    {
        public int ferrite, timber, crystal, alloy;
        public int Total => ferrite + timber + crystal + alloy;
        public ResourceBag(int ferrite = 0, int timber = 0, int crystal = 0, int alloy = 0)
        { this.ferrite = ferrite; this.timber = timber; this.crystal = crystal; this.alloy = alloy; }
        public int Count(ResourceKind kind) => kind == ResourceKind.Ferrite ? ferrite : kind == ResourceKind.Timber ? timber : kind == ResourceKind.Crystal ? crystal : alloy;
        public bool Valid => ferrite >= 0 && timber >= 0 && crystal >= 0 && alloy >= 0;
        public bool Covers(ResourceBag cost) => cost.Valid && ferrite >= cost.ferrite && timber >= cost.timber && crystal >= cost.crystal && alloy >= cost.alloy;
        public void Add(ResourceKind kind, int amount)
        {
            int value = Math.Clamp(Count(kind) + amount, 0, 100000);
            switch (kind) { case ResourceKind.Ferrite: ferrite = value; break; case ResourceKind.Timber: timber = value; break; case ResourceKind.Crystal: crystal = value; break; default: alloy = value; break; }
        }
        public void Add(ResourceBag other)
        { foreach (ResourceKind kind in Enum.GetValues(typeof(ResourceKind))) Add(kind, other.Count(kind)); }
        public bool Spend(ResourceBag cost)
        {
            if (!Covers(cost)) return false;
            ferrite -= cost.ferrite; timber -= cost.timber; crystal -= cost.crystal; alloy -= cost.alloy;
            return true;
        }
        public ResourceBag Half() => new ResourceBag(ferrite / 2, timber / 2, crystal / 2, alloy / 2);
        public override string ToString()
        {
            string value = "";
            void Part(int amount, string label) { if (amount > 0) value += (value.Length > 0 ? " · " : "") + amount + " " + label; }
            Part(ferrite, "Fe"); Part(timber, "wood"); Part(crystal, "crystal"); Part(alloy, "alloy");
            return value.Length > 0 ? value : "empty";
        }
    }
}
