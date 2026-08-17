using UnityEngine;

namespace Ashveil
{
    public enum WeaponGroup { Plasma, Cannon, Missiles }

    public class CraftController : MonoBehaviour
    {
        public CombatUnit Unit;
        public Camera CockpitCam;
        public Camera ChaseCam;
        public Transform GunMuzzle;
        public bool PlayerControlled = true;

        public float MaxSpeed = 210f;
        public float AfterburnerSpeed = 290f;
        public float SpeedFollow = 5.5f;
        public float PitchRate = 70f;
        public float YawRate = 55f;
        public float RollRate = 100f;
        public float MouseDeadzone = 0.08f;

        public float Energy = 100f;
        public float EnergyMax = 100f;
        public int CannonAmmo = 400;
        public int Missiles = 6;
        public WeaponGroup Group = WeaponGroup.Plasma;
        public CombatUnit Target;
        public float LockProgress;
        public Vector3 Velocity;
        public float Throttle = 0.45f;
        public int CameraMode;
        public Vector2 MouseOffset;

        float _plasmaHeat;
        float _cannonCd;
        float _missileCd;
        float _repairCd;
        bool _boostTurn;
        float _alt;
        float _speed;
        int _ignoreMouse;

        public float Altitude => _alt;
        public float TasKmh => _speed * 3.6f;
        public float Heading => (transform.eulerAngles.y + 360f) % 360f;
        public float PitchAngle
        {
            get
            {
                var e = transform.eulerAngles;
                float p = e.x;
                if (p > 180f) p -= 360f;
                return -p;
            }
        }

        public void SetupPlayer(Camera cockpit, Camera chase)
        {
            PlayerControlled = true;
            CockpitCam = cockpit;
            ChaseCam = chase;
            MouseOffset = Vector2.zero;
            _ignoreMouse = 3;
            _speed = Throttle * MaxSpeed;
            Velocity = transform.forward * _speed;
            ApplyCamera();
        }

        public void ResetStick()
        {
            MouseOffset = Vector2.zero;
            _ignoreMouse = 3;
        }

        void Update()
        {
            if (!Unit || !Unit.Alive) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (PlayerControlled && GameSession.I.State == GameState.Playing && !GameSession.I.Paused)
                ReadPlayer(dt);

            Integrate(dt);
            Recharge(dt);
            Ground();
            if (PlayerControlled) UpdateLock(dt);
        }

        void LateUpdate()
        {
            if (!PlayerControlled || ChaseCam == null || CameraMode != 1) return;
            Vector3 back = transform.position - transform.forward * 16f + Vector3.up * 4.5f;
            ChaseCam.transform.position = Vector3.Lerp(ChaseCam.transform.position, back, 8f * Time.deltaTime);
            ChaseCam.transform.LookAt(transform.position + transform.forward * 8f + Vector3.up);
        }

