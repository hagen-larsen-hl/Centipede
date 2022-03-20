using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace CS5410.Objects
{
    public class Mushroom
    {
        public int state = 0;
        public bool isPoisoned = false;
        private Rectangle boundary;

        public Mushroom(Vector2 size, Vector2 center)
        {
            Size = size;
            Center = center;
            boundary = new Rectangle(
                (int) (center.X - size.X / 2),
                (int) (center.Y - size.Y / 2),
                (int) Size.X,
                (int) Size.Y
            );
        }
        
        public Vector2 Center { get; set; }
        public Vector2 Size { get; set; }

        public Rectangle getBoundary()
        {
            return boundary;
        }
    }
}
