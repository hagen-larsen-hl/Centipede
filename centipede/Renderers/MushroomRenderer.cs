using System;
using System.Net.Mime;
using CS5410.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CS5410.Renderers
{
    public class MushroomRenderer
    {
        private SpriteBatch m_spriteBatch;

        public MushroomRenderer(SpriteBatch spriteBatch)
        {
            m_spriteBatch = spriteBatch;
        }
        
        public void loadContent(ContentManager contentManager)
        {
            
        }
        
        public void Render(Mushroom mushroom, Texture2D spriteSheet, int spriteCount)
        {
            int subwidth = (spriteSheet.Width / spriteCount);
            m_spriteBatch.Draw(
                spriteSheet,
                mushroom.getBoundary(),
                new Rectangle(
                    (mushroom.state * subwidth) + mushroom.state, 
                    0, 
                    subwidth, 
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