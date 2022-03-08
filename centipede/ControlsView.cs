using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.ConstrainedExecution;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CS5410
{
    public class ControlsView : GameStateView
    {
        private SpriteFont m_font;
        private const string MESSAGE = "This is how to play the game";
        private bool saving = false;
        private bool loading = false;
        private bool m_waitForKeyRelease = true;
        private bool awaitKey = false;
        private Controls controlsState = new Controls();

        private const string SAVE_MESSAGE = "F1 - Save Something";
        private const string LOAD_MESSAGE = "F2 - Load Something";
        
        private enum Selection
        {
            Up,
            Down,
            Right,
            Left,
            Fire
        }

        private Selection m_currentSelection = Selection.Up;
        
        public override void initialize(GraphicsDevice graphicsDevice, GraphicsDeviceManager graphics)
        {
            m_graphics = graphics;
            m_spriteBatch = new SpriteBatch(graphicsDevice);
            loadControls();
        }

        public override void loadContent(ContentManager contentManager)
        {
            m_font = contentManager.Load<SpriteFont>("Fonts/menu");
        }
        
        /// <summary>
        /// Demonstrates how serialize an object to storage
        /// </summary>
        private void saveControls()
        {
            lock (this)
            {
                if (!saving)
                {
                    saving = true;
                    finalizeSaveAsync(controlsState);
                }
            }
        }
        
        private async void finalizeSaveAsync(Controls state)
        {
            await Task.Run(() =>
            {
                using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    try
                    {
                        using (IsolatedStorageFileStream fs = storage.OpenFile("controls.xml", FileMode.OpenOrCreate))
                        {
                            if (fs != null)
                            {
                                Console.WriteLine("Saving: " + controlsState.up);
                                XmlSerializer mySerializer = new XmlSerializer(typeof(Controls));
                                mySerializer.Serialize(fs, state);
                            }
                        }
                    }
                    catch (IsolatedStorageException)
                    {
                        // Ideally show something to the user, but this is demo code :)
                    }
                }

                this.saving = false;
            });
        }
        
        /// <summary>
        /// Demonstrates how to deserialize an object from storage device
        /// </summary>
        private void loadControls()
        {
            lock (this)
            {
                if (!this.loading)
                {
                    this.loading = true;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    finalizeLoadAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
            }
        }

        private async Task finalizeLoadAsync()
        {
            await Task.Run(() =>
            {
                using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    try
                    {
                        if (storage.FileExists("controls.xml"))
                        {
                            using (IsolatedStorageFileStream fs = storage.OpenFile("controls.xml", FileMode.Open))
                            {
                                if (fs != null)
                                {
                                    Console.Write("Loading: ");
                                    XmlSerializer mySerializer = new XmlSerializer(typeof(Controls));
                                    controlsState = (Controls)mySerializer.Deserialize(fs);
                                    Console.WriteLine("Loaded: " + controlsState.up);
                                }
                            }
                        }
                        else
                        {
                            controlsState = new Controls(Keys.Up, Keys.Down, Keys.Right, Keys.Left, Keys.Space);
                        }
                    }
                    catch (IsolatedStorageException)
                    {
                        // Ideally show something to the user, but this is demo code :)
                    }
                }

                this.loading = false;
            });
        }

        public override GameStateEnum processInput(GameTime gameTime)
        {
            // This is the technique I'm using to ensure one keypress makes one menu navigation move
            if (!m_waitForKeyRelease)
            {
                // If we are awaiting a key press and one exists, assign to current selection
                if (awaitKey && Keyboard.GetState().GetPressedKeyCount() > 0)
                {
                    // set to m_currentSelection
                    Keys key = Keyboard.GetState().GetPressedKeys()[0];
                    
                    if (m_currentSelection == Selection.Up) {  controlsState.up = key; Console.WriteLine("Up: " + controlsState.up);}
                    if (m_currentSelection == Selection.Down) {  controlsState.down = key;}
                    if (m_currentSelection == Selection.Right) {  controlsState.right = key;}
                    if (m_currentSelection == Selection.Left) {  controlsState.left = key;}
                    if (m_currentSelection == Selection.Fire) {  controlsState.fire = key;}

                    awaitKey = false;
                    saveControls();
                    loadControls();
                    return GameStateEnum.Help;
                }
                // Arrow keys to navigate the menu
                if (Keyboard.GetState().IsKeyDown(Keys.Down))
                {
                    if (m_currentSelection != Selection.Fire)
                    {
                        m_currentSelection += 1;
                    }
                    m_waitForKeyRelease = true;
                }

                if (Keyboard.GetState().IsKeyDown(Keys.Up))
                {
                    if (m_currentSelection != Selection.Up)
                    {
                        m_currentSelection -= 1;
                    }

                    m_waitForKeyRelease = true;
                }

                if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                {
                    awaitKey = true;
                    m_waitForKeyRelease = true;
                    return GameStateEnum.Help;
                }
            }
            else if (Keyboard.GetState().GetPressedKeyCount() == 0)
            {
                m_waitForKeyRelease = false;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                m_waitForKeyRelease = true;
                return GameStateEnum.MainMenu;
            }

            return GameStateEnum.Help;
        }

        public override void render(GameTime gameTime)
        {
            m_spriteBatch.Begin();

            if (awaitKey)
            {
                m_spriteBatch.DrawString(m_font, "SET NEW CONTROL",
                    new Vector2(m_graphics.PreferredBackBufferWidth / 2 - m_font.MeasureString("SET NEW CONTROL").X / 2, m_graphics.PreferredBackBufferHeight / 2 - m_font.MeasureString("SET NEW CONTROL").Y / 2), Color.White);
            }
            else
            {
                Vector2 stringSize = m_font.MeasureString(MESSAGE);
                m_spriteBatch.DrawString(m_font, "CONTROLS",
                    new Vector2(m_graphics.PreferredBackBufferWidth / 10, m_graphics.PreferredBackBufferHeight / 10), Color.White);
                
                float bottom = drawMenuItem(m_font, 
                    "Move Up: " + controlsState.up,
                    200, 
                    m_currentSelection == Selection.Up ? Color.Yellow : Color.Blue);
                bottom = drawMenuItem(m_font, "Move Down: " + controlsState.down, bottom, m_currentSelection == Selection.Down ? Color.Yellow : Color.Blue);
                bottom = drawMenuItem(m_font, "Move Right: " + controlsState.right, bottom, m_currentSelection == Selection.Right ? Color.Yellow : Color.Blue);
                bottom = drawMenuItem(m_font, "Move Left: " + controlsState.left, bottom, m_currentSelection == Selection.Left ? Color.Yellow : Color.Blue);
                drawMenuItem(m_font, "Fire: " + controlsState.fire, bottom, m_currentSelection == Selection.Fire ? Color.Yellow : Color.Blue);
            }

            m_spriteBatch.End();
        }
        
        private float drawMenuItem(SpriteFont font, string text, float y, Color color)
        {
            Vector2 stringSize = font.MeasureString(text);
            m_spriteBatch.DrawString(
                font,
                text,
                new Vector2(m_graphics.PreferredBackBufferWidth / 2 - stringSize.X / 2, y),
                color);

            return y + stringSize.Y;
        }

        public override void update(GameTime gameTime)
        {
        }
    }
}
