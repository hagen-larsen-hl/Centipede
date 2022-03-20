using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CS5410.Objects
{
    public class Player
    {
        public int lives = 3;

        public Player(Vector2 vector, Vector2 size, int lives, int score)
        {
            this.lives = lives;
            Origin = vector;
            Center = vector;
            Size = size;
            Speed = 0.6f;
            Score = score;
            Boundary = new Rectangle(
                (int) (Center.X - Size.X / 2),
                (int) (Center.Y - Size.Y / 2),
                (int) Size.X,
                (int) Size.Y
            );
        }

        public bool isAlive()
        {
            return lives > 0;
        }

        public Vector2 Center { get; set; }

        public Vector2 Origin { get; }

        public Vector2 Size { get; }

        public float Speed { get; }
        
        public int FireDelay { get; set; }
        
        public int Score { get; set; }

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

        public Rectangle Boundary { get; set; }
    }
}