using CS5410.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CS5410.Renderers
{
    public class BulletRenderer
    {
        private SpriteBatch m_spriteBatch;

        public BulletRenderer(SpriteBatch spriteBatch)
        {
            m_spriteBatch = spriteBatch;
        }
        
        public void loadContent(ContentManager contentManager)
        {
            
        }
        
        public void Render(Bullet bullet, Texture2D spriteSheet)
        {
            m_spriteBatch.Draw(
                spriteSheet,
                bullet.Boundary,
                null,
                Color.White,
                0,
                new Vector2(
                    0,
                    0
                ),
                SpriteEffects.None,
                0);
        }
    }
}