using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CS5410.Objects
{
    public class Mushroom : AnimatedSprite
    {
        public int state = 0;
        public bool isPoisoned = false;
        private Rectangle boundary;

        public Mushroom(Vector2 size, Vector2 position) : base(size, position)
        {
            boundary = new Rectangle(
                (int) Position.X,
                (int) Position.Y,
                (int) Size.X,
                (int) Size.Y
            );
        }

        public Rectangle getBoundary()
        {
            return boundary;
        }
    }
}
