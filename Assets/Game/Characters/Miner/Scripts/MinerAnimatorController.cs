using System;
using UnityEngine;

namespace Game.Characters.Miner
{
    /// <summary>
    /// Controls Animator parameters for Miner_Collector and relays animation events
    /// (such as OnMiningHit from Mine_Loop) to gameplay listeners.
    /// </summary>
    [DisallowMultipleComponent]
    public class MinerAnimatorController : MonoBehaviour
    {
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int IsMiningHash = Animator.StringToHash("IsMining");
        private static readonly int IsCarryingHash = Animator.StringToHash("IsCarrying");
        private static readonly int CollectHash = Animator.StringToHash("Collect");
        private static readonly int DepositHash = Animator.StringToHash("Deposit");
        private static readonly int DieHash = Animator.StringToHash("Die");

        private static readonly int MineLoopStateHash = Animator.StringToHash("Mine_Loop");

        [SerializeField] private Animator animator;
        [SerializeField] private float speedDampTime = 0.08f;

        public event Action OnMiningHitEvent;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        public void SetAnimator(Animator targetAnimator)
        {
            animator = targetAnimator;
        }

        public void SetMoveSpeed(float speed)
        {
            if (animator == null)
            {
                return;
            }

            if (speed <= 0.01f)
            {
                animator.SetFloat(MoveSpeedHash, 0f);
            }
            else
            {
                animator.SetFloat(MoveSpeedHash, speed, speedDampTime, Time.deltaTime);
            }
        }

        public void SetMining(bool isMining)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetBool(IsMiningHash, isMining);
        }

        public void SetCarrying(bool isCarrying)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetBool(IsCarryingHash, isCarrying);
        }

        public void TriggerCollect()
        {
            if (animator != null)
            {
                animator.SetTrigger(CollectHash);
            }
        }

        public void TriggerDeposit()
        {
            if (animator != null)
            {
                animator.SetTrigger(DepositHash);
            }
        }

        public void TriggerDeath()
        {
            if (animator != null)
            {
                animator.SetTrigger(DieHash);
            }
        }

        public bool IsInMineLoopState()
        {
            if (animator == null)
            {
                return false;
            }

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            return info.shortNameHash == MineLoopStateHash;
        }

        public string GetCurrentStateName()
        {
            if (animator == null)
            {
                return "None";
            }

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("Idle")) return "Idle";
            if (info.IsName("Walk")) return "Walk";
            if (info.IsName("Run")) return "Run";
            if (info.IsName("Mine_Start")) return "Mine_Start";
            if (info.IsName("Mine_Loop")) return "Mine_Loop";
            if (info.IsName("Mine_End")) return "Mine_End";
            if (info.IsName("Collect")) return "Collect";
            if (info.IsName("Carry")) return "Carry";
            if (info.IsName("Deposit")) return "Deposit";
            if (info.IsName("Death")) return "Death";
            return "Other";
        }

        /// <summary>
        /// Invoked by the AnimationEvent placed at the strike impact frame of Mine_Loop.
        /// </summary>
        public void OnMiningHit()
        {
            OnMiningHitEvent?.Invoke();
        }
    }
}
