using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CS5410
{
    public class CreditsView : GameStateView
    {
        private SpriteFont m_font;
        private const string MESSAGE = "*I* wrote this amazing game!";
        
        public override void initialize(GraphicsDevice graphicsDevice, GraphicsDeviceManager graphics)
        {
            m_graphics = graphics;
            m_spriteBatch = new SpriteBatch(graphicsDevice);
        }

        public override void loadContent(ContentManager contentManager)
        {
            m_font = contentManager.Load<SpriteFont>("Fonts/menu");
        }

        public override GameStateEnum processInput(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                return GameStateEnum.MainMenu;
            }

            return GameStateEnum.About;
        }

        public override void render(GameTime gameTime)
        {
            m_spriteBatch.Begin();
            
            float bottom = (float) (m_graphics.PreferredBackBufferHeight * 0.1);
            bottom = drawMenuItem(m_font, "CREDITS", bottom, Color.White);
            bottom = drawMenuItem(m_font,"GAMEPLAY", bottom, Color.Blue);
            bottom = drawMenuItem(m_font, "Hagen Larsen", bottom, Color.SkyBlue);
            bottom = drawMenuItem(m_font, "IMAGES", bottom, Color.Blue);
            bottom = drawMenuItem(m_font, "Hagen Larsen", bottom, Color.SkyBlue);
            bottom = drawMenuItem(m_font, "Spriter's Resource", bottom, Color.SkyBlue);
            bottom = drawMenuItem(m_font, "SOUND", bottom, Color.Blue);
            bottom = drawMenuItem(m_font, "freesound.org", bottom, Color.SkyBlue);
            bottom = drawMenuItem(m_font, "nebulasnails", bottom, Color.SkyBlue);
            bottom = drawMenuItem(m_font, "peepholecircus", bottom, Color.SkyBlue);
            bottom = drawMenuItem(m_font, "psychentist", bottom, Color.SkyBlue);
            
            m_spriteBatch.End();
        }
        
        private float drawMenuItem(SpriteFont font, string text, float y, Color color)
        {
            Vector2 stringSize = font.MeasureString(text);
            m_spriteBatch.DrawString(
                font,
                text,
                new Vector2(m_graphics.PreferredBackBufferWidth / 2 - stringSize.X / 2, y),
                color);

            return y + stringSize.Y;
        }

        public override void update(GameTime gameTime)
        {
        }
    }
}