        void ReadPlayer(float dt)
        {
            Vector2 delta = _ignoreMouse > 0 ? Vector2.zero : GameInput.MouseDelta;
            if (_ignoreMouse > 0) _ignoreMouse--;
            MouseOffset.x = Mathf.Clamp(MouseOffset.x + delta.x / (Screen.height * 0.38f), -1.2f, 1.2f);
            MouseOffset.y = Mathf.Clamp(MouseOffset.y + delta.y / (Screen.height * 0.38f), -1.2f, 1.2f);
            Vector2 n = MouseOffset;
            if (n.magnitude < MouseDeadzone) n = Vector2.zero;
            else n = n.normalized * ((n.magnitude - MouseDeadzone) / (1.2f - MouseDeadzone));

            _boostTurn = GameInput.Mouse(1);
            float boost = _boostTurn ? 1.55f : 1f;
            transform.Rotate(Vector3.right, -n.y * PitchRate * boost * dt, Space.Self);
            transform.Rotate(Vector3.up, n.x * YawRate * boost * dt, Space.Self);
            AutoLevelRoll(dt);

            float roll = 0f;
            if (GameInput.Key(KeyCode.A) || GameInput.Key(KeyCode.LeftArrow)) roll -= 1f;
            if (GameInput.Key(KeyCode.D) || GameInput.Key(KeyCode.RightArrow)) roll += 1f;
            transform.Rotate(Vector3.forward, -roll * RollRate * dt, Space.Self);

            if (GameInput.Key(KeyCode.W) || GameInput.Key(KeyCode.Equals)) Throttle = Mathf.Min(1f, Throttle + 0.55f * dt);
            if (GameInput.Key(KeyCode.S) || GameInput.Key(KeyCode.Minus)) Throttle = Mathf.Max(0f, Throttle - 0.55f * dt);
            if (GameInput.KeyDown(KeyCode.Backspace)) Throttle = 1f;
            if (GameInput.KeyDown(KeyCode.Alpha0)) Throttle = 0f;

            if (GameInput.KeyDown(KeyCode.Tab))
                Group = (WeaponGroup)(((int)Group + 1) % 3);
            if (GameInput.KeyDown(KeyCode.Alpha1)) Group = WeaponGroup.Plasma;
            if (GameInput.KeyDown(KeyCode.Alpha2)) Group = WeaponGroup.Cannon;
            if (GameInput.KeyDown(KeyCode.Alpha3)) Group = WeaponGroup.Missiles;

            if (GameInput.KeyDown(KeyCode.T)) GameSession.I.SelectNearest(this);
            if (GameInput.KeyDown(KeyCode.V)) GameSession.I.SelectThreat(this);
            if (GameInput.KeyDown(KeyCode.G)) GameSession.I.SelectNearReticle(this);
            if (GameInput.KeyDown(KeyCode.RightBracket)) GameSession.I.CycleTarget(this, 1);
            if (GameInput.KeyDown(KeyCode.LeftBracket)) GameSession.I.CycleTarget(this, -1);

            if (GameInput.KeyDown(KeyCode.F1)) { CameraMode = 0; ApplyCamera(); }
            if (GameInput.KeyDown(KeyCode.F3)) { CameraMode = 1; ApplyCamera(); }

            bool fire = GameInput.Mouse(0) || GameInput.Key(KeyCode.Space);
            if (fire) TryFire();
            if (GameInput.MouseDown(1) && Group == WeaponGroup.Missiles) TryFire();
        }

        void AutoLevelRoll(float dt)
        {
            Vector3 flatRight = Vector3.Cross(Vector3.up, transform.forward);
            if (flatRight.sqrMagnitude < 0.001f) return;
            float bank = Vector3.SignedAngle(flatRight.normalized, transform.right, transform.forward);
            if (!GameInput.Key(KeyCode.A) && !GameInput.Key(KeyCode.D)
                && !GameInput.Key(KeyCode.LeftArrow) && !GameInput.Key(KeyCode.RightArrow))
                transform.Rotate(Vector3.forward, -bank * 1.8f * dt, Space.Self);
        }

        void Integrate(float dt)
        {
            bool afterburner = PlayerControlled && GameInput.Key(KeyCode.LeftShift) && Energy > 1f;
            float cap = afterburner ? AfterburnerSpeed : MaxSpeed;
            if (afterburner) Energy -= 12f * dt;

            float targetSpeed = Throttle * cap;
            if (PlayerControlled && GameInput.Key(KeyCode.LeftControl))
                targetSpeed *= 0.4f;

            _speed = Mathf.MoveTowards(_speed, targetSpeed, 55f * dt);

            Vector3 desired = transform.forward * _speed;
            if (PlayerControlled)
            {
                float sx = 0f, sy = 0f;
                if (GameInput.Key(KeyCode.Q)) sx -= 1f;
                if (GameInput.Key(KeyCode.E)) sx += 1f;
                if (GameInput.Key(KeyCode.R)) sy += 1f;
                if (GameInput.Key(KeyCode.F) || GameInput.Key(KeyCode.X)) sy -= 1f;
                desired += transform.right * sx * 55f;
                desired += transform.up * sy * 45f;
            }

            Velocity = Vector3.Lerp(Velocity, desired, 1f - Mathf.Exp(-SpeedFollow * dt));
            transform.position += Velocity * dt;
        }

