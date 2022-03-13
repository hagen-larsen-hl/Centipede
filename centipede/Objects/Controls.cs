using Microsoft.Xna.Framework.Input;

namespace CS5410.Objects
{
    public class Controls
    {
        public Controls() { }

        public Controls(Keys up, Keys down, Keys left, Keys right, Keys fire)
        {
            Up = up;
            Down = down; 
            Left = left;
            Right = right;
            Fire = fire;
        }
        
        public Keys Up { get; set; }
        public Keys Down { get; set; }
        public Keys Right { get; set; }
        public Keys Left { get; set; }
        public Keys Fire { get; set; }
    }
}