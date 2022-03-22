using CS5410.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CS5410.Renderers
{
    public class SpiderRenderer
    {
        private SpriteBatch m_spriteBatch;

        public SpiderRenderer(SpriteBatch spriteBatch)
        {
            m_spriteBatch = spriteBatch;
        }
        
        public void loadContent(ContentManager contentManager)
        {
            
        }
        
        public void Render(Spider spider, Texture2D spriteSheet, int spriteCount)
        {
            m_spriteBatch.Draw(
                spriteSheet,
                spider.Boundary,
                new Rectangle(
                    (spriteSheet.Width / spriteCount) * spider.State,
                    0,
                    spriteSheet.Width / spriteCount,
                    spriteSheet.Height
                ),
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