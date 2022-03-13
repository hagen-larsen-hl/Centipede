using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using System.Xml.Serialization;
using centipede.Content.Input;
using CS5410.Objects;
using CS5410.Renderers;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CS5410
{
    public class GamePlayView : GameStateView
    {
        // Arena Related Variables
        private int yFrame;
        private int xFrame;
        private int columns = 50;
        private int rows = 40;
        private int gameTop;
        private int gameBottom;
        private int gameLeft;
        private int gameRight;
        private float aspectRatio;
        private TimeSpan totalGameTime;
        
        // Renderers
        private MushroomRenderer m_mushroomRenderer;
        private PlayerRenderer m_playerRenderer;
        private BulletRenderer m_bulletRenderer;
        private FleaRenderer m_fleaRenderer;
        
        // Rendering Components
        private SpriteFont m_font;
        private Texture2D m_mushroomSpriteSheet;
        private Texture2D m_poisonMushroomSpriteSheet;
        private Texture2D m_playerSprite;
        private Texture2D m_bulletSprite;
        private Texture2D m_fleaSpriteSheet;
        
        // Data Structures
        private List<Mushroom> mushrooms;
        private List<Bullet> bullets;
        private Player player;
        private Flea flea;

        // Technical Variables
        private bool loading;
        private Objects.Controls m_keyboardLayout;
        private KeyboardInput m_inputHandler = new KeyboardInput();
        private GameStateEnum m_gameState;

        public override void initialize(GraphicsDevice graphicsDevice, GraphicsDeviceManager graphics)
        {
            m_graphics = graphics;
            m_spriteBatch = new SpriteBatch(graphicsDevice);
            
            // Initialize Data Structures
            mushrooms = new List<Mushroom>();
            bullets = new List<Bullet>();
            initializePlayer(3, 0);
            
            // Renderer Configuration
            m_mushroomRenderer = new MushroomRenderer(m_spriteBatch);
            m_playerRenderer = new PlayerRenderer(m_spriteBatch);
            m_bulletRenderer = new BulletRenderer(m_spriteBatch);
            m_fleaRenderer = new FleaRenderer(m_spriteBatch);
            
            // Arena Configuration
            aspectRatio = graphics.PreferredBackBufferWidth / (float) graphics.PreferredBackBufferHeight;
            yFrame = (graphics.PreferredBackBufferHeight % rows) / 2;
            xFrame = (graphics.PreferredBackBufferWidth % columns) / 2;
            gameTop = yFrame;
            gameBottom = graphics.PreferredBackBufferHeight - yFrame;
            gameLeft = xFrame;
            gameRight = graphics.PreferredBackBufferWidth - xFrame;
            Random rand = new Random();
            placeMushrooms(rand);

            // Load From Persistent Local Storage
            loadLayout();
        }

        public void placeMushrooms(Random rand)
        {
            for (int i = rows / 10; i <= rows / 10 * 8; i++)
            {
                for (int j = 0; j <= columns; j++)
                {
                    int randInt = rand.Next(0, 26);
                    if (randInt == 0)
                    {
                        mushrooms.Add(new Mushroom(
                            new Vector2(
                                (gameRight - gameLeft) / columns,
                                (gameBottom - gameTop) / rows),
                            new Vector2(
                                ((gameRight - gameLeft) / columns * j),
                                ((gameBottom - gameTop) / rows * i))));
                    }
                }
            }
        }
        public void initializePlayer(int lives, int score)
        {
            player = new Player(
                new Vector2(
                    (m_graphics.PreferredBackBufferWidth / 2),
                    (m_graphics.PreferredBackBufferHeight / 10) * 9),
                new Vector2(
                    m_graphics.PreferredBackBufferWidth / 50,
                    m_graphics.PreferredBackBufferHeight / 50 * aspectRatio),
                lives, 
                score);
        }
        
        private void loadLayout()
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
                        if (storage.FileExists("layout.xml"))
                        {
                            using (IsolatedStorageFileStream fs = storage.OpenFile("layout.xml", FileMode.Open))
                            {
                                if (fs != null)
                                {
                                    XmlSerializer mySerializer = new XmlSerializer(typeof(Objects.Controls));
                                    m_keyboardLayout = (Objects.Controls)mySerializer.Deserialize(fs);
                                    m_inputHandler.registerCommand("back", Keys.Escape, true, new InputDeviceHelper.CommandDelegate(navigateBack));
                                    m_inputHandler.registerCommand("up", m_keyboardLayout.Up, false, new InputDeviceHelper.CommandDelegate(moveUp));
                                    m_inputHandler.registerCommand("down", m_keyboardLayout.Down, false, new InputDeviceHelper.CommandDelegate(moveDown));
                                    m_inputHandler.registerCommand("right", m_keyboardLayout.Right, false, new InputDeviceHelper.CommandDelegate(moveRight));
                                    m_inputHandler.registerCommand("left", m_keyboardLayout.Left, false, new InputDeviceHelper.CommandDelegate(moveLeft));
                                    m_inputHandler.registerCommand("fire", m_keyboardLayout.Fire, false, new InputDeviceHelper.CommandDelegate(fireBullet));
                                }
                            }
                        }
                        else
                        {
                            m_keyboardLayout = new Objects.Controls();
                            m_keyboardLayout.Up = Keys.Up;
                            m_keyboardLayout.Down = Keys.Down;
                            m_keyboardLayout.Left = Keys.Left;
                            m_keyboardLayout.Right = Keys.Right;
                            m_keyboardLayout.Fire = Keys.Space;
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
        
        public override void loadContent(ContentManager contentManager)
        {
            m_font = contentManager.Load<SpriteFont>("Fonts/menu");
            m_mushroomSpriteSheet = contentManager.Load<Texture2D>("SpriteSheets/mushroom");
            m_playerSprite = contentManager.Load<Texture2D>("SpriteSheets/player");
            m_bulletSprite = contentManager.Load<Texture2D>("SpriteSheets/bullet");
            m_fleaSpriteSheet = contentManager.Load<Texture2D>("SpriteSheets/flea");
            // m_poisonMushroomSpriteSheet = contentManager.Load<Texture2D>("SpriteSheets/poisonMushroom");
        }

        public override GameStateEnum processInput(GameTime gameTime)
        {
            m_gameState = GameStateEnum.GamePlay;
            m_inputHandler.Update(gameTime);
            return m_gameState;
        }
        
        public override void update(GameTime gameTime)
        {
            totalGameTime += gameTime.ElapsedGameTime;
            updateBullets(gameTime);
            updateFlea(gameTime);
        }

        private void updateBullets(GameTime gameTime)
        {
            List<Bullet> bulletsToRemove = new List<Bullet>();
            List<Mushroom> mushroomsToRemove = new List<Mushroom>();
            foreach (Bullet bullet in bullets)
            {
                // Move bullet
                bullet.setPosition(new Vector2(bullet.Position.X,
                    bullet.Position.Y - (float) (bullet.Speed * gameTime.ElapsedGameTime.Milliseconds)));
                
                // Check if hit mushroom
                foreach (Mushroom mushroom in mushrooms)
                {
                    if (bullet.Boundary.Intersects(mushroom.getBoundary()))
                    {
                        mushroom.state += 1;
                        if (mushroom.state == 4)
                        {
                            mushroomsToRemove.Add(mushroom);
                            player.Score += 4;
                        }
                        bulletsToRemove.Add(bullet);
                    }
                }

                // Check if hit flea
                if (flea != null && bullet.Boundary.Intersects(flea.Boundary))
                {
                    flea = null;
                    player.Score += 200;
                    bulletsToRemove.Add(bullet);
                }

                if (bullet.Position.Y < 0)
                {
                    bulletsToRemove.Add(bullet);
                }
            }

            foreach (Bullet bullet in bulletsToRemove)
            {
                bullets.Remove(bullet);
            }

            foreach (Mushroom mushroom in mushroomsToRemove)
            {
                mushrooms.Remove(mushroom);
            }
        }

        private void updateFlea(GameTime gameTime)
        {
            if (mushrooms.Count < 40 && flea == null)
            {
                Random rand = new Random();
                int x = rand.Next(0, columns);
                flea = new Flea(
                    new Vector2(
                        (m_graphics.PreferredBackBufferWidth / columns) * x, 
                        0), 
                    new Vector2(
                        m_graphics.PreferredBackBufferWidth / 50, 
                        m_graphics.PreferredBackBufferHeight / 50 * aspectRatio),
                    new int[] { 50, 50 }
                    );
                for (int i = 0; i < 5; i++)
                {
                    int y = rand.Next(rows / 10, rows / 10 * 8);
                    flea.Mushrooms.Add(new Mushroom(
                        new Vector2(
                            (gameRight - gameLeft) / columns,
                            (gameBottom - gameTop) / rows),
                        new Vector2(
                            flea.Position.X,
                            y * (gameBottom - gameTop) / rows)
                        ));
                }
            }

            if (flea != null)
            {
                // Move Flee
                flea.setPosition(new Vector2(
                    flea.Position.X, flea.Position.Y + (gameTime.ElapsedGameTime.Milliseconds * flea.Speed)));

                // Update Sprite
                flea.AnimationTime += gameTime.ElapsedGameTime;
                if (flea.AnimationTime.TotalMilliseconds >= flea.SpriteTime[flea.State])
                {
                    flea.AnimationTime -= TimeSpan.FromMilliseconds(flea.SpriteTime[flea.State]);
                    flea.State++;
                    flea.State = flea.State % flea.SpriteTime.Length;
                }
                
                // Drop Mushrooms
                List<Mushroom> toRemove = new List<Mushroom>();
                foreach (Mushroom mushroom in flea.Mushrooms)
                {
                    if (flea.Position.Y > mushroom.Position.Y)
                    {
                        mushrooms.Add(mushroom);
                        toRemove.Add(mushroom);
                    }
                }

                foreach (Mushroom m in toRemove)
                {
                    flea.Mushrooms.Remove(m);
                }
                
                // Check if hit player
                if (flea.Boundary.Intersects(player.Boundary))
                {
                    flea = null;
                    initializePlayer(player.lives - 1, player.Score);
                }

                // Remove flea if at bottom of screen
                if (flea != null && flea.Position.Y > gameBottom)
                {
                    flea = null;
                }
            }
        }

        public override void render(GameTime gameTime)
        {
            m_spriteBatch.Begin();

            renderTopBar();
            renderMushrooms();
            renderBullets();
            renderPlayer();
            renderFlea();

            m_spriteBatch.End();
        }

        private void renderTopBar()
        {
            Vector2 stringSize = m_font.MeasureString("High Scores and Lives Here");
            
            m_spriteBatch.DrawString(m_font, "Time: " + totalGameTime.Minutes + ":" + totalGameTime,
                new Vector2(gameLeft, gameTop), Color.White);
            
            m_spriteBatch.DrawString(m_font, "Score: " + player.Score,
                new Vector2((gameRight / 2) - (m_font.MeasureString("Score: XXXX").X / 2), gameTop), Color.White);
            
            m_spriteBatch.DrawString(m_font, "Lives: " + player.lives,
                new Vector2(gameRight - m_font.MeasureString("Lives: X").X, gameTop), Color.White);
        }

        private void renderMushrooms()
        {
            foreach (Mushroom mushroom in mushrooms)
            {
                m_mushroomRenderer.Render(mushroom, m_mushroomSpriteSheet, 4);
            }
        }

        private void renderPlayer()
        {
            m_playerRenderer.Render(player, m_playerSprite);
        }

        private void renderBullets()
        {
            foreach (Bullet bullet in bullets)
            {
                m_bulletRenderer.Render(bullet, m_bulletSprite);
            }
        }

        private void renderFlea()
        {
            if (flea != null)
            {
                m_fleaRenderer.Render(flea, m_fleaSpriteSheet, 4);
            }
        }

        /*
         * Callback methods utilized by processInput
         */
        private void navigateBack(GameTime gameTime)
        {
            m_gameState = GameStateEnum.MainMenu;
        }
        
        public void moveUp(GameTime gameTime)
        { 
            Rectangle newRect = new Rectangle(
                player.Boundary.Left,
                (int) (player.Boundary.Top - (gameTime.ElapsedGameTime.Milliseconds * player.Speed)), 
                player.Boundary.Width,
                player.Boundary.Height);
            
            bool isCollision = checkMushroomPlayerCollision(newRect);
            
            if (!isCollision && newRect.Top > (m_graphics.PreferredBackBufferHeight / 10) * 6)
            {
                player.setPosition(new Vector2(newRect.Left, newRect.Top));
            }
        }
        public void moveDown(GameTime gameTime)
        {
            Rectangle newRect = new Rectangle(
                player.Boundary.Left,
                (int) (player.Boundary.Top + (gameTime.ElapsedGameTime.Milliseconds * player.Speed)), 
                player.Boundary.Width,
                player.Boundary.Height);

            bool isCollision = checkMushroomPlayerCollision(newRect);
            
            if (!isCollision && newRect.Bottom < (m_graphics.PreferredBackBufferHeight / 10) * 9)
            {
                player.setPosition(new Vector2(newRect.Left, newRect.Top));
            }
            
        }

        public void moveRight(GameTime gameTime)
        {
            Rectangle newRect = new Rectangle(
                (int) (player.Boundary.Left + (gameTime.ElapsedGameTime.Milliseconds * player.Speed)),
                player.Boundary.Top, 
                player.Boundary.Width,
                player.Boundary.Height);

            bool isCollision = checkMushroomPlayerCollision(newRect);
            
            if (!isCollision && newRect.Right < m_graphics.PreferredBackBufferWidth)
            {
                player.setPosition(new Vector2(newRect.Left, newRect.Top));
            }
        }

        public void moveLeft(GameTime gameTime)
        {
            Rectangle newRect = new Rectangle(
                (int) (player.Boundary.Left - (gameTime.ElapsedGameTime.Milliseconds * player.Speed)),
                player.Boundary.Top, 
                player.Boundary.Width,
                player.Boundary.Height);
            

            bool isCollision = checkMushroomPlayerCollision(newRect);
            
            if (!isCollision && newRect.Left > 0)
            {
                player.setPosition(new Vector2(newRect.Left, newRect.Top));
            }
        }

        public void fireBullet(GameTime gameTime)
        {
            player.FireDelay -= gameTime.ElapsedGameTime.Milliseconds;
            if (player.FireDelay <= 0)
            {
                Bullet bullet = new Bullet(
                    new Vector2(
                        player.Boundary.Left + player.Boundary.Width / 2,
                        player.Boundary.Top),
                    new Vector2(
                        player.Boundary.Width / 8,
                        player. Boundary.Height / 8 * 6));
                
                bullets.Add(bullet);
                player.FireDelay = 100;
            }
            
        }

        private bool checkMushroomPlayerCollision(Rectangle newPlayer)
        {
            foreach (Mushroom mushroom in mushrooms)
            {
                if (newPlayer.Intersects(mushroom.getBoundary()))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
