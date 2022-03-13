using Microsoft.Xna.Framework;

namespace CS5410.Objects
{
    public class AnimatedSprite
    {
        private readonly Vector2 m_size;
        protected Vector2 m_position;
        protected float m_rotation = 0;

        public AnimatedSprite(Vector2 size, Vector2 position)
        {
            m_size = size;
            m_position = position;
        }

        public Vector2 Size
        {
            get { return m_size; }
        }

        public Vector2 Position
        {
            get { return m_position; }
        }

        public float Rotation
        {
            get { return m_rotation; }
            set { m_rotation = value; }
        }
    }
}