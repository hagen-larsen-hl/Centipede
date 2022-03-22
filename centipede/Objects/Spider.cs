using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace CS5410.Objects
{
    public class Spider
    {
        public Spider(Vector2 center, Vector2 size, int[] spriteTime, bool down, bool right)
        {
            Center = center;
            Size = size;
            Speed = 0.25f;
            Down = down;
            Right = right;
            SpriteTime = spriteTime;
            State = 0;
            Boundary = new Rectangle(
                (int) (Center.X - Size.X / 2),
                (int) (Center.Y - Size.Y / 2),
                (int) Size.X,
                (int) Size.Y
            );
        }
        public Vector2 Center { get; set; }
        
        public Vector2 Size { get; set; }
        
        public Rectangle Boundary { get; set; }
        
        public float Speed { get; }
        
        public TimeSpan AnimationTime { get; set; }
        
        public int[] SpriteTime { get; set; }
        
        public int State { get; set; }
        
        public bool Down { get; set; }
        
        public bool Right { get; set; }
        
        public void setPosition(Vector2 vector)
        {
            Center = vector;
            Boundary = new Rectangle(
                (int) (Center.X - Size.X / 2),
                (int) (Center.Y - Size.Y / 2),
                (int) Size.X,
                (int) Size.Y
            );
        }
    }
}