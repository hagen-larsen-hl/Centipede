using System;
using System.Net.Mime;
using Microsoft.Xna.Framework;

namespace CS5410.Objects
{
    public class CentipedeSegment
    {
        public enum DirectionEnum 
        {
            right,
            down,
            left,
        }
        public CentipedeSegment(Centipede head, Centipede tail, Vector2 center, Vector2 size, int[] spriteTime, double toDescend)
        {
            Head = head;
            Tail = tail;
            Size = size;
            Center = center;
            Speed = 0.25f;
            SpriteTime = spriteTime;
            State = 0;
            ToDescend = toDescend;
            Rotation = (float) Math.PI;
            Direction = DirectionEnum.down;
            Boundary = new Rectangle(
                (int) (Center.X - Size.X / 2),
                (int) (Center.Y - Size.Y / 2),
                (int) Size.X,
                (int) Size.Y
            );
        }
        
        public Centipede Head { get; set; }
        public Centipede Tail { get; set; }

        public Vector2 Center { get; set; }

        public Vector2 Size { get; set; }
        public Rectangle Boundary { get; set; }
        public float Rotation { get; set; }
        public float Speed { get; }
        public  DirectionEnum Direction { get; set; }
        public TimeSpan AnimationTime { get; set; }
        public int[] SpriteTime { get; set; }
        public int State { get; set; }
        
        public double ToDescend { get; set; }
        
        public DirectionEnum LastDirection { get; set; }
        
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