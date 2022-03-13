using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace CS5410.Objects
{
    public class Flea
    {
        public Flea(Vector2 position, Vector2 size, int[] spriteTime)
        {
            Position = position;
            Size = size;
            Speed = 0.5f;
            Mushrooms = new List<Mushroom>();
            SpriteTime = spriteTime;
            State = 0;
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

        public List<Mushroom> Mushrooms { get; set; }
        
        public TimeSpan AnimationTime { get; set; }
        
        public int[] SpriteTime { get; set; }
        
        public int State { get; set; }
        
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