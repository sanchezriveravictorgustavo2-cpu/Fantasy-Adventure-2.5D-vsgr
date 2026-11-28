using UnityEngine;

namespace Game.Characters.Archer
{
    /// <summary>
    /// Controls Animator parameters for the Defensive Archer unit (Idle, Walk, Attack, Hurt, Death).
    /// </summary>
    [DisallowMultipleComponent]
    public class ArcherAnimatorController : MonoBehaviour
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
    }
}
