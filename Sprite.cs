using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatAndMouse
{
    /// <summary>
    /// Base class for anything that is drawn and moves.
    /// Holds the shared state (texture, position, velocity, rotation...) and
    /// does time-corrected movement so every child is frame-rate independent.
    /// </summary>
    public class Sprite
    {
        public Texture2D Texture { get; protected set; }
        public Vector2 Position;
        public Vector2 Velocity;           // pixels per SECOND, not per frame
        public float Rotation;             // radians, 0 = facing right (+X)
        public float Scale = 1f;
        public Color Tint = Color.White;
        public bool Visible = true;
        public float LayerDepth = 0.5f;    // 0 = front, 1 = back (SpriteSortMode.BackToFront)
        public float RadiusFactor = 0.5f;  // fraction of texture width used for collision

        public Vector2 Origin => new Vector2(Texture.Width / 2f, Texture.Height / 2f);
        public float Radius => Texture.Width * Scale * RadiusFactor;

        public Sprite(Texture2D texture, Vector2 position)
        {
            Texture = texture;
            Position = position;
        }

        public virtual void Update(GameTime gameTime)
        {
            // Time correction: multiply by seconds since last frame.
            // 60 FPS -> dt ~0.0167, 30 FPS -> dt ~0.033, same distance per second.
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position += Velocity * dt;
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible) return;
            spriteBatch.Draw(Texture, Position, null, Tint, Rotation, Origin, Scale,
                             SpriteEffects.None, LayerDepth);
        }

        /// <summary>Circle-vs-circle collision.</summary>
        public bool CollidesWith(Sprite other)
        {
            float r = Radius + other.Radius;
            return Vector2.DistanceSquared(Position, other.Position) < r * r;
        }

        /// <summary>Clamp inside an area and bounce off the edges.</summary>
        public void KeepInside(Rectangle area, float bounce = 0.5f)
        {
            float r = Radius;
            if (Position.X < area.Left + r)   { Position.X = area.Left + r;   Velocity.X =  MathF.Abs(Velocity.X) * bounce; }
            if (Position.X > area.Right - r)  { Position.X = area.Right - r;  Velocity.X = -MathF.Abs(Velocity.X) * bounce; }
            if (Position.Y < area.Top + r)    { Position.Y = area.Top + r;    Velocity.Y =  MathF.Abs(Velocity.Y) * bounce; }
            if (Position.Y > area.Bottom - r) { Position.Y = area.Bottom - r; Velocity.Y = -MathF.Abs(Velocity.Y) * bounce; }
        }

        // ---- math helpers shared by children ----
        protected static Vector2 AngleToVector(float radians) =>
            new Vector2(MathF.Cos(radians), MathF.Sin(radians));

        protected static Vector2 Rotate(Vector2 v, float radians)
        {
            float c = MathF.Cos(radians), s = MathF.Sin(radians);
            return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
        }

        protected static Vector2 SafeNormalize(Vector2 v) =>
            v.LengthSquared() < 0.0001f ? Vector2.UnitX : Vector2.Normalize(v);
    }
}
