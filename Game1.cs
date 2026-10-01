using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CatAndMouse
{
    public class Game1 : Game
    {
        enum GameState { Menu, Playing, CatWon, MouseWon }
        enum Difficulty { Easy, Medium, Hard }

        const float RoundDuration = 45f;   // mouse wins if the cat hasn't caught it by then

        readonly GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;
        Texture2D catTexture, mouseTexture, holeTexture, pixel;

        Cat cat;
        readonly List<Mouse> mice = new List<Mouse>();
        Difficulty difficulty = Difficulty.Easy;
        readonly Rectangle[] menuButtons = new Rectangle[3];
        Rectangle menuButton;
        readonly List<Hole> holes = new List<Hole>();
        readonly List<Sprite> sprites = new List<Sprite>();  // updated/drawn polymorphically

        GameState state;
        float roundTime;
        int catScore;
        float smoothedFps = 60f;
        KeyboardState prevKeys;
        MouseState prevMouseState;
        Rectangle playArea;
        Rectangle resetButton;

        float TimeLeft => Math.Max(0f, RoundDuration - roundTime);

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 640;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Variable time step + no vsync = frame rate runs as fast as it can.
            // Press F in-game to lock to 30 FPS; movement speed should look identical.
            IsFixedTimeStep = false;
            graphics.SynchronizeWithVerticalRetrace = false;
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("DefaultFont");
            playArea = GraphicsDevice.Viewport.Bounds;

            catTexture   = TextureFactory.Creature(GraphicsDevice, 64, new Color(235, 140, 50), new Color(190, 95, 30), Color.Black);
            mouseTexture = TextureFactory.Creature(GraphicsDevice, 36, new Color(150, 150, 160), Color.Pink, Color.Black);
            holeTexture  = TextureFactory.Circle(GraphicsDevice, 48, new Color(45, 28, 18));

            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            const int buttonWidth = 200, buttonHeight = 56;
            resetButton = new Rectangle((playArea.Width - buttonWidth) / 2,
                                         (int)(playArea.Height * 0.6f), buttonWidth, buttonHeight);

            for (int i = 0; i < menuButtons.Length; i++)
                menuButtons[i] = new Rectangle((playArea.Width - 240) / 2, (int)(playArea.Height * 0.38f) + i * 76, 240, 56);
            menuButton = new Rectangle(resetButton.X, resetButton.Bottom + 16, buttonWidth, buttonHeight);

            ResetGame();
            state = GameState.Menu;
        }

        void ResetGame()
        {
            holes.Clear();
            sprites.Clear();

            float w = playArea.Width, h = playArea.Height;
            Vector2[] holeSpots =
            {
                new Vector2(w * 0.08f, h * 0.12f), new Vector2(w * 0.92f, h * 0.12f),
                new Vector2(w * 0.08f, h * 0.88f), new Vector2(w * 0.92f, h * 0.88f),
                new Vector2(w * 0.50f, h * 0.50f),
            };
            foreach (Vector2 p in holeSpots) holes.Add(new Hole(holeTexture, p));

            cat   = new Cat(catTexture, new Vector2(w * 0.25f, h * 0.5f), playArea);

            // Easy: 1 normal mouse. Medium: 1 mouse, 25% faster and warier. Hard: 4 mice, 25% faster.
            float speed = difficulty == Difficulty.Easy ? 1f : 1.25f;
            float panic = difficulty == Difficulty.Medium ? 1.25f : 1f;
            int count = difficulty == Difficulty.Hard ? 4 : 1;
            mice.Clear();
            for (int i = 0; i < count; i++)
            {
                float y = count == 1 ? 0.5f : 0.2f + 0.6f * i / (count - 1);
                mice.Add(new Mouse(mouseTexture, new Vector2(w * 0.75f, h * y), playArea, cat, holes, speed, panic));
            }

            sprites.AddRange(holes);
            sprites.AddRange(mice);
            sprites.Add(cat);

            state = GameState.Playing;
            roundTime = 0f;
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keys = Keyboard.GetState();
            MouseState mouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (keys.IsKeyDown(Keys.Escape)) Exit();

            // F: toggle a locked 30 FPS to demonstrate frame-rate independence
            if (Pressed(keys, Keys.F))
            {
                IsFixedTimeStep = !IsFixedTimeStep;
                TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 30.0);
            }

            bool clicked = mouseState.LeftButton == ButtonState.Pressed &&
                           prevMouseState.LeftButton == ButtonState.Released;

            if (state == GameState.Menu)
            {
                int pick = -1;
                for (int i = 0; i < menuButtons.Length; i++)
                    if (clicked && menuButtons[i].Contains(mouseState.Position)) pick = i;
                if (Pressed(keys, Keys.D1)) pick = 0;
                if (Pressed(keys, Keys.D2)) pick = 1;
                if (Pressed(keys, Keys.D3)) pick = 2;

                if (pick >= 0)
                {
                    difficulty = (Difficulty)pick;
                    ResetGame();
                }
            }
            else if (state == GameState.Playing)
            {
                roundTime += dt;
                foreach (Sprite s in sprites) s.Update(gameTime);

                for (int i = mice.Count - 1; i >= 0; i--)
                {
                    if (!mice[i].IsHidden && cat.CollidesWith(mice[i]))
                    {
                        sprites.Remove(mice[i]);
                        mice.RemoveAt(i);
                        catScore++;
                    }
                }

                if (mice.Count == 0)
                {
                    state = GameState.CatWon;
                }
                else if (TimeLeft <= 0f)
                {
                    state = GameState.MouseWon;
                }
            }
            else
            {
                bool clickedReset = clicked && resetButton.Contains(mouseState.Position);

                if ((clicked && menuButton.Contains(mouseState.Position)) || Pressed(keys, Keys.M))
                    state = GameState.Menu;
                else if (clickedReset || Pressed(keys, Keys.Enter))
                    ResetGame();
            }

            if (dt > 0f) smoothedFps = MathHelper.Lerp(smoothedFps, 1f / dt, 0.05f);

            Window.Title = "Cat & Mouse";

            prevKeys = keys;
            prevMouseState = mouseState;
            base.Update(gameTime);
        }

        bool Pressed(KeyboardState keys, Keys key) => keys.IsKeyDown(key) && prevKeys.IsKeyUp(key);

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(214, 190, 150));   // wooden floor

            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
            foreach (Sprite s in sprites) s.Draw(spriteBatch);
            spriteBatch.End();

            spriteBatch.Begin();

            if (state == GameState.Menu)
            {
                DrawTextCentered("CAT & MOUSE - Select Difficulty", playArea.Height * 0.22f, Color.Black);
                string[] labels = { "1  EASY", "2  MEDIUM", "3  HARD" };
                for (int i = 0; i < menuButtons.Length; i++)
                {
                    spriteBatch.Draw(pixel, menuButtons[i], Color.SaddleBrown);
                    DrawTextCenteredIn(labels[i], menuButtons[i], Color.White);
                }
                spriteBatch.End();
                base.Draw(gameTime);
                return;
            }

            spriteBatch.DrawString(font, $"Time: {TimeLeft:0}", new Vector2(16, 12), Color.Black);
            DrawTextRightAligned($"Cat Score: {catScore}", 16, Color.Black);
            spriteBatch.DrawString(font,
                $"Pounce: {(cat.PounceReady ? "READY" : "...")}   {smoothedFps:0} FPS " +
                $"({(IsFixedTimeStep ? "locked 30" : "uncapped")}, F to toggle)",
                new Vector2(16, playArea.Height - 32), Color.Black * 0.7f);

            if (state != GameState.Playing)
            {
                Color overlay = state == GameState.CatWon ? Color.Gold : Color.SteelBlue;
                spriteBatch.Draw(pixel, playArea, null, overlay * 0.3f, 0f, Vector2.Zero, SpriteEffects.None, 0f);

                string message = state == GameState.CatWon
                    ? "CAT WINS! All mice caught."
                    : "MOUSE WINS! Time ran out.";
                DrawTextCentered(message, playArea.Height * 0.30f, Color.Black);

                spriteBatch.Draw(pixel, resetButton, Color.SaddleBrown);
                DrawTextCenteredIn("RESET", resetButton, Color.White);
                spriteBatch.Draw(pixel, menuButton, Color.SaddleBrown);
                DrawTextCenteredIn("MENU", menuButton, Color.White);
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }

        void DrawTextRightAligned(string text, float y, Color color)
        {
            float width = font.MeasureString(text).X;
            spriteBatch.DrawString(font, text, new Vector2(playArea.Width - width - 16, y), color);
        }

        void DrawTextCentered(string text, float y, Color color)
        {
            Vector2 size = font.MeasureString(text);
            spriteBatch.DrawString(font, text, new Vector2((playArea.Width - size.X) / 2f, y), color);
        }

        void DrawTextCenteredIn(string text, Rectangle rect, Color color)
        {
            Vector2 size = font.MeasureString(text);
            Vector2 pos = new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f);
            spriteBatch.DrawString(font, text, pos, color);
        }
    }
}