        void Ground()
        {
            if (Physics.Raycast(transform.position + Vector3.up * 12f, Vector3.down, out var hit, 8000f, ~0, QueryTriggerInteraction.Ignore))
            {
                _alt = hit.distance - 12f - 1.8f;
                if (_alt < 1.6f)
                {
                    float impact = -Vector3.Dot(Velocity, hit.normal);
                    transform.position = hit.point + hit.normal * 2.4f;
                    Velocity = Vector3.ProjectOnPlane(Velocity, hit.normal);
                    _speed = Velocity.magnitude;
                    _alt = 2.4f;
                    if (impact > 32f && PlayerControlled)
                        Unit.ApplyDamage(impact * 0.85f, null);
                }
            }
            else _alt = transform.position.y;
        }

        void Recharge(float dt)
        {
            Energy = Mathf.Min(EnergyMax, Energy + 7f * dt);
            _plasmaHeat = Mathf.Max(0f, _plasmaHeat - 28f * dt);
            _cannonCd = Mathf.Max(0f, _cannonCd - dt);
            _missileCd = Mathf.Max(0f, _missileCd - dt);
            if (Energy > 20f && Unit.Health < Unit.MaxHealth)
            {
                _repairCd += dt;
                if (_repairCd > 0.4f)
                {
                    Unit.Health = Mathf.Min(Unit.MaxHealth, Unit.Health + 2.5f);
                    Energy -= 1.2f;
                    _repairCd = 0f;
                }
            }
        }

        void UpdateLock(float dt)
        {
            if (Target == null || !Target.Alive)
            {
                LockProgress = 0f;
                Target = null;
                return;
            }
            Vector3 to = Target.transform.position - transform.position;
            float ang = Vector3.Angle(transform.forward, to);
            if (ang < 18f && to.magnitude < 900f)
                LockProgress = Mathf.Min(1f, LockProgress + dt * 0.85f);
            else
                LockProgress = Mathf.Max(0f, LockProgress - dt * 0.5f);
        }

        public void TryFire()
        {
            Vector3 muzzle = GunMuzzle != null ? GunMuzzle.position : transform.position + transform.forward * 4.5f;
            switch (Group)
            {
                case WeaponGroup.Plasma:
                    if (_plasmaHeat > 90f || Energy < 2f) return;
                    _plasmaHeat += 14f;
                    Energy -= 1.6f;
                    Projectile.Spawn(muzzle, transform.forward * 420f + Velocity, 11f, Unit, Palette.Plasma, 0.42f);
                    SoundBank.PlayPlasma(muzzle);
                    break;
                case WeaponGroup.Cannon:
                    if (_cannonCd > 0f || CannonAmmo <= 0) return;
                    _cannonCd = 0.09f;
                    CannonAmmo--;
                    Projectile.Spawn(muzzle + transform.right * Random.Range(-0.15f, 0.15f),
                        (transform.forward * 520f + Velocity), 8f, Unit, Palette.Tracer, 0.22f);
                    SoundBank.PlayCannon(muzzle);
                    break;
                case WeaponGroup.Missiles:
                    if (_missileCd > 0f || Missiles <= 0 || LockProgress < 1f || Target == null) return;
                    _missileCd = 1.1f;
                    Missiles--;
                    Missile.Spawn(muzzle + transform.right * 1.4f, transform.forward, Target, Unit);
                    SoundBank.PlayMissile(muzzle);
                    LockProgress = 0.35f;
                    break;
            }
        }

        public void FireAt(Vector3 dir)
        {
            Vector3 muzzle = transform.position + dir.normalized * 4f;
            Projectile.Spawn(muzzle, dir.normalized * 380f + Velocity, 9f, Unit, Palette.TharneAccent, 0.34f);
        }

        void ApplyCamera()
        {
            if (CockpitCam) CockpitCam.enabled = CameraMode == 0;
            if (ChaseCam) ChaseCam.enabled = CameraMode == 1;
        }
    }
}
