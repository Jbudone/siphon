using UnityEngine;

namespace Siphon
{
    // Presentation only: combat continues to aim and fire from the camera.
    public sealed class WeaponPresentation : MonoBehaviour
    {
        public GameObject pulse;
        public GameObject disc;
        private PlayerCombat combat;

        private void Start() => combat = GetComponentInParent<PlayerCombat>();

        private void LateUpdate()
        {
            if (combat == null) return;
            bool rifle = combat.Weapon == 0;
            if (pulse.activeSelf != rifle) pulse.SetActive(rifle);
            if (disc.activeSelf == rifle) disc.SetActive(!rifle);
        }
    }
}
