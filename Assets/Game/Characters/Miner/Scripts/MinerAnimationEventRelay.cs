using UnityEngine;

namespace Game.Characters.Miner
{
    /// <summary>
    /// Sits on the same GameObject as the Animator (Visual_Model child)
    /// and forwards AnimationEvents up to MinerAnimatorController on the unit root.
    /// </summary>
    [DisallowMultipleComponent]
    public class MinerAnimationEventRelay : MonoBehaviour
    {
        [SerializeField] private MinerAnimatorController animatorController;

        private void Awake()
        {
            if (animatorController == null)
            {
                animatorController = GetComponentInParent<MinerAnimatorController>();
            }
        }

        public void OnMiningHit()
        {
            if (animatorController != null)
            {
                animatorController.OnMiningHit();
            }
        }
    }
}
