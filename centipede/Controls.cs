using System;
using Microsoft.Xna.Framework.Input;

namespace CS5410
{
    /// <summary>
    /// This class demonstrates how to create an object that can be serialized
    /// under the XNA framework.
    /// </summary>
    //[Serializable]
    public class Controls
    {
        public Controls() {}
        /// <summary>
        /// Overloaded constructor used to create an object for long term storage
        /// </summary>
        public Controls(Keys up, Keys down, Keys right, Keys left, Keys fire)
        {
            this.up = up;
            this.down = down;
            this.right = right;
            this.left = left;
            this.fire = fire;
        }

        public Keys up { get; set; }
        public Keys down { get; set; }
        public Keys right { get; set; }
        public Keys left { get; set; }
        public Keys fire { get; set; }
    }
}