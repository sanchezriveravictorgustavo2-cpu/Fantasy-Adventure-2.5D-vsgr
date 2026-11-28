using UnityEngine;

namespace Game.Characters.Explorer
{
    /// <summary>
    /// Controls Animator parameters for the Explorer unit (Idle, Walk, Attack, Hurt, Death).
    /// </summary>
    [DisallowMultipleComponent]
    public class ExplorerAnimatorController : MonoBehaviour
    {
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int DieHash = Animator.StringToHash("Die");

        [SerializeField] private Animator animator;
        [SerializeField] private float speedDampTime = 0.08f;

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

        public void TriggerAttack()
        {
            if (animator != null)
            {
                animator.SetTrigger(AttackHash);
            }
        }

        public void TriggerHurt()
        {
            if (animator != null)
            {
                animator.SetTrigger(HurtHash);
            }
        }

        public void TriggerDeath()
        {
            if (animator != null)
            {
                animator.SetTrigger(DieHash);
            }
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
            if (info.IsName("Attack")) return "Attack";
            if (info.IsName("Hurt")) return "Hurt";
            if (info.IsName("Death")) return "Death";
            return "Other";
        }
    }
}
