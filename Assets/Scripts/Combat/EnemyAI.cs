using UnityEngine;

namespace Ashveil
{
    public class EnemyFighterAI : MonoBehaviour
    {
        public CombatUnit Unit;
        public CraftController Craft;
        public CombatUnit Prey;
        float _fireCd;
        float _repath;

        public void Init(CombatUnit unit, CraftController craft)
        {
            Unit = unit;
            Craft = craft;
            craft.PlayerControlled = false;
        }

        void Update()
        {
            if (Unit == null || !Unit.Alive || GameSession.I.Paused) return;
            if (Prey == null || !Prey.Alive) Prey = GameSession.I.PlayerUnit;
            if (Prey == null) return;

            float dt = Time.deltaTime;
            Vector3 to = Prey.transform.position - transform.position;
            float dist = to.magnitude;
            Vector3 desiredDir = to.normalized;
            if (dist < 90f) desiredDir = Quaternion.Euler(0, 70f, 0) * desiredDir;
            if (transform.position.y < WorldBuilder.Height(transform.position.x, transform.position.z) + 40f)
                desiredDir = (desiredDir + Vector3.up).normalized;

            Quaternion want = Quaternion.LookRotation(desiredDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 80f * dt);
            Craft.Throttle = dist > 160f ? 0.9f : 0.55f;

            _fireCd -= dt;
            float ang = Vector3.Angle(transform.forward, to);
            if (_fireCd <= 0f && ang < 12f && dist < 420f)
            {
                Craft.FireAt(to);
                _fireCd = Random.Range(0.18f, 0.42f);
            }
        }
    }

    public class GroundDefense : MonoBehaviour
    {
        public CombatUnit Unit;
        float _cd;

        void Update()
        {
            if (Unit == null || !Unit.Alive || GameSession.I.Paused) return;
            var player = GameSession.I.PlayerUnit;
            if (player == null || !player.Alive) return;
            Vector3 to = player.transform.position - transform.position;
            if (to.magnitude > 520f) return;
            _cd -= Time.deltaTime;
            if (_cd > 0f) return;
            if (Vector3.Angle(Vector3.up, to) < 25f && to.y < 20f) return;
            Vector3 dir = (to + Vector3.up * 2f).normalized;
            Projectile.Spawn(transform.position + Vector3.up * 3f, dir * 260f, 7f, Unit, Palette.TharneAccent, 0.28f);
            _cd = Unit.Kind == UnitKind.Turret ? 0.55f : 1.1f;
        }
    }
}
