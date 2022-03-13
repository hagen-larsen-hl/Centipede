using System.ComponentModel.Design;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace CS5410.Objects
{
    public class Bullet
    {
        public Bullet(Vector2 position, Vector2 size)
        {
            Position = position;
            Size = size;
            Speed = 1.0f;
            Boundary = new Rectangle(
                (int) Position.X,
                (int) Position.Y,
                (int) Size.X,
                (int) Size.Y
            );
        }
        public Vector2 Position { get; set; }
        
        public Vector2 Size { get; set; }
        
        public Rectangle Boundary { get; set; }
        
        public float Speed { get; }
        
        public void setPosition(Vector2 vector)
        {
            Position = vector;
            Boundary = new Rectangle(
                (int) Position.X,
                (int) Position.Y,
                (int) Size.X,
                (int) Size.Y
            );
        }
        
    }
}