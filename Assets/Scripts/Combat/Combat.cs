using UnityEngine;

namespace Ashveil
{
    public enum Faction { Concord, Tharne, Neutral }
    public enum UnitKind { Fighter, Tank, Turret, Radar, Structure }

    public class CombatUnit : MonoBehaviour
    {
        public string DisplayName;
        public Faction Faction;
        public UnitKind Kind;
        public float MaxHealth = 100f;
        public float Health;
        public float Armor = 0.1f;
        public bool Regenerates;
        public float RegenPerSec = 4f;
        public bool IsPlayer;
        public bool Objective;
        public bool Airborne = true;
        public float Radius = 4f;

        public bool Alive => Health > 0f;
        public float Health01 => Mathf.Clamp01(Health / MaxHealth);

        void Awake()
        {
            if (Health <= 0f) Health = MaxHealth;
        }

        void Update()
        {
            if (!Alive || !Regenerates) return;
            Health = Mathf.Min(MaxHealth, Health + RegenPerSec * Time.deltaTime);
        }

        public void ApplyDamage(float amount, CombatUnit attacker)
        {
            if (!Alive) return;
            Health -= amount * (1f - Armor);
            if (Health <= 0f)
            {
                Health = 0f;
                Die();
            }
        }

        void Die()
        {
            Fx.Explosion(transform.position, Kind == UnitKind.Fighter ? 8f : 14f);
            SoundBank.PlayExplosion(transform.position);
            if (IsPlayer)
            {
                GameSession.I.OnPlayerDestroyed();
                gameObject.SetActive(false);
                return;
            }
            Destroy(gameObject);
        }
    }

    public class Projectile : MonoBehaviour
    {
        public Vector3 Velocity;
        public float Damage = 12f;
        public Faction OwnerFaction;
        public CombatUnit Owner;
        public float Life = 3.2f;
        public Color Color;
        float _age;

        public static Projectile Spawn(Vector3 pos, Vector3 vel, float damage, CombatUnit owner, Color color, float scale = 0.35f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Bolt";
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            Object.Destroy(go.GetComponent<Collider>());
            Palette.ApplyColor(go.GetComponent<Renderer>(), color);
            var light = go.AddComponent<Light>();
            light.color = color;
            light.range = 18f;
            light.intensity = 2.4f;
            var p = go.AddComponent<Projectile>();
            p.Velocity = vel;
            p.Damage = damage;
            p.Owner = owner;
            p.OwnerFaction = owner != null ? owner.Faction : Faction.Neutral;
            p.Color = color;
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.startWidth = scale * 0.8f;
            trail.endWidth = 0f;
            trail.material = Palette.Colored(color);
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            return p;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            transform.position += Velocity * dt;
            if (_age > Life)
            {
                Destroy(gameObject);
                return;
            }

            if (Physics.SphereCast(transform.position - Velocity * dt, 0.6f, Velocity.normalized, out var hit, Velocity.magnitude * dt + 0.6f, ~0, QueryTriggerInteraction.Ignore))
            {
                var unit = hit.collider.GetComponentInParent<CombatUnit>();
                if (unit != null && unit.Alive && unit.Faction != OwnerFaction)
                {
                    unit.ApplyDamage(Damage, Owner);
                    Fx.Spark(hit.point);
                    Destroy(gameObject);
                    return;
                }
                if (unit == null)
                {
                    Fx.Spark(hit.point);
                    Destroy(gameObject);
                }
            }
        }
    }

    public class Missile : MonoBehaviour
    {
        public CombatUnit Target;
        public CombatUnit Owner;
        public float Speed = 160f;
        public float Turn = 220f;
        public float Damage = 55f;
        public float Life = 8f;
        float _age;

        public static Missile Spawn(Vector3 pos, Vector3 dir, CombatUnit target, CombatUnit owner)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Missile";
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(dir);
            go.transform.localScale = new Vector3(0.18f, 0.45f, 0.18f);
            Object.Destroy(go.GetComponent<Collider>());
            Palette.ApplyColor(go.GetComponent<Renderer>(), Palette.Missile);
            var m = go.AddComponent<Missile>();
            m.Target = target;
            m.Owner = owner;
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.55f;
            trail.startWidth = 0.35f;
            trail.endWidth = 0f;
            trail.material = Palette.Colored(new Color(1f, 0.6f, 0.15f));
            var light = go.AddComponent<Light>();
            light.color = Palette.Missile;
            light.range = 12f;
            light.intensity = 1.8f;
            return m;
        }

        void Update()
        {
            _age += Time.deltaTime;
            if (_age > Life || (Target != null && !Target.Alive))
            {
                if (Target == null || !Target.Alive) { Destroy(gameObject); return; }
            }

            Vector3 desired = transform.forward;
            if (Target != null && Target.Alive)
                desired = (Target.transform.position - transform.position).normalized;

            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(desired), Turn * Time.deltaTime);
            transform.position += transform.forward * Speed * Time.deltaTime;

            if (Target != null && Vector3.Distance(transform.position, Target.transform.position) < Target.Radius + 1.5f)
            {
                Target.ApplyDamage(Damage, Owner);
                Fx.Explosion(transform.position, 6f);
                SoundBank.PlayExplosion(transform.position);
                Destroy(gameObject);
                return;
            }

            if (Physics.Raycast(transform.position, transform.forward, out var hit, Speed * Time.deltaTime + 1f))
            {
                var unit = hit.collider.GetComponentInParent<CombatUnit>();
                if (unit != null && unit.Faction != (Owner != null ? Owner.Faction : Faction.Neutral))
                    unit.ApplyDamage(Damage, Owner);
                Fx.Explosion(hit.point, 5f);
                Destroy(gameObject);
            }
        }
    }

    public static class Fx
    {
        public static void Explosion(Vector3 pos, float size)
        {
            var go = new GameObject("Boom");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.startLifetime = 0.7f;
            main.startSpeed = size * 3f;
            main.startSize = size * 0.35f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.7f, 0.2f), new Color(1f, 0.2f, 0.05f));
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.4f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = Palette.Particle;
            renderer.material.color = new Color(1f, 0.5f, 0.1f);
            Object.Destroy(go, 1.4f);

            var flash = new GameObject("Flash");
            flash.transform.position = pos;
            var light = flash.AddComponent<Light>();
            light.color = new Color(1f, 0.65f, 0.25f);
            light.intensity = 8f;
            light.range = size * 6f;
            Object.Destroy(flash, 0.25f);
        }

        public static void Spark(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.5f;
            Object.Destroy(go.GetComponent<Collider>());
            Palette.ApplyColor(go.GetComponent<Renderer>(), Color.white);
            Object.Destroy(go, 0.08f);
        }
    }
}
