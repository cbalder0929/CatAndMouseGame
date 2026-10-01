using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatAndMouse
{
    /// <summary>A mouse hole. Doesn't move, but pulses when the mouse is inside.</summary>
    public class Hole : Sprite
    {
        public bool Occupied;
        float clock;

        public Hole(Texture2D texture, Vector2 position) : base(texture, position)
        {
            LayerDepth = 0.9f;   // drawn behind the cat and mouse
            RadiusFactor = 0.5f;
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            clock += dt;
            // Pulse is driven by accumulated time, so it's the same speed at any FPS
            Scale = Occupied ? 1f + 0.06f * MathF.Sin(clock * 10f) : 1f;
        }
    }
}
