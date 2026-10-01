using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatAndMouse
{
    /// <summary>
    /// Builds simple textures in code so the project runs without any art assets.
    /// To use real art instead, add PNGs through the MGCB editor and swap these
    /// calls for Content.Load&lt;Texture2D&gt;("cat") etc. in Game1.LoadContent.
    /// Art should face RIGHT, since Rotation 0 = +X.
    /// </summary>
    public static class TextureFactory
    {
        public static Texture2D Circle(GraphicsDevice gd, int diameter, Color color) =>
            Build(gd, diameter, new[] { (new Vector2(0.5f, 0.5f), 0.5f, color) });

        /// <summary>Round body with ears, eyes and a nose, facing right.</summary>
        public static Texture2D Creature(GraphicsDevice gd, int size, Color body, Color ears, Color eyes)
        {
            return Build(gd, size, new[]
            {
                (new Vector2(0.62f, 0.22f), 0.14f, ears),
                (new Vector2(0.62f, 0.78f), 0.14f, ears),
                (new Vector2(0.50f, 0.50f), 0.36f, body),
                (new Vector2(0.66f, 0.38f), 0.05f, eyes),
                (new Vector2(0.66f, 0.62f), 0.05f, eyes),
                (new Vector2(0.86f, 0.50f), 0.05f, Color.HotPink),
            });
        }

        /// <summary>Paints circles (normalized 0..1 coords) in order; later ones draw on top.</summary>
        static Texture2D Build(GraphicsDevice gd, int size, (Vector2 center, float radius, Color color)[] circles)
        {
            var tex = new Texture2D(gd, size, size);
            var data = new Color[size * size];   // defaults to transparent

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                foreach (var c in circles)
                    if (Vector2.DistanceSquared(p, c.center) <= c.radius * c.radius)
                        data[y * size + x] = c.color;
            }

            tex.SetData(data);
            return tex;
        }
    }
}
