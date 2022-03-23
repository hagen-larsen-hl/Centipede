using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Serialization;
using centipede.Content.Input;
using CS5410.Objects;
using CS5410.Renderers;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CS5410
{
    public class GamePlayView : GameStateView
    {
        // Enemy Frequencies
        private int fleaFrequency = 5;
        private int spiderFrequency = 5;
        private int scorpionFrequency = 5;
        
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
        private ScorpionRenderer m_scorpionRenderer;
        private SpiderRenderer m_spiderRenderer;
        private CentipedeRenderer m_centipedeRenderer;
        
        // Rendering Components
        private SpriteFont m_font;
        private Texture2D m_mushroomSpriteSheet;
        private Texture2D m_poisonMushroomSpriteSheet;
        private Texture2D m_playerSprite;
        private Texture2D m_bulletSprite;
        private Texture2D m_fleaSpriteSheet;
        private Texture2D m_centipedeHeadSpriteSheet;
        private Texture2D m_centipedeBodySpriteSheet;
        private Texture2D m_centipedeHeadRightSpriteSheet;
        private Texture2D m_centipedeBodyRightSpriteSheet;
        private Texture2D m_scorpionSpriteSheet;
        private Texture2D m_spiderSpriteSheet;
        
        // Audio
        private SoundEffect m_fire;
        private SoundEffect m_death;
        private SoundEffect m_hit;
        
        // Data Structures
        private List<Mushroom> mushrooms;
        private List<Bullet> bullets;
        private Player player;
        private Flea flea;
        private Scorpion scorpion;
        private Spider spider;
        private List<CentipedeSegment> centipedeSegments;
        private bool gameOver;
        private bool pause;
        private List<int> scores;

        // Timers
        private TimeSpan fleaTimer;
        private TimeSpan spiderTimer;
        private TimeSpan scorpionTimer;

        // Technical Variables
        private bool loading;
        private bool saving;
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
            flea = null;
            spider = null;
            scorpion = null;
            gameOver = false;
            scores = new List<int>() {0, 0, 0, 0, 0};
            pause = false;

            // Initialize Timers
            fleaTimer = TimeSpan.FromSeconds(fleaFrequency);
            spiderTimer = TimeSpan.FromSeconds(spiderFrequency);
            scorpionTimer = TimeSpan.FromSeconds(scorpionFrequency);
            
            // Renderer Configuration
            m_mushroomRenderer = new MushroomRenderer(m_spriteBatch);
            m_playerRenderer = new PlayerRenderer(m_spriteBatch);
            m_bulletRenderer = new BulletRenderer(m_spriteBatch);
            m_fleaRenderer = new FleaRenderer(m_spriteBatch);
            m_scorpionRenderer = new ScorpionRenderer(m_spriteBatch);
            m_spiderRenderer = new SpiderRenderer(m_spriteBatch);
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
            loadScores();
        }

        public void placeMushrooms(Random rand) 
        {

            for (int i = 0; i < 20; i++)
            {
                int y = rand.Next(3, rows - 5);
                int x = rand.Next(2, columns - 1);
                mushrooms.Add(new Mushroom(
                    new Vector2(
                        columnWidth,
                        rowHeight),
                    new Vector2(
                        (float) (gameLeft + ((x + 0.5) * columnWidth)),
                        (float) (gameTop + ((y + 0.5) * rowHeight)))));
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
            int x = (gameRight - gameLeft) / 2;
            for (int i = 0; i < 8; i++)
            {
                CentipedeSegment centipede = new CentipedeSegment(
                    null, null,
                    new Vector2(
                        x,
                        gameTop + yMargin + rowHeight * 2),
                    new Vector2(
                        columnWidth,
                        rowHeight),
                    new int[] {25, 25, 25, 25, 25, 25, 25, 25},
                    0);
                x -= (int) centipede.Size.X;
                centipedeSegments.Add(centipede);
            }
        }
        
        private void saveScore(List<int> scores)
        {
            lock (this)
            {
                if (!this.saving)
                {
                    this.saving = true;
                    finalizeSaveAsync(scores);
                }
            }
        }
        
        private async void finalizeSaveAsync(List<int> scores)
        {
            await Task.Run(() =>
            {
                using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    try
                    {
                        using (IsolatedStorageFileStream fs = storage.OpenFile("scores.xml", FileMode.OpenOrCreate))
                        {
                            if (fs != null)
                            {
                                XmlSerializer mySerializer = new XmlSerializer(typeof(List<int>));
                                mySerializer.Serialize(fs, scores);
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
        
        private void loadLayout()
        {
            lock (this)
            {
                if (!this.loading)
                {
                    this.loading = true;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    loadLayoutAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
            }
        }

        private async Task loadLayoutAsync()
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
                            m_inputHandler.registerCommand("back", Keys.Escape, true, new InputDeviceHelper.CommandDelegate(navigateBack));
                            m_inputHandler.registerCommand("up", m_keyboardLayout.Up, false, new InputDeviceHelper.CommandDelegate(moveUp));
                            m_inputHandler.registerCommand("down", m_keyboardLayout.Down, false, new InputDeviceHelper.CommandDelegate(moveDown));
                            m_inputHandler.registerCommand("right", m_keyboardLayout.Right, false, new InputDeviceHelper.CommandDelegate(moveRight));
                            m_inputHandler.registerCommand("left", m_keyboardLayout.Left, false, new InputDeviceHelper.CommandDelegate(moveLeft));
                            m_inputHandler.registerCommand("fire", m_keyboardLayout.Fire, false, new InputDeviceHelper.CommandDelegate(fireBullet));
                        }
                        if (storage.FileExists("scores.xml"))
                        {
                            using (IsolatedStorageFileStream fs = storage.OpenFile("scores.xml", FileMode.Open))
                            {
                                if (fs != null)
                                {
                                    XmlSerializer mySerializer = new XmlSerializer(typeof(List<int>));
                                    scores = (List<int>)mySerializer.Deserialize(fs);
                                }
                            }
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
        
        private void loadScores()
        {
            lock (this)
            {
                if (!this.loading)
                {
                    this.loading = true;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    loadScoresAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
            }
        }

        private async Task loadScoresAsync()
        {
            await Task.Run(() =>
            {
                using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    try
                    {
                        if (storage.FileExists("scores.xml"))
                        {
                            using (IsolatedStorageFileStream fs = storage.OpenFile("scores.xml", FileMode.Open))
                            {
                                if (fs != null)
                                {
                                    XmlSerializer mySerializer = new XmlSerializer(typeof(List<int>));
                                    scores = (List<int>)mySerializer.Deserialize(fs);
                                }
                            }
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
            m_centipedeHeadSpriteSheet = contentManager.Load<Texture2D>("SpriteSheets/centipedeHead");
            m_centipedeHeadRightSpriteSheet = contentManager.Load<Texture2D>("SpriteSheets/centipedeHeadRight");
            m_poisonMushroomSpriteSheet = contentManager.Load<Texture2D>("SpriteSheets/poisonMushroom");
            m_centipedeBodySpriteSheet = contentManager.Load<Texture2D>("SpriteSheets/centipedeBody");
            m_centipedeBodyRightSpriteSheet = contentManager.Load<Texture2D>("SpriteSheets/centipedeBodyRight");
            m_spiderSpriteSheet = contentManager.Load<Texture2D>("SpriteSheets/spider");
            m_scorpionSpriteSheet = contentManager.Load<Texture2D>("SpriteSheets/scorpion");

            m_fire = contentManager.Load<SoundEffect>("Audio/fire");
            m_hit = contentManager.Load<SoundEffect>("Audio/hit");
            m_death = contentManager.Load<SoundEffect>("Audio/death");
        }

        public override GameStateEnum processInput(GameTime gameTime)
        {
            m_gameState = GameStateEnum.GamePlay;
            m_inputHandler.Update(gameTime);
            return m_gameState;
        }
        
        public override void update(GameTime gameTime)
        {
            if (gameOver || pause)
            {
                return;
            }

            totalGameTime += gameTime.ElapsedGameTime;
            fleaTimer -= gameTime.ElapsedGameTime;
            spiderTimer -= gameTime.ElapsedGameTime;
            scorpionTimer -= gameTime.ElapsedGameTime;
            updateBullets(gameTime);
            updateFlea(gameTime);
            updateScorpion(gameTime);
            updateSpider(gameTime);
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
                        m_hit.Play();
                        mushroom.state += 1;
                        mushroom.setBoundary();
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
                        m_hit.Play();
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
                    m_hit.Play();
                    flea = null;
                    player.Score += 200;
                    bulletsToRemove.Add(bullet);
                }
                
                // Check if hit scorpion
                if (scorpion != null && bullet.Boundary.Intersects(scorpion.Boundary))
                {
                    m_hit.Play();
                    scorpion = null;
                    player.Score += 1000;
                    bulletsToRemove.Add(bullet);
                }
                
                // Check if hit spider
                if (spider != null && bullet.Boundary.Intersects(spider.Boundary))
                {
                    m_hit.Play();
                    spider = null;
                    player.Score += 500;
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

            if (centipedeSegments.Count == 0)
            {
                initializeCentipede();
            }
        }

        private void updateFlea(GameTime gameTime)
        {
            if (fleaTimer.TotalSeconds <= 0 && flea == null)
            {
                fleaTimer = TimeSpan.FromSeconds(fleaFrequency);
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
                checkPlayerCollision(flea.Boundary);

                // Remove flea if at bottom of screen
                if (flea != null && flea.Center.Y > gameBottom)
                {
                    flea = null;
                }
            }
        }
        
        private void updateScorpion(GameTime gameTime)
        {
            if (scorpionTimer.TotalSeconds <= 0 && scorpion == null)
            {
                scorpionTimer = TimeSpan.FromSeconds(scorpionFrequency);
                Random rand = new Random();
                int y = rand.Next(2, (int) (rows * 0.7));
                scorpion = new Scorpion(
                    new Vector2(
                        0, 
                        (float) (yMargin + rowHeight * y)), 
                    new Vector2(
                        columnWidth, 
                        rowHeight),
                    new int[] { 70, 70, 70, 70 }
                    );
            }

            if (scorpion != null)
            {
                // Move Scorpion
                scorpion.setPosition(new Vector2(
                    scorpion.Center.X + (gameTime.ElapsedGameTime.Milliseconds * scorpion.Speed),
                    scorpion.Center.Y ));

                // Update Sprite
                scorpion.AnimationTime += gameTime.ElapsedGameTime;
                if (scorpion.AnimationTime.TotalMilliseconds >= scorpion.SpriteTime[scorpion.State])
                {
                    scorpion.AnimationTime -= TimeSpan.FromMilliseconds(scorpion.SpriteTime[scorpion.State]);
                    scorpion.State++;
                    scorpion.State = scorpion.State % scorpion.SpriteTime.Length;
                }

                // Check if hit player
                checkPlayerCollision(scorpion.Boundary);
                
                // Check if poisoned mushroom
                foreach (Mushroom mushroom in mushrooms)
                {
                    if (scorpion.Boundary.Intersects(mushroom.getBoundary()))
                    {
                        mushroom.Poison = true;
                    }
                }

                // Remove scorpion if at right of screen
                if (scorpion != null && scorpion.Center.X > gameRight)
                {
                    scorpion = null;
                }
            }
        }
        
        private void updateSpider(GameTime gameTime)
        {
            if (spiderTimer.TotalSeconds <= 0 && spider == null)
            {
                spiderTimer = TimeSpan.FromSeconds(spiderFrequency);
                Random rand = new Random();
                bool down = false;
                float x = (gameRight);
                bool right = false;
                if (rand.Next(0, 2) == 0)
                {
                    down = true;
                    
                }

                if (rand.Next(0, 2) == 1)
                {
                    x = gameLeft;
                    right = true;
                }
                spider = new Spider(
                    new Vector2(
                        x, 
                        (float) (rows * 0.7 * rowHeight)), 
                    new Vector2(
                        columnWidth, 
                        rowHeight),
                    new int[] { 70, 70, 70, 70 },
                    down,
                    right
                    );
            }

            if (spider != null)
            {
                // Move Spider
                if (spider.Down)
                {
                    if (spider.Right)
                    {
                        spider.setPosition(new Vector2(
                            spider.Center.X + (gameTime.ElapsedGameTime.Milliseconds * spider.Speed),
                            spider.Center.Y + (gameTime.ElapsedGameTime.Milliseconds * spider.Speed)));
                    }
                    else
                    {
                        spider.setPosition(new Vector2(
                            spider.Center.X - (gameTime.ElapsedGameTime.Milliseconds * spider.Speed),
                            spider.Center.Y + (gameTime.ElapsedGameTime.Milliseconds * spider.Speed)));
                    }
                }
                else
                {
                    if (spider.Right)
                    {
                        spider.setPosition(new Vector2(
                            spider.Center.X + (gameTime.ElapsedGameTime.Milliseconds * spider.Speed),
                            spider.Center.Y - (gameTime.ElapsedGameTime.Milliseconds * spider.Speed)));
                    }
                    else
                    {
                        spider.setPosition(new Vector2(
                            spider.Center.X - (gameTime.ElapsedGameTime.Milliseconds * spider.Speed),
                            spider.Center.Y - (gameTime.ElapsedGameTime.Milliseconds * spider.Speed)));
                    }
                }

                // Update Sprite
                spider.AnimationTime += gameTime.ElapsedGameTime;
                if (spider.AnimationTime.TotalMilliseconds >= spider.SpriteTime[spider.State])
                {
                    spider.AnimationTime -= TimeSpan.FromMilliseconds(spider.SpriteTime[spider.State]);
                    spider.State++;
                    spider.State = spider.State % spider.SpriteTime.Length;
                }
                
                // Check if hit player
                checkPlayerCollision(spider.Boundary);

                // Bounce Spider Off Top and Bottom Boundary
                if (spider.Center.Y > rows * 0.9 * rowHeight)
                {
                    spider.Down = false;
                }
                else if (spider.Center.Y < rows * 0.6 * rowHeight)
                {
                    spider.Down = true;
                }
                
                // Remove Spider if at Boundary
                if (spider.Center.X > gameRight || 
                    spider.Center.X < gameLeft)
                {
                    spider = null;
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
                else if (centipede.Direction == CentipedeSegment.DirectionEnum.up)
                {
                    newPos = new Vector2(
                        centipede.Center.X,
                        (float) (centipede.Center.Y - elapsedDistance));
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
                checkPlayerCollision(centipede.Boundary);
            }
        }

        private void moveCentipede(CentipedeSegment centipede, Vector2 position, Rectangle newBoundary, double distance)
        {
            Vector2 newPos = position;
            float rotation = centipede.Rotation;
            bool newDescend = false;
            double toDescend = centipede.ToDescend;
            double elapsedDistance = distance;
            CentipedeSegment.DirectionEnum direction = centipede.Direction;
            CentipedeSegment.DirectionEnum newTarget = centipede.Target;

            bool collisionDown = false;
            bool collisionRight = false;
            bool collisionLeft = false;
            bool collisionUp = false;
            
            // Check collisions
            foreach (Mushroom mushroom in mushrooms)
            {
                if (newBoundary.Intersects(mushroom.getBoundary()))
                {
                    if (mushroom.Poison)
                    {
                        direction = CentipedeSegment.DirectionEnum.down;
                        newDescend = true;
                        toDescend = gameBottom - centipede.Center.Y;
                    }
                    if (centipede.Direction == CentipedeSegment.DirectionEnum.down)
                    {
                        collisionDown = true;
                    }
                    else if (centipede.Direction == CentipedeSegment.DirectionEnum.up)
                    {
                        collisionUp = true;
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
                newTarget = CentipedeSegment.DirectionEnum.up;
            }
            
            else if (newBoundary.Bottom < gameTop + rowHeight * 2)
            {
                collisionUp = true;
                newTarget = CentipedeSegment.DirectionEnum.down;
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
            else if (collisionUp)
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
                direction = centipede.Target;
                centipede.ToDescend = centipede.Size.Y;
                if (centipede.Target == CentipedeSegment.DirectionEnum.down)
                {
                    newPos = new Vector2(
                        centipede.Center.X,
                        (float) (centipede.Center.Y + elapsedDistance));
                }
                else
                {
                    newPos = new Vector2(
                        centipede.Center.X,
                        (float) (centipede.Center.Y - elapsedDistance));
                }
            }
            else if (collisionLeft)
            {
                centipede.LastDirection = CentipedeSegment.DirectionEnum.left;
                rotation = (float) (3 * Math.PI / 2);
                direction = centipede.Target;
                centipede.ToDescend = centipede.Size.Y;
                if (centipede.Target == CentipedeSegment.DirectionEnum.down)
                {
                    newPos = new Vector2(
                        centipede.Center.X,
                        (float) (centipede.Center.Y +
                                 centipede.Speed * elapsedDistance));
                }
                else
                {
                    newPos = new Vector2(
                        centipede.Center.X,
                        (float) (centipede.Center.Y -
                                 centipede.Speed * elapsedDistance));
                }
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
                else if (centipede.Direction == CentipedeSegment.DirectionEnum.up)
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
                                    (float) (centipede.Center.Y - centipede.ToDescend));
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

            if (newDescend)
            {
                centipede.ToDescend = toDescend;
            }
            centipede.Rotation = rotation;
            centipede.Direction = direction;
            centipede.setPosition(newPos);
            centipede.Target = newTarget;
        }

        private void checkPlayerCollision(Rectangle boundary)
        {
            if (boundary.Intersects(player.Boundary))
            {
                m_death.Play();
                if (player.lives == 1)
                {
                    foreach (int score in scores)
                    {
                        if (player.Score > score)
                        {
                            scores.Insert(scores.IndexOf(score), player.Score);
                            saveScore(scores);
                            break;
                        }
                    }

                    player.lives -= 1;
                    gameOver = true;
                }
                else
                {
                    initializePlayer(player.lives - 1, player.Score);
                }
            }
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
            if (pause)
            {
                renderPause();
                m_spriteBatch.End();
                return;
            }
            renderTopBar();
            renderMushrooms();
            renderBullets();
            renderPlayer();
            renderFlea();
            renderScorpion();
            renderSpider();
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
            float bottom = (float) (m_graphics.PreferredBackBufferHeight * 0.2);
            bottom = drawMenuItem(m_font, "GAME OVER", bottom, Color.DarkRed);
            bottom = drawMenuItem(m_font, " ", bottom, Color.Aqua);
            bottom = drawMenuItem(m_font, "Final Score:", bottom, Color.SkyBlue);
            bottom = drawMenuItem(m_font, score.ToString(), bottom, Color.SkyBlue);
        }
        
        private void renderPause()
        {
            float bottom = (float) (m_graphics.PreferredBackBufferHeight * 0.2);
            bottom = drawMenuItem(m_font, "PAUSED", bottom, Color.White);
            bottom = drawMenuItem(m_font, " ", bottom, Color.Aqua);
            bottom = drawMenuItem(m_font, "Press SPACE to resume", bottom, Color.SkyBlue);
            bottom = drawMenuItem(m_font, "Press ESC to return to menu", bottom, Color.DarkRed);
        }

        private void renderTopBar( )
        {
            m_spriteBatch.DrawString(m_font, "Time: " + totalGameTime.Minutes + ":" + totalGameTime.Seconds,
                new Vector2(gameLeft, gameTop), Color.White);
            
            m_spriteBatch.DrawString(m_font, "Score: " + player.Score,
                new Vector2((gameRight / 2) - (m_font.MeasureString("Score: XXXX").X / 2), gameTop), Color.White);
            
            m_spriteBatch.DrawString(m_font, "Lives: ",
                new Vector2(gameRight - m_font.MeasureString("Lives: ").X - (3 * player.Size.X), gameTop), Color.White);

            int x = (int) (gameRight - (3 * player.Size.X));
            for (int i = 0; i < player.lives; i++)
            {
                m_spriteBatch.Draw(
                    m_playerSprite,
                    new Rectangle(
                        x, 
                        (int) (gameTop + m_font.MeasureString("Lives: ").Y / 10), 
                        (int) player.Size.X,
                        (int) player.Size.Y),
                    null,
                    Color.White,
                    0,
                    new Vector2(0, 0),
                    SpriteEffects.None,
                    0
                );
                x += (int) player.Size.X;
            }
        }

        private void renderMushrooms()
        {
            foreach (Mushroom mushroom in mushrooms)
            {
                if (mushroom.Poison)
                {
                    m_mushroomRenderer.Render(mushroom, m_poisonMushroomSpriteSheet, 4);
                }
                else
                {
                    m_mushroomRenderer.Render(mushroom, m_mushroomSpriteSheet, 4);
                }
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
        
        private void renderScorpion()
        {
            if (scorpion != null)
            {
                m_scorpionRenderer.Render(scorpion, m_scorpionSpriteSheet, 4);
            }
        }
        
        private void renderSpider()
        {
            if (spider != null)
            {
                m_spiderRenderer.Render(spider, m_spiderSpriteSheet, 4);
            }
        }

        private void renderCentipede()
        {
            int segment = 0;
            foreach (CentipedeSegment centipede in centipedeSegments)
            {
                if (segment == 0)
                {
                    if (centipede.Direction == CentipedeSegment.DirectionEnum.up)
                    {
                        m_centipedeRenderer.Render(centipede, m_centipedeHeadSpriteSheet, 8, SpriteEffects.None);
                    }
                    else if (centipede.Direction == CentipedeSegment.DirectionEnum.down)
                    {
                        m_centipedeRenderer.Render(centipede, m_centipedeHeadSpriteSheet, 8, SpriteEffects.FlipVertically);
                    }
                    else if (centipede.Direction == CentipedeSegment.DirectionEnum.right)
                    {
                        m_centipedeRenderer.Render(centipede, m_centipedeHeadRightSpriteSheet, 8, SpriteEffects.None);
                    }
                    else if (centipede.Direction == CentipedeSegment.DirectionEnum.left)
                    {
                        m_centipedeRenderer.Render(centipede, m_centipedeHeadRightSpriteSheet, 8, SpriteEffects.FlipHorizontally);
                    }
                }
                else
                {
                    if (centipede.Direction == CentipedeSegment.DirectionEnum.up)
                    {
                        m_centipedeRenderer.Render(centipede, m_centipedeBodySpriteSheet, 8, SpriteEffects.None);
                    }
                    else if (centipede.Direction == CentipedeSegment.DirectionEnum.down)
                    {
                        m_centipedeRenderer.Render(centipede, m_centipedeBodySpriteSheet, 8, SpriteEffects.FlipVertically);
                    }
                    else if (centipede.Direction == CentipedeSegment.DirectionEnum.right)
                    {
                        m_centipedeRenderer.Render(centipede, m_centipedeBodyRightSpriteSheet, 8, SpriteEffects.None);
                    }
                    else if (centipede.Direction == CentipedeSegment.DirectionEnum.left)
                    {
                        m_centipedeRenderer.Render(centipede, m_centipedeBodyRightSpriteSheet, 8, SpriteEffects.FlipHorizontally);
                    }
                }
                segment++;
            }
        }

        /*
         * Callback methods utilized by processInput
         */
        private void navigateBack(GameTime gameTime)
        {
            if (pause || gameOver)
            {
                m_gameState = GameStateEnum.MainMenu;
            }
            else
            {
                pause = true;
            }
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
            if (pause)
            {
                pause = false;
                return;
            }
            player.FireDelay -= gameTime.ElapsedGameTime.Milliseconds;
            if (player.FireDelay <= 0 && player.lives > 0)
            {
                m_fire.Play();
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
