using UnityEngine;

namespace Game.Systems.Combat
{
    /// <summary>
    /// Stylized ranged arrow projectile fired by Archers.
    /// Respects faction rules so allied arrows can never damage allied units or Player_Statue.
    /// </summary>
    public class ArcherProjectile : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float speed = 18f;
        [SerializeField, Min(0f)] private float arcHeight = 1.6f;
        [SerializeField, Min(0.5f)] private float maxLifetime = 4f;
        [SerializeField, Min(0.1f)] private float hitRadius = 0.6f;

        private TeamFaction attackerFaction = TeamFaction.Ally;
        private float damage = 12f;
        private ICombatTarget target;
        private Vector3 startPos;
        private Vector3 lastTargetPos;
        private float flightDuration;
        private float elapsed;
        private bool launched;

        public void Launch(
            Vector3 origin,
            ICombatTarget combatTarget,
            float projectileDamage,
            TeamFaction faction,
            float projectileSpeed = 18f)
        {
            startPos = origin;
            target = combatTarget;
            damage = projectileDamage;
            attackerFaction = faction;
            speed = Mathf.Max(2f, projectileSpeed);

            lastTargetPos = target != null && target.TargetTransform != null
                ? target.TargetPosition
                : origin + (faction == TeamFaction.Ally ? Vector3.right : Vector3.left) * 12f;

            float dist = Vector3.Distance(startPos, lastTargetPos);
            flightDuration = Mathf.Clamp(dist / speed, 0.12f, maxLifetime);
            elapsed = 0f;
            launched = true;

            transform.position = startPos;
            Vector3 dir = (lastTargetPos - startPos);
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }

        private void Update()
        {
            if (!launched)
            {
                return;
            }

            if (target != null && target.IsAlive && target.TargetTransform != null)
            {
                lastTargetPos = target.TargetPosition;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, flightDuration));

            Vector3 prevPos = transform.position;
            Vector3 linearPos = Vector3.Lerp(startPos, lastTargetPos, t);
            float parabola = 4f * arcHeight * t * (1f - t);
            Vector3 nextPos = linearPos + Vector3.up * parabola;

            transform.position = nextPos;

            Vector3 velocity = nextPos - prevPos;
            if (velocity.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            }

            if (t >= 1f || Vector3.Distance(nextPos, lastTargetPos) <= hitRadius)
            {
                ApplyImpact();
            }
        }

        private void ApplyImpact()
        {
            if (target != null && target.IsAlive && target.Faction != attackerFaction)
            {
                target.TakeDamage(damage, attackerFaction);
            }

            Destroy(gameObject);
        }
    }
}
