using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatAndMouse
{
    public enum MouseBehavior { Wander, Flee, Hidden }

    /// <summary>
    /// AI-controlled mouse with a small state machine:
    ///   Wander -> meanders around slowly,
    ///   Flee   -> cat is close: zig-zags toward the safest hole (or just away),
    ///   Hidden -> inside a hole for a few seconds, then pops out of a different hole.
    /// After hiding there's a cooldown, which is the cat's window to catch it.
    /// (Inside namespace CatAndMouse, this Mouse class takes priority over
    ///  Microsoft.Xna.Framework.Input.Mouse.)
    /// </summary>
    public class Mouse : Sprite
    {
        const float WanderSpeed     = 90f;
        const float FleeSpeed       = 240f;
        const float PanicRadius     = 230f;  // cat distance that triggers fleeing
        const float Steering        = 5f;    // how fast velocity turns toward the desired direction
        const float WobbleFrequency = 9f;    // zig-zag speed
        const float FleeWobble      = 0.7f;  // zig-zag width while fleeing (radians)
        const float WanderWobble    = 0.25f;
        const float HideDuration    = 2.5f;
        const float HideCooldown    = 5f;
        const float WallMargin      = 60f;

        public Rectangle PlayArea;
        public MouseBehavior Behavior { get; private set; } = MouseBehavior.Wander;
        public bool IsHidden => Behavior == MouseBehavior.Hidden;

        readonly Cat cat;
        readonly List<Hole> holes;
        readonly Random rng = new Random();
        readonly float speedScale, panicScale;

        float wanderAngle;
        float clock;         // running time, drives the wobble and meander
        float hideTimer;
        float hideCooldown;
        Hole currentHole;

        public Mouse(Texture2D texture, Vector2 position, Rectangle playArea, Cat cat, List<Hole> holes,
                     float speedScale = 1f, float panicScale = 1f)
            : base(texture, position)
        {
            PlayArea = playArea;
            this.cat = cat;
            this.holes = holes;
            this.speedScale = speedScale;
            this.panicScale = panicScale;
            RadiusFactor = 0.36f;
            LayerDepth = 0.5f;
            wanderAngle = (float)(rng.NextDouble() * MathHelper.TwoPi);
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            clock += dt;
            hideCooldown -= dt;

            if (Behavior == MouseBehavior.Hidden)
            {
                UpdateHidden(dt);
                return;
            }

            // --- choose state ---
            float distToCat = Vector2.Distance(Position, cat.Position);
            Behavior = distToCat < PanicRadius * panicScale ? MouseBehavior.Flee : MouseBehavior.Wander;

            // --- choose desired direction + speed for this state ---
            Vector2 desiredDir;
            float speed, wobble;
            if (Behavior == MouseBehavior.Flee)
            {
                speed = FleeSpeed * speedScale;
                wobble = FleeWobble;
                Hole target = hideCooldown <= 0f ? PickEscapeHole() : null;
                desiredDir = target != null
                    ? SafeNormalize(target.Position - Position)
                    : SafeNormalize(Position - cat.Position);
            }
            else
            {
                speed = WanderSpeed * speedScale;
                wobble = WanderWobble;
                // Smooth, deterministic meandering (two sine waves) - frame-rate independent
                wanderAngle += (MathF.Sin(clock * 0.9f) + MathF.Sin(clock * 2.3f)) * 0.9f * dt;
                desiredDir = AngleToVector(wanderAngle);
            }

            desiredDir = SafeNormalize(desiredDir + WallAvoidance());

            // Zig-zag: rotate the desired direction back and forth with a sine wave
            desiredDir = Rotate(desiredDir, MathF.Sin(clock * WobbleFrequency) * wobble);

            // Steer: ease current velocity toward desired velocity (time-corrected)
            Vector2 desiredVelocity = desiredDir * speed;
            Velocity = Vector2.Lerp(Velocity, desiredVelocity, 1f - MathF.Exp(-Steering * dt));

            if (Velocity.LengthSquared() > 1f)
                Rotation = MathF.Atan2(Velocity.Y, Velocity.X);

            base.Update(gameTime);
            KeepInside(PlayArea, 0.8f);
            TryEnterHole();
        }

        void UpdateHidden(float dt)
        {
            hideTimer -= dt;
            if (hideTimer > 0f) return;

            // Pop out of a hole that isn't near the cat
            if (currentHole != null) currentHole.Occupied = false;
            Hole exit = PickExitHole();
            Position = exit.Position;
            Visible = true;
            Behavior = MouseBehavior.Wander;
            hideCooldown = HideCooldown;
            Velocity = SafeNormalize(Position - cat.Position) * FleeSpeed * speedScale * 0.5f;
            currentHole = null;
        }

        void TryEnterHole()
        {
            if (Behavior != MouseBehavior.Flee || hideCooldown > 0f) return;

            foreach (Hole h in holes)
            {
                if (Vector2.Distance(Position, h.Position) < h.Radius * 0.6f)
                {
                    Behavior = MouseBehavior.Hidden;
                    Visible = false;
                    Velocity = Vector2.Zero;
                    hideTimer = HideDuration;
                    currentHole = h;
                    h.Occupied = true;
                    return;
                }
            }
        }

        /// <summary>Nearest hole that the cat isn't closer to.</summary>
        Hole PickEscapeHole()
        {
            Hole best = null;
            float bestScore = float.MaxValue;
            foreach (Hole h in holes)
            {
                float mouseToHole = Vector2.Distance(Position, h.Position);
                float catToHole   = Vector2.Distance(cat.Position, h.Position);
                if (catToHole < mouseToHole) continue;           // don't run at the cat
                float score = mouseToHole - 0.5f * catToHole;     // close to me, far from cat
                if (score < bestScore) { bestScore = score; best = h; }
            }
            return best;
        }

        Hole PickExitHole()
        {
            var safe = holes.FindAll(h => h != currentHole &&
                                          Vector2.Distance(h.Position, cat.Position) > 200f);
            if (safe.Count > 0) return safe[rng.Next(safe.Count)];

            // Otherwise the hole farthest from the cat
            Hole far = holes[0];
            foreach (Hole h in holes)
                if (Vector2.Distance(h.Position, cat.Position) > Vector2.Distance(far.Position, cat.Position))
                    far = h;
            return far;
        }

        Vector2 WallAvoidance()
        {
            Vector2 push = Vector2.Zero;
            if (Position.X < PlayArea.Left + WallMargin)   push.X += 1f;
            if (Position.X > PlayArea.Right - WallMargin)  push.X -= 1f;
            if (Position.Y < PlayArea.Top + WallMargin)    push.Y += 1f;
            if (Position.Y > PlayArea.Bottom - WallMargin) push.Y -= 1f;
            return push * 1.5f;
        }
    }
}
