using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CatAndMouse
{
    /// <summary>
    /// Player-controlled cat. Movement is "steer + thrust + drift + pounce":
    ///   Left/Right (A/D) rotate with angular momentum,
    ///   Up (W) accelerates in the facing direction, Down (S) brakes/reverses,
    ///   sideways motion bleeds off slower than a car with grip, so the cat drifts on turns,
    ///   Space = pounce (burst of speed with a cooldown).
    /// </summary>
    public class Cat : Sprite
    {
        // All tuning values are per-second, so they behave the same at any frame rate.
        const float TurnSpeed      = 4.0f;  // max angular speed, radians/sec
        const float TurnResponse   = 10f;   // how quickly turning ramps up/down
        const float Thrust         = 650f;  // forward acceleration, px/sec^2
        const float ReverseThrust  = 300f;
        const float MaxSpeed       = 300f;  // px/sec (outside of a pounce)
        const float ForwardDrag    = 1.5f;  // decay rate of forward speed
        const float SideGrip       = 6f;    // decay rate of sideways speed (higher = less drift)
        const float PounceSpeed    = 700f;
        const float PounceDuration = 0.25f;
        const float PounceCooldown = 1.2f;

        public Rectangle PlayArea;

        float turnRate;       // current angular velocity (smoothed)
        float pounceTimer;    // > 0 while pouncing
        float cooldownTimer;  // > 0 while pounce is recharging
        KeyboardState prevKeys;

        public bool IsPouncing  => pounceTimer > 0f;
        public bool PounceReady => cooldownTimer <= 0f;

        public Cat(Texture2D texture, Vector2 position, Rectangle playArea)
            : base(texture, position)
        {
            PlayArea = playArea;
            RadiusFactor = 0.36f;
            LayerDepth = 0.3f;
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState keys = Keyboard.GetState();

            // 1. Steering with angular momentum (eases into and out of turns)
            float turnInput = 0f;
            if (keys.IsKeyDown(Keys.Left)  || keys.IsKeyDown(Keys.A)) turnInput -= 1f;
            if (keys.IsKeyDown(Keys.Right) || keys.IsKeyDown(Keys.D)) turnInput += 1f;

            // Frame-rate independent smoothing: 1 - e^(-k*dt) instead of a fixed lerp amount
            turnRate = MathHelper.Lerp(turnRate, turnInput * TurnSpeed, 1f - MathF.Exp(-TurnResponse * dt));
            Rotation += turnRate * dt;

            Vector2 forward = AngleToVector(Rotation);
            Vector2 right   = new Vector2(-forward.Y, forward.X);

            // 2. Thrust (acceleration changes velocity, velocity changes position)
            if (keys.IsKeyDown(Keys.Up)   || keys.IsKeyDown(Keys.W)) Velocity += forward * Thrust * dt;
            if (keys.IsKeyDown(Keys.Down) || keys.IsKeyDown(Keys.S)) Velocity -= forward * ReverseThrust * dt;

            // 3. Pounce (edge-triggered: only on the frame Space goes down)
            pounceTimer   -= dt;
            cooldownTimer -= dt;
            bool spacePressed = keys.IsKeyDown(Keys.Space) && prevKeys.IsKeyUp(Keys.Space);
            if (spacePressed && PounceReady)
            {
                Velocity = forward * PounceSpeed;
                pounceTimer = PounceDuration;
                cooldownTimer = PounceCooldown;
            }

            // 4. Drift: split velocity into forward and sideways parts and decay them
            //    at different rates. e^(-k*dt) is exact exponential decay, so it
            //    gives the same result whether you run at 30 or 300 FPS.
            if (!IsPouncing)
            {
                float fwdSpeed  = Vector2.Dot(Velocity, forward) * MathF.Exp(-ForwardDrag * dt);
                float sideSpeed = Vector2.Dot(Velocity, right)   * MathF.Exp(-SideGrip * dt);
                Velocity = forward * fwdSpeed + right * sideSpeed;

                // 5. Soft speed cap so the end of a pounce eases down instead of snapping
                float speed = Velocity.Length();
                if (speed > MaxSpeed)
                {
                    float eased = MathHelper.Lerp(speed, MaxSpeed, 1f - MathF.Exp(-8f * dt));
                    Velocity *= eased / speed;
                }
            }

            base.Update(gameTime);          // Position += Velocity * dt
            KeepInside(PlayArea, 0.4f);

            // Visual feedback for pounce state
            Tint = IsPouncing ? Color.OrangeRed : (PounceReady ? Color.White : Color.LightGray);

            prevKeys = keys;
        }
    }
}
