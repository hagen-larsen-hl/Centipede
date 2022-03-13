using System;
using System.Net.Mime;
using CS5410.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CS5410.Renderers
{
    public class PlayerRenderer
    {
        private SpriteBatch m_spriteBatch;

        public PlayerRenderer(SpriteBatch spriteBatch)
        {
            m_spriteBatch = spriteBatch;
        }
        
        public void loadContent(ContentManager contentManager)
        {
            
        }
        
        public void Render(Player player, Texture2D spriteSheet)
        {
            m_spriteBatch.Draw(
                spriteSheet,
                player.Boundary,
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