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
        private int yMargin;
        private int xMargin;
        private int columns = 32;
        private int rows = 18;
        private int columnWidth;
        private int rowHeight;
        private int gameTop;
        private int gameBottom;
        private int gameLeft;
        private int gameRight;
        private TimeSpan totalGameTime;
        
        // Renderers
        private MushroomRenderer m_mushroomRenderer;
        private PlayerRenderer m_playerRenderer;
        private BulletRenderer m_bulletRenderer;
        private FleaRenderer m_fleaRenderer;
        private CentipedeRenderer m_centipedeRenderer;
        
        // Rendering Components
        private SpriteFont m_font;
        private Texture2D m_mushroomSpriteSheet;
        private Texture2D m_poisonMushroomSpriteSheet;
        private Texture2D m_playerSprite;
        private Texture2D m_bulletSprite;
        private Texture2D m_fleaSpriteSheet;
        private Texture2D m_centipedeHeadSpriteSheet;
        
        // Data Structures
        private List<Mushroom> mushrooms;
        private List<Bullet> bullets;
        private Player player;
        private Flea flea;
        private List<CentipedeSegment> centipedeSegments;
        private bool gameOver;

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
            totalGameTime = TimeSpan.Zero;
            initializeCentipede();
            gameOver = false;
            
            // Renderer Configuration
            m_mushroomRenderer = new MushroomRenderer(m_spriteBatch);
            m_playerRenderer = new PlayerRenderer(m_spriteBatch);
            m_bulletRenderer = new BulletRenderer(m_spriteBatch);
            m_fleaRenderer = new FleaRenderer(m_spriteBatch);
            m_centipedeRenderer = new CentipedeRenderer(m_spriteBatch);
            
            // Arena Configuration
            yMargin = (graphics.PreferredBackBufferHeight % rows) / 2;
            xMargin = (graphics.PreferredBackBufferWidth % columns) / 2;
            gameTop = yMargin;
            gameBottom = graphics.PreferredBackBufferHeight - yMargin;
            gameLeft = xMargin;
            gameRight = graphics.PreferredBackBufferWidth - xMargin;
            columnWidth = (gameRight - gameLeft) / columns;
            rowHeight = (gameBottom - gameTop) / rows;
            Random rand = new Random();
            placeMushrooms(rand);

            // Load From Persistent Local Storage
            loadLayout();
        }

        public void placeMushrooms(Random rand) 
        {
            for (int i = (rows - (rows - 3)); i <= rows / 10 * 8; i++)
            {
                for (int j = 0; j <= columns - 1; j++)
                {
                    int randInt = rand.Next(0, 15);
                    if (randInt == 0)
                    {
                        mushrooms.Add(new Mushroom(
                            new Vector2(
                                columnWidth,
                                rowHeight),
                                new Vector2(
                                    (float) (gameLeft + ((j + 0.5) * columnWidth)),
                                    (float) (gameTop + ((i + 0.5) * rowHeight)))));
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
                    columnWidth,
                    rowHeight),
                lives, 
                score);
        }

        public void initializeCentipede()
        {
            centipedeSegments = new List<CentipedeSegment>();
            double descend = (3 * rowHeight) + rowHeight / 2;
            int y = -50;
            for (int i = 0; i < 8; i++)
            {
                CentipedeSegment centipede = new CentipedeSegment(
                    null, null,
                    new Vector2(
                        (gameRight - gameLeft) / 2,
                        y),
                    new Vector2(
                        columnWidth,
                        rowHeight),
                    new int[] {25, 25, 25, 25, 25, 25, 25, 25},
                    descend + yMargin);
                descend += centipede.Size.Y;
                y -= (int) centipede.Size.Y;
                centipedeSegments.Add(centipede);
            }
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
            m_centipedeHeadSpriteSheet = contentManager.Load<Texture2D>("SpriteSheets/head");
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
            if (gameOver)
            {
                return;
            }
            totalGameTime += gameTime.ElapsedGameTime;
            updateBullets(gameTime);
            updateFlea(gameTime);
            updateCentipede(gameTime);
        }

        private void updateBullets(GameTime gameTime)
        {
            List<Bullet> bulletsToRemove = new List<Bullet>();
            List<Mushroom> mushroomsToRemove = new List<Mushroom>();
            List<CentipedeSegment> centipedesToRemove = new List<CentipedeSegment>();
            List<Mushroom> mushroomsToAdd = new List<Mushroom>();
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
                
                // Check if hit centipede
                foreach (CentipedeSegment centipede in centipedeSegments)
                {
                    
                    if (bullet.Boundary.Intersects(centipede.Boundary))
                    {
                        Mushroom mushroom = new Mushroom(
                            new Vector2(
                                columnWidth,
                                rowHeight),
                            new Vector2(
                                (int) (centipede.Center.X),
                                (int) (centipede.Center.Y)));
                        player.Score += 100;
                        bulletsToRemove.Add(bullet);
                        centipedesToRemove.Add(centipede);
                        mushroomsToAdd.Add(mushroom);
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

            foreach (CentipedeSegment centipede in centipedesToRemove)
            {
                centipedeSegments.Remove(centipede);
            }

            foreach (Mushroom mushroom in mushroomsToAdd)
            {
                mushrooms.Add(mushroom);
            }
        }

        private void updateFlea(GameTime gameTime)
        {
            if (mushrooms.Count < 20 && flea == null)
            {
                Random rand = new Random();
                int x = rand.Next(0, columns);
                flea = new Flea(
                    new Vector2(
                        (float) (xMargin + columnWidth * (x + 0.5)), 0), 
                    new Vector2(
                        columnWidth, 
                        rowHeight),
                    new int[] { 70, 70, 70, 70 }
                    );
                for (int i = 0; i < 5; i++)
                {
                    int y = rand.Next(3, rows - 5);
                    flea.Mushrooms.Add(new Mushroom(
                        new Vector2(
                            columnWidth,
                            rowHeight),
                        new Vector2(
                            flea.Center.X,
                            (float) (yMargin + rowHeight * (y + 0.5)))
                        ));
                }
            }

            if (flea != null)
            {
                // Move Flee
                flea.setPosition(new Vector2(
                    flea.Center.X, flea.Center.Y + (gameTime.ElapsedGameTime.Milliseconds * flea.Speed)));

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
                    if (flea.Center.Y > mushroom.Center.Y)
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
                    if (player.lives == 1)
                    {
                        gameOver = true;
                    }
                    else
                    {
                        flea = null;
                        initializePlayer(player.lives - 1, player.Score);
                    }
                }

                // Remove flea if at bottom of screen
                if (flea != null && flea.Center.Y > gameBottom)
                {
                    flea = null;
                }
            }
        }
        
        private void updateCentipede(GameTime gameTime)
        {
            foreach (CentipedeSegment centipede in centipedeSegments)
            {
                Vector2 newPos;
                Rectangle newBoundary;
                double elapsedDistance = centipede.Speed * gameTime.ElapsedGameTime.TotalMilliseconds;

                if (centipede.Direction == CentipedeSegment.DirectionEnum.right)
                {
                    newPos = new Vector2(
                        (float) (centipede.Center.X + elapsedDistance),
                        centipede.Center.Y);
                }
                else if (centipede.Direction == CentipedeSegment.DirectionEnum.down)
                {
                    newPos = new Vector2(
                        centipede.Center.X,
                        (float) (centipede.Center.Y + elapsedDistance));
                }
                else
                {
                    newPos = new Vector2(
                        (float) (centipede.Center.X - elapsedDistance),
                        centipede.Center.Y);
                }
                
                newBoundary = new Rectangle(
                    (int) (newPos.X - centipede.Size.X / 2),
                    (int) (newPos.Y - centipede.Size.Y / 2),
                    (int) centipede.Size.X,
                    (int) centipede.Size.Y);
                
                moveCentipede(centipede, newPos, newBoundary, elapsedDistance);

                // Update Sprite
                centipede.AnimationTime += gameTime.ElapsedGameTime;
                if (centipede.AnimationTime.TotalMilliseconds >= centipede.SpriteTime[centipede.State])
                {
                    centipede.AnimationTime -= TimeSpan.FromMilliseconds(centipede.SpriteTime[centipede.State]);
                    centipede.State++;
                    centipede.State = centipede.State % centipede.SpriteTime.Length;
                }
                
                // Check if hit player
                if (centipede.Boundary.Intersects(player.Boundary))
                {
                    if (player.lives == 1)
                    {
                        gameOver = true;
                    }
                    else
                    {
                        initializePlayer(player.lives - 1, player.Score);
                    }
                }
            }
        }

        private void moveCentipede(CentipedeSegment centipede, Vector2 position, Rectangle newBoundary, double distance)
        {
            Vector2 newPos = position;
            float rotation = centipede.Rotation;
            double toDescend = centipede.ToDescend;
            double elapsedDistance = distance;
            CentipedeSegment.DirectionEnum direction = centipede.Direction;

            bool collisionDown = false;
            bool collisionRight = false;
            bool collisionLeft = false;
            
            // Check collisions
            foreach (Mushroom mushroom in mushrooms)
            {
                if (newBoundary.Intersects(mushroom.getBoundary()))
                {
                    if (centipede.Direction == CentipedeSegment.DirectionEnum.down)
                    {
                        collisionDown = true;
                    }
                    else if (centipede.Direction == CentipedeSegment.DirectionEnum.right)
                    {
                        collisionRight = true;
                    }
                    else
                    {
                        collisionLeft = true;
                    }
                }
            }

            if (newBoundary.Left < gameLeft)
            {
                collisionLeft = true;
            }
            else if (newBoundary.Right > gameRight)
            {
                collisionRight = true;
            }
            else if (newBoundary.Bottom > gameBottom)
            {
                collisionDown = true;
            }

            if (collisionDown)
            {
                if (centipede.LastDirection == CentipedeSegment.DirectionEnum.left)
                {
                    direction = CentipedeSegment.DirectionEnum.right;
                    newPos = new Vector2(
                        (float) (centipede.Center.X + elapsedDistance),
                        (float) (centipede.Center.Y));
                    centipede.LastDirection = CentipedeSegment.DirectionEnum.right;

                }
                else
                {
                    direction = CentipedeSegment.DirectionEnum.left;
                    newPos = new Vector2(
                        (float) (centipede.Center.X - elapsedDistance),
                        (float) (centipede.Center.Y));
                    centipede.LastDirection = CentipedeSegment.DirectionEnum.left;
                }
                rotation = (float) Math.PI;
            }
            else if (collisionRight)
            {
                centipede.LastDirection = CentipedeSegment.DirectionEnum.right;
                rotation = (float) (3 * Math.PI / 2);
                direction = CentipedeSegment.DirectionEnum.down;
                centipede.ToDescend = centipede.Size.Y;
                newPos = new Vector2(
                    centipede.Center.X,
                    (float) (centipede.Center.Y + elapsedDistance));
            }
            else if (collisionLeft)
            {
                centipede.LastDirection = CentipedeSegment.DirectionEnum.left;
                rotation = (float) (3 * Math.PI / 2);
                direction = CentipedeSegment.DirectionEnum.down;
                centipede.ToDescend = centipede.Size.Y;
                newPos = new Vector2(
                    centipede.Center.X,
                    (float) (centipede.Center.Y +
                             centipede.Speed * elapsedDistance));
            }
            else
            {
                if (centipede.Direction == CentipedeSegment.DirectionEnum.down)
                {
                    if (centipede.ToDescend > 0)
                    {
                        if (elapsedDistance > centipede.ToDescend)
                        {
                            elapsedDistance -= centipede.ToDescend;
                            if (centipede.LastDirection == CentipedeSegment.DirectionEnum.right)
                            {
                                direction = CentipedeSegment.DirectionEnum.left;
                                newPos = new Vector2(
                                    (float) (centipede.Center.X - elapsedDistance),
                                    (float) (centipede.Center.Y + centipede.ToDescend));
                            }
                            else
                            {
                                direction = CentipedeSegment.DirectionEnum.right;
                                newPos = new Vector2(
                                    (float) (centipede.Center.X + elapsedDistance),
                                    (float) (centipede.Center.Y - centipede.ToDescend));
                            }
                            centipede.ToDescend = 0;
                        }
                        else
                        {
                            centipede.ToDescend -= elapsedDistance;
                        }
                    }
                }
            }
            centipede.Rotation = rotation;
            centipede.Direction = direction;
            centipede.setPosition(newPos);
        }

        public override void render(GameTime gameTime)
        {
            m_spriteBatch.Begin();

            if (gameOver)
            {
                renderGameOver(player.Score);
                m_spriteBatch.End();
                return;
            }
            renderTopBar();
            renderMushrooms();
            renderBullets();
            renderPlayer();
            renderFlea();
            renderCentipede();

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

        private void renderGameOver(int score)
        {
            float bottom = (float) (m_graphics.PreferredBackBufferHeight * 0.3);
            bottom = drawMenuItem(m_font, "Final Score: ", bottom, Color.SkyBlue);
            bottom = drawMenuItem(m_font, player.Score.ToString(), bottom, Color.LimeGreen);
        }

        private void renderTopBar( )
        {
            Vector2 stringSize = m_font.MeasureString("High Scores and Lives Here");
            
            m_spriteBatch.DrawString(m_font, "Time: " + totalGameTime.Minutes + ":" + totalGameTime.Seconds,
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

        private void renderCentipede()
        {
            foreach (CentipedeSegment centipede in centipedeSegments)
            {
                m_centipedeRenderer.RenderHead(centipede, m_centipedeHeadSpriteSheet, 8);
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
                player.setPosition(new Vector2(player.Center.X, newRect.Center.Y));
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
                player.setPosition(new Vector2(player.Center.X, newRect.Center.Y));
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
                player.setPosition(new Vector2(newRect.Center.X, player.Center.Y));
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
                player.setPosition(new Vector2(newRect.Center.X, player.Center.Y));
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
