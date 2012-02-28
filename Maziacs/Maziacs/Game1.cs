using System;
using System.Collections.Generic;
using System.Linq;
#if DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;
using Microsoft.Advertising.Mobile.Xna;
#if DEBUG
using Microsoft.Phone.Info;
#endif
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Shell;
using Polenter.Serialization;

namespace Maziacs
{
    public static class GameSettings
    {
        public const string GameName = "Maziacs";
        public const string GameVersion = "1.1";

        public static int[] Swords = { 1053, 928, 803 };
        public static int[] Prisoners = { 951, 826, 701 };
        public static int[] Food = { 1053, 928, 803 };
        public static int[] Maziacs = { 748, 748, 748 };

        public static int[] WinMultiplier = { 1, 2, 3 };

        //public const int MinimumDistance = 200;
        //public const int MaximumDistance = 800;
        public static int[] MinimumDistance = { 150, 250, 400 };
        public static int[] MaximumDistance = { 550, 650, 800 };
        //public static int[] MinimumDistance = { 100, 100, 100 };
        //public static int[] MaximumDistance = { 300, 300, 300 };

        public const int ThresholdSteps = 65;

        public const int PrisonerPoints = 100;
        public const int EnemyPoints = 300;
    }

    public struct SettingsData
    {
        public int Highscore;
        public int Difficulty;
        public bool SoundEnabled;
        public int GamesLaunched;
        public bool GameIsReviewed;

        public void Load()
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

            Highscore = (settings.Contains("Highscore")) ? (int)settings["Highscore"] : 0;
            Difficulty = (settings.Contains("Difficulty")) ? (int)settings["Difficulty"] : 1;
            SoundEnabled = (settings.Contains("SoundEnabled")) ? (bool)settings["SoundEnabled"] : true;
            GamesLaunched = (settings.Contains("GamesLaunched")) ? (int)settings["GamesLaunched"] : 0;
            GameIsReviewed = (settings.Contains("GameIsReviewed")) ? (bool)settings["GameIsReviewed"] : false;
        }

        public void Save()
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

            settings["Highscore"] = Highscore;
            settings["Difficulty"] = Difficulty;
            settings["SoundEnabled"] = SoundEnabled;
            settings["GamesLaunched"] = GamesLaunched;
            settings["GameIsReviewed"] = GameIsReviewed;

            settings.Save();
        }
    }

    public struct GameData
    {
        public Game1.GameState GameState { get; set; }
        public Game1.GameState PreviousGameState { get; set; }
        
        // Global data
        public int PastSteps { get; set; }
        public bool ShowPath { get; set; }
        public int Count { get; set; }
        public int FightCounter { get; set; }
        public int Faith { get; set; }
        public double MinimumMoves { get; set; }

        public bool PlayerAnimationActive { get; set; }
        public bool FightAnimationActive { get; set; }
        public bool WinAnimationActive { get; set; }
        public bool LoseAnimationActive { get; set; }

        public int InstructionsPage { get; set; }

        // Enemy data
        public List<int> EnemyX { get; set; }
        public List<int> EnemyY { get; set; }

        // Item data
        public List<int> ItemX { get; set; }
        public List<int> ItemY { get; set; }
        public List<Maze.State> ItemType { get; set; }

        // Player data
        public int PlayerX { get; set; }
        public int PlayerY { get; set; }
        public int Steps { get; set; }
        public int Score { get; set; }
        public bool HasSword { get; set; }
        public bool HasTreasure { get; set; }
        public float Energy { get; set; }
    }

    public struct MazeData
    {
        //public List<int> CellX { get; set; }
        //public List<int> CellY { get; set; }
        public List<Maze.State> CellState { get; set; }

        public void Save(object data)
        {
#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif

            IsolatedStorageFile myStore = IsolatedStorageFile.GetUserStoreForApplication();

            using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("maze.lvl", FileMode.Create, myStore))
            {
                SharpSerializer serializer = new SharpSerializer(true);
                serializer.Serialize((MazeData)data, stream);
            }

#if DEBUG
            sw.Stop();
            Debug.WriteLine("Saving maze took: {0}", sw.Elapsed);
#endif
        }

        public MazeData Load()
        {
#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif

            MazeData data = new MazeData();

            IsolatedStorageFile myStore = IsolatedStorageFile.GetUserStoreForApplication();

            using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("maze.lvl", FileMode.Open, myStore))
            {
                SharpSerializer serializer = new SharpSerializer(true);

                try
                {
                    data = (MazeData)serializer.Deserialize(stream);
                }
                catch
                {
                    List<string> buttons = new List<string>();
                    buttons.Add("OK");
                    Guide.BeginShowMessageBox("Error", "Your savegame files are corrupt, returning to main menu!", buttons, 0, MessageBoxIcon.Error, null, null);
                }
            }

#if DEBUG
            sw.Stop();
            Debug.WriteLine("Loading maze took: {0}", sw.Elapsed);
#endif

            return data;
        }
    }
    
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {        
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        static AdGameComponent adGameComponent;
        static DrawableAd bannerAd;

        Texture2D logoTexture;
        Texture2D backgroundTexture;
        Texture2D instructionsTexture1;
        Texture2D instructionsTexture2;

        int instructionsPage;

        Texture2D pixelTexture;

        Maze maze;
        Texture2D wallTexture;
        Texture2D pathTexture;
        Texture2D solutionTexture;
        Texture2D foodTexture;
        Texture2D swordTexture;
        Texture2D startTexture;
        Texture2D treasureTexture;
        Texture2D prisonerTexture;
        RenderTarget2D mazeTexture;

        Player player;
        Texture2D playerTexture;
        Vector2 playerPosition;

        Texture2D enemyTexture;
        Dictionary<Vector2, Enemy> enemies;

        Texture2D energyBarTexture;

        Texture2D overlayTexture;

        SpriteFont spriteFont;
        SpriteFont menuFont;

        TouchLocationState previousTouchState;

        Camera2D camera;

        Animation playerAnimation;
        Animation startAnimation;
        Animation treasureAnimation;
        Animation prisonerAnimation;
        Animation swordAnimation;
        Animation foodAnimation;

        Texture2D fightTexture;
        Animation fightAnimation;

        Texture2D winTexture;
        Animation winAnimation;

        Texture2D loseTexture;
        Animation loseAnimation;

        int mazeWidth;
        int mazeHeight;

        int frameCount;

        int pastSteps;

        bool showPath;
#if DEBUG
        int frameRate;
        int frameCounter;
        TimeSpan elapsedTime;

        int numberOfSlowdowns;
#endif
        bool mazeNeedsRedraw;

        Rectangle mazeRectangle;
        Rectangle overlayRectangle;
        Rectangle energyRectangle;

        enum UserInput
        {
            Up,
            Right,
            Down,
            Left,
            Idle,
            False
        }

        enum Collisions
        {
            None,
            Wall,
            Prisoner,
            Sword,
            Food,
            Treasure,
            Start
        }

        public enum GameState
        {
            Menu,
            Instructions,
            GameSettings,
            Loading,
            Playing,
            Paused,
            Fighting,
            GameOver,
            GameWon,
            About,
            Resume
        }

        GameState gameState;
        GameState previousGameState;

        Dictionary<Vector2, Item> items;

        bool soundEnabled;

        int count;
        int fightCounter;
        int energyCounter;

        int faith;

        double minimumMoves;
        double actualMoves;
        int finalScore;

        Random random = new Random();

        SoundEffect playerStep;
        SoundEffect enemyStep;
        SoundEffect playerFight;
        SoundEffect swordDraw;
        SoundEffect foodEating;
        SoundEffect pickupTreasure;
        SoundEffect releasePrisoner;

        TextAnimation tapScreen;

        bool retryAdCreation;
        int adTimeout;

        int highscore;
        int difficulty;

        bool gameLoaded;
        bool loadSaveGame;
        bool saveGameAvailable = false;

        bool gameIsLaunched;
        int gamesLaunched;
        bool gameIsReviewed;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 480;
            graphics.PreferredBackBufferHeight = 800;
            graphics.IsFullScreen = true;
            
            Content.RootDirectory = "Content";

            // Frame rate is 30 fps by default for Windows Phone.
            TargetElapsedTime = TimeSpan.FromTicks(333333);

            // Extend battery life under lock.
            InactiveSleepTime = TimeSpan.FromSeconds(1);

            PhoneApplicationService.Current.Launching += new EventHandler<LaunchingEventArgs>(Game_Launching);
            PhoneApplicationService.Current.Closing += new EventHandler<ClosingEventArgs>(Game_Closing);
            PhoneApplicationService.Current.Deactivated += new EventHandler<DeactivatedEventArgs>(Game_Deactivated);
            PhoneApplicationService.Current.Activated += new EventHandler<ActivatedEventArgs>(Game_Activated);

            TouchPanel.EnabledGestures = GestureType.Tap;

#if DEBUG
            AdGameComponent.Initialize(this, "test_client");
#else
            AdGameComponent.Initialize(this, "644f3cef-dfcc-47c3-a726-b9d7a1b40e53");
#endif
            adGameComponent = AdGameComponent.Current;
            Components.Add(adGameComponent);
        }

        void Game_Launching(object sender, LaunchingEventArgs e)
        {
            gameIsLaunched = true;

            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();

            if (store.FileExists("maze.lvl") && store.FileExists("savegame.sav"))
            {
                saveGameAvailable = true;
            }
        }

        void Game_Activated(object sender, ActivatedEventArgs e)
        {
            if (e.IsApplicationInstancePreserved)
            {
                GameState t = (GameState)PhoneApplicationService.Current.State["gameState"];
                if (t == GameState.Playing || t == GameState.Fighting)
                {
                    previousGameState = t;
                    gameState = GameState.Paused;
                }
            }
            else
            {
                IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();

                if (store.FileExists("maze.lvl") && store.FileExists("savegame.sav"))
                {
                    saveGameAvailable = true;
                    loadSaveGame = true;
                }
                
                //gameState = GameState.Resume;
                gameState = GameState.Menu;
            }
        }

        void Game_Deactivated(object sender, DeactivatedEventArgs e)
        {
            SettingsData data = new SettingsData();

            data.Highscore = highscore;
            data.SoundEnabled = soundEnabled;
            data.Difficulty = difficulty;
            data.GamesLaunched = gamesLaunched;
            data.GameIsReviewed = gameIsReviewed;

            data.Save();

            PhoneApplicationService.Current.State["gameState"] = gameState;
            SaveGameData();
        }

        void Game_Closing(object sender, ClosingEventArgs e)
        {
            SettingsData data = new SettingsData();

            data.Highscore = highscore;
            data.SoundEnabled = soundEnabled;
            data.Difficulty = difficulty;
            data.GamesLaunched = gamesLaunched;
            data.GameIsReviewed = gameIsReviewed;

            data.Save();

            SaveGameData();
        }

        void DoReview(IAsyncResult r)
        {
            int? b = Guide.EndShowMessageBox(r);

            if (b == 0)
            {
                MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
                marketplaceReviewTask.Show();
                gameIsReviewed = true;
            }
        }

        GameData LoadGameData()
        {
            GameData data = new GameData();

            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();

            if (store.FileExists("savegame.sav"))
            {
                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("savegame.sav", FileMode.Open, store))
                {
                    SharpSerializer serializer = new SharpSerializer(true);
                    data = (GameData)serializer.Deserialize(stream);
                }
            }

            return data;
        }

        void SaveGameData()
        {
            if (gameState == GameState.Playing || gameState == GameState.Paused || gameState == GameState.Fighting ||
                previousGameState == GameState.Playing || previousGameState == GameState.Paused || previousGameState == GameState.Fighting)
            {
                GameData data = new GameData();

                data.GameState = gameState;
                data.PreviousGameState = previousGameState;

                data.PastSteps = pastSteps;
                data.ShowPath = showPath;
                data.Count = count;
                data.FightCounter = fightCounter;
                data.Faith = faith;
                data.MinimumMoves = minimumMoves;

                data.PlayerAnimationActive = playerAnimation.Active;
                data.FightAnimationActive = fightAnimation.Active;
                data.WinAnimationActive = winAnimation.Active;
                data.LoseAnimationActive = loseAnimation.Active;

                data.EnemyX = new List<int>();
                data.EnemyY = new List<int>();

                data.ItemX = new List<int>();
                data.ItemY = new List<int>();
                data.ItemType = new List<Maze.State>();

                foreach (KeyValuePair<Vector2, Enemy> kvp in enemies)
                {
                    data.EnemyX.Add((int)kvp.Value.Position.X);
                    data.EnemyY.Add((int)kvp.Value.Position.Y);
                }

                foreach (KeyValuePair<Vector2, Item> kvp in items)
                {
                    data.ItemX.Add((int)kvp.Value.Position.X);
                    data.ItemY.Add((int)kvp.Value.Position.Y);
                    data.ItemType.Add(kvp.Value.Type);
                }

                data.PlayerX = (int)player.Position.X;
                data.PlayerY = (int)player.Position.Y;
                data.Steps = player.Steps;
                data.Score = player.Score;
                data.HasSword = player.HasSword;
                data.HasTreasure = player.HasTreasure;
                data.Energy = player.Energy;

                IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();

                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("savegame.sav", FileMode.Create, store))
                {
                    SharpSerializer serializer = new SharpSerializer(true);
                    serializer.Serialize(data, stream);
                }
            }
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            gameState = GameState.Menu;

            mazeWidth = 257;
            mazeHeight = 257;

            retryAdCreation = false;
            adTimeout = 0;

#if DEBUG
            frameRate = 0;
            frameCounter = 0;
            elapsedTime = TimeSpan.Zero;

            numberOfSlowdowns = 0;
#endif

            instructionsPage = 1;

            SettingsData data = new SettingsData();
            data.Load();

            highscore = data.Highscore;
            soundEnabled = data.SoundEnabled;
            difficulty = data.Difficulty;
            gamesLaunched = data.GamesLaunched;
            gameIsReviewed = data.GameIsReviewed;
            
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

#if DEBUG
            bannerAd = adGameComponent.CreateAd("Image480_80", new Rectangle(0, 0, 480, 80), true);
#else
            bannerAd = adGameComponent.CreateAd("81067", new Rectangle(0, 0, 480, 80), true);
#endif
            bannerAd.ErrorOccurred += new EventHandler<Microsoft.Advertising.AdErrorEventArgs>(bannerAd_ErrorOccurred);
            bannerAd.AdRefreshed += new EventHandler(bannerAd_AdRefreshed);

            backgroundTexture = Content.Load<Texture2D>("background");
            logoTexture = Content.Load<Texture2D>("logo");
            instructionsTexture1 = Content.Load<Texture2D>("Instructions1");
            instructionsTexture2 = Content.Load<Texture2D>("instructions2");

            pixelTexture = Content.Load<Texture2D>("pixel");

            spriteFont = Content.Load<SpriteFont>("SpriteFont");
            menuFont = Content.Load<SpriteFont>("MenuFont");

            tapScreen = new TextAnimation();
            string text = "Tap the screen to continue";
            Vector2 position = new Vector2(
                               (GraphicsDevice.Viewport.Width / 2) - (spriteFont.MeasureString(text).X / 2), 
                               GraphicsDevice.Viewport.Height - (spriteFont.MeasureString(text).Y * 4)
                               );

            tapScreen.Initialize(spriteFont, text, position, Color.White);

            wallTexture = Content.Load<Texture2D>("wall");
            pathTexture = Content.Load<Texture2D>("path");
            solutionTexture = Content.Load<Texture2D>("solution");
            foodTexture = Content.Load<Texture2D>("food");
            swordTexture = Content.Load<Texture2D>("sword");
            prisonerTexture = Content.Load<Texture2D>("prisonerAnimation");

            startTexture = Content.Load<Texture2D>("startAnimation");

            treasureTexture = Content.Load<Texture2D>("treasureAnimation");

            playerAnimation = new Animation();
            playerTexture = Content.Load<Texture2D>("playerAnimation");
            playerAnimation.Initialize(playerTexture, Vector2.Zero, 48, 48, 2, 167, Color.White, 1f, true);

            fightAnimation = new Animation();
            fightTexture = Content.Load<Texture2D>("fightAnimation");
            fightAnimation.Initialize(fightTexture, Vector2.Zero, 48, 48, 6, 300, Color.White, 1f, true);

            winAnimation = new Animation();
            winTexture = Content.Load<Texture2D>("winAnimation");
            winAnimation.Initialize(winTexture, Vector2.Zero, 48, 48, 2, 300, Color.White, 1f, true);

            loseAnimation = new Animation();
            loseTexture = Content.Load<Texture2D>("loseAnimation");
            loseAnimation.Initialize(loseTexture, Vector2.Zero, 48, 48, 2, 300, Color.White, 1f, true);

            enemyTexture = Content.Load<Texture2D>("enemyAnimation");

            energyBarTexture = Content.Load<Texture2D>("energyBar");

            overlayTexture = Content.Load<Texture2D>("overlay");

            playerStep = Content.Load<SoundEffect>("sound/playerStep");
            enemyStep = Content.Load<SoundEffect>("sound/enemyStep");
            playerFight = Content.Load<SoundEffect>("sound/playerFight");
            swordDraw = Content.Load<SoundEffect>("sound/swordDraw");
            foodEating = Content.Load<SoundEffect>("sound/foodEat");
            pickupTreasure = Content.Load<SoundEffect>("sound/treasure");
            releasePrisoner = Content.Load<SoundEffect>("sound/prisoner");
        }
         
        protected override void UnloadContent()
        {
            // Unload any non ContentManager content here
        }

        private UserInput HandleInput()
        {
            TouchCollection touchCollection = TouchPanel.GetState();

            UserInput input = UserInput.False;

            if (touchCollection.Count > 0)
            {
                TouchLocation touchLocation = touchCollection[0];
                Vector2 touchPosition = touchLocation.Position;

                Rectangle rectangleInput = new Rectangle((int)touchLocation.Position.X, (int)touchLocation.Position.Y, 0, 0);

                Rectangle rectangleLeft = new Rectangle(135, 677, 75, 60);
                Rectangle rectangleUp = new Rectangle(210, 629, 60, 75);
                Rectangle rectangleDown = new Rectangle(210, 711, 60, 75);
                Rectangle rectangleRight = new Rectangle(270, 677, 75, 60);

                switch (touchLocation.State)
                {
                    case TouchLocationState.Moved:
                        if (previousTouchState == TouchLocationState.Pressed)
                        {
                            previousTouchState = TouchLocationState.Moved;
                            frameCount = 5;
                        }

                        if (frameCount == 5)
                        {
                            if (rectangleInput.Intersects(rectangleUp))
                            {
                                input = UserInput.Up;
                            }
                            else if (rectangleInput.Intersects(rectangleDown))
                            {
                                input = UserInput.Down;
                            }
                            else if (rectangleInput.Intersects(rectangleLeft))
                            {
                                input = UserInput.Left;
                            }
                            else if (rectangleInput.Intersects(rectangleRight))
                            {
                                input = UserInput.Right;
                            }
                            else
                            {
                                input = UserInput.Idle;
                            }
                        }
                        break;

                    case TouchLocationState.Pressed:
                        previousTouchState = TouchLocationState.Pressed;
                        break;

                    case TouchLocationState.Released:
                        input = UserInput.Idle;
                        break;

                    default:
                        break;
                }
            }

            return input;            
        }

        private Collisions UpdateCollisions(UserInput input)
        {
            Collisions test = Collisions.None;

            int[] dx = { 0, 1, 0, -1, 0, 0 };
            int[] dy = { -1, 0, 1, 0, 0, 0 };

            int x = (int)player.Position.X / 48;
            int y = (int)player.Position.Y / 48;

            int d = (int)input;

            Cell c = maze.Cells[(x + dx[d]) + (y + dy[d]) * mazeWidth];

            if (c.State != Maze.State.Path)
            {
                test = Collisions.Wall;

                if (c.State == Maze.State.Prisoner)
                {
                    test = Collisions.Prisoner;
                    c.State = Maze.State.Wall;
                    items[new Vector2(c.X * 48, c.Y * 48)].IsActive = false;
                    items.Remove(new Vector2(c.X * 48, c.Y * 48));
                    //items[c.X, c.Y].IsActive = false;

                    if (soundEnabled == true)
                    {
                        releasePrisoner.Play();
                    }

                    player.Score += GameSettings.PrisonerPoints;

                    return test;
                }

                if (c.State == Maze.State.Sword)
                {
                    test = Collisions.Sword;

                    if (player.HasSword == false)
                    {
                        c.State = Maze.State.Wall;
                        items[new Vector2(c.X * 48, c.Y * 48)].IsActive = false;
                        items.Remove(new Vector2(c.X * 48, c.Y * 48));
                        player.HasSword = true;

                        if (soundEnabled == true)
                        {
                            swordDraw.Play();
                        }

                        if (player.HasTreasure == true)
                        {
                            c.State = Maze.State.Treasure;
                            player.HasTreasure = false;

                            items.Remove(new Vector2(c.X * 48, c.Y * 48));

                            Vector2 position = new Vector2(c.X * 48, c.Y * 48);
                            Item item = new Item();

                            Animation treasureAnimation = new Animation();
                            treasureAnimation.Initialize(treasureTexture, Vector2.Zero, 48, 48, 1, 400, Color.White, 1f, true);
                            item.Initialize(treasureAnimation, position, Maze.State.Sword);
                            items.Add(position, item);

                            maze.GoalCell = c;
                        }
                    }

                    return test;
                }

                if (c.State == Maze.State.Food)
                {
                    test = Collisions.Food;
                    c.State = Maze.State.Wall;
                    items[new Vector2(c.X * 48, c.Y * 48)].IsActive = false;
                    items.Remove(new Vector2(c.X * 48, c.Y * 48));

                    return test;
                }

                if (c.State == Maze.State.Treasure)
                {
                    test = Collisions.Treasure;
                    c.State = Maze.State.Wall;
                    items[new Vector2(c.X * 48, c.Y * 48)].IsActive = false;
                    items.Remove(new Vector2(c.X * 48, c.Y * 48));
                    player.HasTreasure = true;

                    if (soundEnabled == true)
                    {
                        pickupTreasure.Play();
                    }

                    if (player.HasSword == true)
                    {
                        c.State = Maze.State.Sword;
                        player.HasSword = false;

                        items.Remove(new Vector2(c.X * 48, c.Y * 48));

                        Vector2 position = new Vector2(c.X * 48, c.Y * 48);
                        Item item = new Item();

                        Animation swordAnimation = new Animation();
                        swordAnimation.Initialize(swordTexture, Vector2.Zero, 48, 48, 1, 400, Color.White, 1f, true);
                        item.Initialize(swordAnimation, position, Maze.State.Sword);
                        items.Add(position, item);
                    }

                    return test;
                }

                if (c.State == Maze.State.Start)
                {
                    test = Collisions.Start;
                }
            }

            return test;
        }

        private void UpdatePlayer(GameTime gameTime)
        {
            if (player.Energy <= 0)
            {
                gameState = GameState.GameOver;
            }

            UserInput input;
            Collisions collision;

            input = HandleInput();
            collision = UpdateCollisions(input);

            Vector2 newPosition = Vector2.Zero;

            switch (collision)
            {
                case Collisions.Wall:
                    {
                        player.Move(Vector2.Zero, (int)Player.State.Idle);
                    }
                    break;

                case Collisions.Food:
                    {
                        player.Energy += 150;
                        mazeNeedsRedraw = true;
                        player.Move(Vector2.Zero, (int)Player.State.Idle);

                        if (soundEnabled == true)
                        {
                            foodEating.Play();
                        }
                    }
                    break;

                case Collisions.Prisoner:
                    {
                        if (player.HasTreasure == true)
                        {
                            if (maze.LastRebuildTarget != maze.StartCell)
                            {
                                maze.BuildDistanceTable(maze.StartCell);
                            }

                            maze.FindPath(maze.StartCell, maze.Cells[(int)(player.Position.X / 48) + (int)(player.Position.Y / 48) * mazeWidth]);
                        }
                        else
                        {
                            if (maze.LastRebuildTarget != maze.GoalCell)
                            {
                                maze.BuildDistanceTable(maze.GoalCell);
                            }

                            maze.FindPath(maze.GoalCell, maze.Cells[(int)(player.Position.X / 48) + (int)(player.Position.Y / 48) * mazeWidth]);
                        }
                        
                        pastSteps = player.Steps;
                        showPath = true;
                        mazeNeedsRedraw = true;
                        player.Move(Vector2.Zero, (int)Player.State.Idle);
                    }
                    break;

                case Collisions.Sword:
                    {
                        mazeNeedsRedraw = true;
                        player.Move(Vector2.Zero, (int)Player.State.Idle);
                    }
                    break;

                case Collisions.Treasure:
                    {
                        mazeNeedsRedraw = true;
                        player.Move(Vector2.Zero, (int)Player.State.Idle);
                    }
                    break;

                case Collisions.Start:
                    {
                        if (player.HasTreasure == true)
                        {
                            player.Score *= GameSettings.WinMultiplier[difficulty];
                            gameState = GameState.GameWon;
                        }
                    }
                    break;

                case Collisions.None:
                    {
                        if (input == UserInput.Up)
                        {
                            newPosition = new Vector2(0, player.Width * -1);
                            player.Move(newPosition, Player.State.Up);
                            if (player.Position.Y >= 240 && player.Position.Y < (mazeHeight * 48) - 288)
                            {
                                camera.Move(newPosition);
                                mazeNeedsRedraw = true;
                            }

                            if (soundEnabled == true)
                            {
                                playerStep.Play();
                            }
                        }

                        if (input == UserInput.Down)
                        {
                            newPosition = new Vector2(0, player.Width);
                            player.Move(newPosition, Player.State.Down);
                            if (player.Position.Y > 240 && player.Position.Y <= (mazeHeight * 48) - 288)
                            {
                                camera.Move(newPosition);
                                mazeNeedsRedraw = true;
                            }

                            if (soundEnabled == true)
                            {
                                playerStep.Play();
                            }
                        }

                        if (input == UserInput.Left)
                        {
                            newPosition = new Vector2(player.Width * -1, 0);
                            player.Move(newPosition, Player.State.Left);
                            if (player.Position.X >= 240 && player.Position.X < (mazeWidth * 48) - 288)
                            {
                                camera.Move(newPosition);
                                mazeNeedsRedraw = true;
                            }

                            if (soundEnabled == true)
                            {
                                playerStep.Play();
                            }
                        }

                        if (input == UserInput.Right)
                        {
                            newPosition = new Vector2(player.Width, 0);
                            player.Move(newPosition, Player.State.Right);
                            if (player.Position.X > 240 && player.Position.X <= (mazeWidth * 48) - 288)
                            {
                                camera.Move(newPosition);
                                mazeNeedsRedraw = true;
                            }

                            if (soundEnabled == true)
                            {
                                playerStep.Play();
                            }
                        }

                        if (input == UserInput.Idle)
                        {
                            player.Move(Vector2.Zero, (int)Player.State.Idle);
                        }
                    }
                    break;
            }
            
            if (frameCount == 5)
            {
                frameCount = 0;
            }
            else
            {
                frameCount++;
            }

            if (showPath == true && player.Steps - pastSteps >= GameSettings.ThresholdSteps)
            {
                maze.ClearPath();
                showPath = false;
            }

            player.Update(gameTime);
        }

        private void UpdateEnemies(GameTime gameTime)
        {
            List<Vector2> previousKeys = new List<Vector2>();
            List<Vector2> keys = new List<Vector2>();
            List<Enemy> values = new List<Enemy>();

            int[] dx = { 0, 0, 1, -1, 0 };
            int[] dy = { 1, -1, 0, 0, 0 };

            Rectangle playerRectangle = new Rectangle((int)player.Position.X, (int)player.Position.Y, 48, 48);

            for (int x = mazeRectangle.X / 48; x < mazeRectangle.Right / 48; x++)
            {
                for (int y = mazeRectangle.Y / 48; y < mazeRectangle.Bottom / 48; y++)
                {
                    if (enemies.ContainsKey(new Vector2(x * 48, y * 48)))
                    {
                        Rectangle enemyRectangle = new Rectangle(x * 48, y * 48, 48, 48);

                        if (enemyRectangle.Intersects(playerRectangle))
                        {
                            enemies.Remove(new Vector2(x * 48, y * 48));
                            fightAnimation.Position = player.Position;

                            playerAnimation.Active = false;
                            fightAnimation.Active = true;
                            fightAnimation.Update(gameTime);

                            if (soundEnabled == true)
                            {
                                playerFight.Play();
                            }

                            player.Move(Vector2.Zero, Player.State.Idle);

                            gameState = GameState.Fighting;
                            break;
                        }

                        if (count == 15)
                        {

                            int d;
                            bool found = false;
                            int index;
                            int i = 0;

                            List<int> playerPosition = new List<int>();

                            // right
                            if (player.Position.X > enemies[new Vector2(x * 48, y * 48)].Position.X)
                            {
                                playerPosition.Add(2);
                            }
                            // left
                            else if (player.Position.X < enemies[new Vector2(x * 48, y * 48)].Position.X)
                            {
                                playerPosition.Add(3);
                            }

                            // down
                            if (player.Position.Y > enemies[new Vector2(x * 48, y * 48)].Position.Y)
                            {
                                playerPosition.Add(0);
                            }
                            // up
                            else if (player.Position.Y < enemies[new Vector2(x * 48, y * 48)].Position.Y)
                            {
                                playerPosition.Add(1);
                            }

                            do
                            {
                                if (i >= playerPosition.Count)
                                {
                                    d = random.Next(0, 5);
                                }
                                else
                                {
                                    index = random.Next(0, playerPosition.Count);
                                    d = playerPosition[index];
                                }

                                if (maze.Cells[(x + dx[d]) + (y + dy[d]) * mazeWidth].State == Maze.State.Path)
                                {
                                    enemies[new Vector2(x * 48, y * 48)].Position = new Vector2((x + dx[d]) * 48, (y + dy[d]) * 48);
                                    found = true;
                                }

                                i++;
                            }
                            while (found == false);
                        }

                        previousKeys.Add(new Vector2(x * 48, y * 48));
                        keys.Add(enemies[new Vector2(x * 48, y * 48)].Position);
                        values.Add(enemies[new Vector2(x * 48, y * 48)]);
                    }
                }
            }

            foreach (KeyValuePair<Vector2, Enemy> kvp in enemies)
            {
                kvp.Value.Update(gameTime);
            }

            if (count >= 15)
            {
                count = 0;

                if (keys.Count > 0 && soundEnabled == true)
                {
                    enemyStep.Play();
                }
            }
            else
            {
                count++;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                enemies.Remove(previousKeys[i]);

                if (enemies.ContainsKey(keys[i]))
                {
                    enemies.Add(previousKeys[i], values[i]);
                    enemies[previousKeys[i]].Position = previousKeys[i];
                    enemies[previousKeys[i]].Update(gameTime);
                }
                else
                {
                    enemies.Add(keys[i], values[i]);
                }
            }
        }

        private void RedrawMaze()
        {
            if (mazeNeedsRedraw == false)
            {
                return;
            }

            GraphicsDevice.SetRenderTarget(mazeTexture);

            spriteBatch.Begin();

            int i = 0;
            int j = 0;

            for (int x = mazeRectangle.X / 48; x < mazeRectangle.Right / 48; x++)
            {
                for (int y = mazeRectangle.Y / 48; y < mazeRectangle.Bottom / 48; y++)
                {
                    Vector2 position = new Vector2(i * 48, j * 48);
                    if (maze.Cells[x + y * mazeWidth].State != Maze.State.Path)
                    {
                        spriteBatch.Draw(wallTexture, position, Color.White);

                    }
                    else if (maze.Cells[x + y * mazeWidth].State == Maze.State.Path)
                    {
                        if (maze.Cells[x + y * mazeWidth].IsSolution == true)
                        {
                            spriteBatch.Draw(solutionTexture, position, Color.White);
                        }
                        else
                        {
                            spriteBatch.Draw(pathTexture, position, Color.White);
                        }
                    }
                    j++;
                }
                j = 0;
                i++;
            }

            spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);

            mazeNeedsRedraw = false;
        }

        private void UpdateRectangles()
        {
            int x, y, width, height;

            x = (int)camera.Position.X - (GraphicsDevice.Viewport.Width / 2);
            y = (int)camera.Position.Y - (GraphicsDevice.Viewport.Height / 2);
            width = overlayTexture.Width;
            height = overlayTexture.Height;

            overlayRectangle = new Rectangle(x, y, width, height);

            x = (int)camera.Position.X - (GraphicsDevice.Viewport.Width / 2);
            y = (int)camera.Position.Y - (GraphicsDevice.Viewport.Height / 2) + 84;
            width = (int)player.Energy;
            height = energyBarTexture.Height;

            energyRectangle = new Rectangle(x, y, width, height);

            x = (int)camera.Position.X - (GraphicsDevice.Viewport.Width / 2);
            y = (int)camera.Position.Y - (GraphicsDevice.Viewport.Height / 2) + 112;
            width = GraphicsDevice.Viewport.Width + 48;
            height = GraphicsDevice.Viewport.Height - 272;

            mazeRectangle = new Rectangle(x, y, width, height);
        }

        // GAMESTATE UPDATE FUNCTIONS

        private void UpdateMenu(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                ExitGame();
            }

            if (gameIsLaunched)
            {
                gamesLaunched++;

                if (gamesLaunched == 2 || gamesLaunched == 5 || gamesLaunched == 10)
                {
                    if (!Guide.IsVisible)
                    {
                        List<string> buttons = new List<string>();
                        buttons.Add("Yes");
                        buttons.Add("No");
                        Guide.BeginShowMessageBox("Review", "Would you like to review this game?", buttons, 0, MessageBoxIcon.Alert, DoReview, null);
                    }
                }

                gameIsLaunched = false;
            }

            TouchCollection touchCollection = TouchPanel.GetState();

            if (touchCollection.Count > 0)
            {
                TouchLocation touchLocation = touchCollection[0];
                Vector2 touchPosition = touchLocation.Position;

                Rectangle rectangleInput = new Rectangle((int)touchLocation.Position.X, (int)touchLocation.Position.Y, 0, 0);

                Rectangle resumeRectangle = new Rectangle(0, 394, 480, 60);
                Rectangle startRectangle = new Rectangle(0, 464, 480, 60);
                Rectangle howtoRectangle = new Rectangle(0, 534, 480, 60);
                Rectangle aboutRectangle = new Rectangle(0, 604, 480, 60);
                Rectangle exitRectangle = new Rectangle(0, 674, 480, 60);

                switch (touchLocation.State)
                {
                    case TouchLocationState.Pressed:
                        {
                            previousTouchState = TouchLocationState.Pressed;
                        }
                        break;

                    case TouchLocationState.Released:
                        {
                            if (previousTouchState == TouchLocationState.Pressed)
                            {
                                if (rectangleInput.Intersects(resumeRectangle) && (saveGameAvailable == true || gameLoaded == true))
                                {
                                    loadSaveGame = true;
                                    gameState = GameState.Resume;
                                }

                                if (rectangleInput.Intersects(startRectangle))
                                {
                                    gameState = GameState.GameSettings;
                                }

                                if (rectangleInput.Intersects(howtoRectangle))
                                {
                                    gameState = GameState.Instructions;
                                }

                                if (rectangleInput.Intersects(aboutRectangle))
                                {
                                    gameState = GameState.About;
                                }

                                if (rectangleInput.Intersects(exitRectangle))
                                {
                                    ExitGame();
                                }
                            }

                            previousTouchState = TouchLocationState.Released;
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        private void UpdateInstructions(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                if (gameState == GameState.Instructions)
                {
                    instructionsPage = 1;
                }

                gameState = GameState.Menu;
            }

            tapScreen.Update(gameTime);

            TouchCollection touchCollection = TouchPanel.GetState();

            if (touchCollection.Count > 0)
            {
                TouchLocation touchLocation = touchCollection[0];
                Vector2 touchPosition = touchLocation.Position;

                switch (touchLocation.State)
                {
                    case TouchLocationState.Pressed:
                        {
                            previousTouchState = TouchLocationState.Pressed;
                        }
                        break;

                    case TouchLocationState.Released:
                        {
                            if (previousTouchState == TouchLocationState.Pressed)
                            {
                                switch (instructionsPage)
                                {
                                    case 1:
                                        instructionsPage = 2;
                                        break;
                                    case 2:
                                        instructionsPage = 1;
                                        gameState = GameState.Menu;
                                        break;
                                }
                            }

                            previousTouchState = TouchLocationState.Released;
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        private void UpdateGameSettings(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                gameState = GameState.Menu;
            }

            TouchCollection touchCollection = TouchPanel.GetState();

            if (touchCollection.Count > 0)
            {
                TouchLocation touchLocation = touchCollection[0];
                Vector2 touchPosition = touchLocation.Position;

                Rectangle rectangleInput = new Rectangle((int)touchLocation.Position.X, (int)touchLocation.Position.Y, 0, 0);

                Rectangle startRectangle = new Rectangle(0, 464, 480, 60);
                Rectangle howtoRectangle = new Rectangle(0, 534, 480, 60);
                Rectangle aboutRectangle = new Rectangle(0, 604, 480, 60);
                Rectangle exitRectangle = new Rectangle(0, 674, 480, 60);

                switch (touchLocation.State)
                {
                    case TouchLocationState.Pressed:
                        {
                            previousTouchState = TouchLocationState.Pressed;
                        }
                        break;

                    case TouchLocationState.Released:
                        {
                            if (previousTouchState == TouchLocationState.Pressed)
                            {
                                if (rectangleInput.Intersects(startRectangle))
                                {
                                    gameState = GameState.Loading;
                                }

                                if (rectangleInput.Intersects(howtoRectangle))
                                {
                                    switch (difficulty)
                                    {
                                        case 0:
                                            difficulty = 1;
                                            break;
                                        case 1:
                                            difficulty = 2;
                                            break;
                                        case 2:
                                            difficulty = 0;
                                            break;
                                    }
                                }

                                if (rectangleInput.Intersects(aboutRectangle))
                                {
                                    if (soundEnabled == true)
                                    {
                                        soundEnabled = false;
                                    }
                                    else
                                    {
                                        soundEnabled = true;
                                    }
                                }

                                if (rectangleInput.Intersects(exitRectangle))
                                {
                                    gameState = GameState.Menu;
                                }
                            }

                            previousTouchState = TouchLocationState.Released;
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        private void UpdateLoading(GameTime gameTime)
        {
            if (loadSaveGame == true)
            {
                GameData data = LoadGameData();

                if (data.GameState == GameState.Playing || data.GameState == GameState.Fighting || data.GameState == GameState.Paused ||
                    data.PreviousGameState == GameState.Playing || data.PreviousGameState == GameState.Fighting || data.PreviousGameState == GameState.Paused)
                {
                    pastSteps = data.PastSteps;
                    showPath = data.ShowPath;
                    count = data.Count;
                    fightCounter = data.FightCounter;
                    faith = data.Faith;
                    minimumMoves = data.MinimumMoves;

                    maze = new Maze();
                    maze.Initialize(mazeWidth, mazeHeight, difficulty);

                    items = new Dictionary<Vector2, Item>();

                    MazeData mazeData = new MazeData().Load();

                    int x = 0;
                    int y = 0;

                    for (int i = 0; i < mazeData.CellState.Count; i++)
                    {
                        maze.Cells[i] = new Cell();
                        maze.Cells[i].X = x;
                        maze.Cells[i].Y = y;
                        maze.Cells[i].State = mazeData.CellState[i];

                        if (maze.Cells[i].State == Maze.State.Start)
                        {
                            maze.StartCell = maze.Cells[i];
                        }

                        if (maze.Cells[i].State == Maze.State.Treasure)
                        {
                            maze.GoalCell = maze.Cells[i];
                        }

                        if (x == mazeWidth - 1)
                        {
                            x = 0;
                            y++;
                        }
                        else
                        {
                            x++;
                        }
                    }

                    player = new Player();
                    player.Initialize(playerAnimation, new Vector2(data.PlayerX, data.PlayerY));
                    player.Steps = data.Steps;
                    player.Score = data.Score;
                    player.HasSword = data.HasSword;
                    player.HasTreasure = data.HasTreasure;
                    player.Energy = data.Energy;

                    playerAnimation.newFrameRow = 0;

                    playerAnimation.Active = data.PlayerAnimationActive;
                    fightAnimation.Active = data.FightAnimationActive;
                    winAnimation.Active = data.WinAnimationActive;
                    loseAnimation.Active = data.LoseAnimationActive;

                    playerAnimation.Position = player.Position;
                    fightAnimation.Position = player.Position;
                    winAnimation.Position = player.Position;
                    loseAnimation.Position = player.Position;

                    player.Move(Vector2.Zero, Player.State.Idle);

                    camera = new Camera2D();
                    camera.Position = new Vector2(data.PlayerX + 24, data.PlayerY + 48);

                    if (data.PlayerX < 240)
                    {
                        camera._pos.X = 240 + 24;
                    }
                    else if (data.PlayerX > (mazeWidth * 48) - 288)
                    {
                        camera._pos.X = ((mazeWidth * 48) - 288) + 24;
                    }

                    if (data.PlayerY < 240)
                    {
                        camera._pos.Y = 240 + 48;
                    }
                    else if (data.PlayerY > (mazeHeight * 48) - 288)
                    {
                        camera._pos.Y = ((mazeHeight * 48) - 288) + 48;
                    }

                    int width, height;

                    x = (int)camera.Position.X - (GraphicsDevice.Viewport.Width / 2);
                    y = (int)camera.Position.Y - (GraphicsDevice.Viewport.Height / 2) + 112;
                    width = GraphicsDevice.Viewport.Width + 48;
                    height = GraphicsDevice.Viewport.Height - 272;

                    mazeRectangle = new Rectangle(x, y, width, height);
                    mazeTexture = new RenderTarget2D(GraphicsDevice, mazeRectangle.Width, mazeRectangle.Height);

                    enemies = new Dictionary<Vector2, Enemy>();

                    for (int i = 0; i < data.EnemyX.Count; i++)
                    {
                        Vector2 position = new Vector2(data.EnemyX[i], data.EnemyY[i]);
                        Enemy enemy = new Enemy();

                        Animation enemyAnimation = new Animation();
                        enemyAnimation.Initialize(enemyTexture, Vector2.Zero, 48, 48, 2, 500, Color.White, 1f, true);
                        enemy.Initialize(enemyAnimation, position);
                        enemies.Add(position, enemy);
                    }

                    items = new Dictionary<Vector2, Item>();

                    for (int i = 0; i < data.ItemX.Count; i++)
                    {
                        Item item = new Item();
                        Vector2 position = new Vector2(data.ItemX[i], data.ItemY[i]);

                        switch (data.ItemType[i])
                        {
                            case Maze.State.Prisoner:
                                prisonerAnimation = new Animation();
                                prisonerAnimation.Initialize(prisonerTexture, Vector2.Zero, 48, 48, 2, 400, Color.White, 1f, true);
                                item.Initialize(prisonerAnimation, position, Maze.State.Prisoner);
                                items.Add(position, item);
                                break;
                            case Maze.State.Sword:
                                swordAnimation = new Animation();
                                swordAnimation.Initialize(swordTexture, Vector2.Zero, 48, 48, 1, 400, Color.White, 1f, true);
                                item.Initialize(swordAnimation, position, Maze.State.Sword);
                                items.Add(position, item);
                                break;
                            case Maze.State.Food:
                                foodAnimation = new Animation();
                                foodAnimation.Initialize(foodTexture, Vector2.Zero, 48, 48, 1, 400, Color.White, 1f, true);
                                item.Initialize(foodAnimation, position, Maze.State.Food);
                                items.Add(position, item);
                                break;
                            case Maze.State.Treasure:
                                treasureAnimation = new Animation();
                                treasureAnimation.Initialize(treasureTexture, Vector2.Zero, 48, 48, 2, 400, Color.White, 1f, true);
                                item.Initialize(treasureAnimation, position, Maze.State.Treasure);
                                items.Add(position, item);
                                break;
                            case Maze.State.Start:
                                startAnimation = new Animation();
                                startAnimation.Initialize(startTexture, Vector2.Zero, 48, 48, 2, 400, Color.White, 1f, true);
                                item.Initialize(startAnimation, position, Maze.State.Start);
                                items.Add(position, item);
                                break;
                            default:
                                break;
                        }
                    }

                    for (int i = 0; i < maze.Cells.Length; i++)
                    {
                        if (maze.Cells[i].State == Maze.State.Prisoner ||
                            maze.Cells[i].State == Maze.State.Food ||
                            maze.Cells[i].State == Maze.State.Start ||
                            maze.Cells[i].State == Maze.State.Sword ||
                            maze.Cells[i].State == Maze.State.Treasure
                           )
                        {
                            if (items.ContainsKey(new Vector2(maze.Cells[i].X * 48, maze.Cells[i].Y * 48)) == false)
                            {
                                maze.Cells[i].State = Maze.State.Wall;
                            }
                        }
                    }

                    if (showPath == true)
                    {
                        if (player.HasTreasure == true)
                        {
                            maze.BuildDistanceTable(maze.StartCell);
                            maze.FindPath(maze.StartCell, maze.Cells[(int)(player.Position.X / 48) + (int)(player.Position.Y / 48) * mazeWidth]);
                        }
                        else
                        {
                            maze.BuildDistanceTable(maze.GoalCell);
                            maze.FindPath(maze.GoalCell, maze.Cells[(int)(player.Position.X / 48) + (int)(player.Position.Y / 48) * mazeWidth]);
                        }
                    }

                    mazeNeedsRedraw = true;
                    RedrawMaze();

                    loadSaveGame = false;

                    if (data.GameState == GameState.Paused)
                    {
                        if (saveGameAvailable == true)
                        {
                            gameState = data.PreviousGameState;
                        }
                        else
                        {
                            previousGameState = data.PreviousGameState;
                            gameState = data.GameState;
                        }
                    }
                    else if (data.GameState == GameState.Playing)
                    {
                        if (saveGameAvailable == true)
                        {
                            gameState = data.GameState;
                        }
                        else
                        {
                            previousGameState = GameState.Playing;
                            gameState = GameState.Paused;
                        }
                    }
                    else if (data.GameState == GameState.Fighting)
                    {
                        if (saveGameAvailable == true)
                        {
                            gameState = data.GameState;
                        }
                        else
                        {
                            previousGameState = GameState.Fighting;
                            gameState = GameState.Paused;
                        }
                    }
                    else
                    {
                        if (saveGameAvailable == true)
                        {
                            gameState = data.PreviousGameState;
                        }
                        else
                        {
                            //gameState = data.GameState;

                            //if (gameState == GameState.Instructions)
                            //{
                            //    instructionsPage = data.InstructionsPage;
                            //}
                            gameState = GameState.Menu;
                        }                        
                    }

                    if (saveGameAvailable == true)
                    {
                        saveGameAvailable = false;
                    }
                }
            }
            else
            {
                maze = new Maze();
                player = new Player();
                camera = new Camera2D();

                pastSteps = 0;

                showPath = false;

                count = 0;
                fightCounter = 0;
                energyCounter = 0;

                faith = 0;

                mazeNeedsRedraw = true;

                playerAnimation.Active = true;
                fightAnimation.Active = false;
                winAnimation.Active = false;
                loseAnimation.Active = false;

                items = new Dictionary<Vector2, Item>();

                maze.Initialize(mazeWidth, mazeHeight, difficulty);
                maze.Generate();

                MazeData mazeData = new MazeData();
                mazeData.CellState = new List<Maze.State>();

                foreach (Cell c in maze.Cells)
                {
                    mazeData.CellState.Add(c.State);
                }

                mazeData.Save(mazeData);

                minimumMoves = (maze.StartCell.Distance * 2) - 4;

                int[] dx = { 0, 0, -1, 1 };
                int[] dy = { -1, 1, 0, 0 };

                for (int i = 0; i < 4; i++)
                {
                    if (maze.Cells[(maze.StartCell.X + dx[i]) + (maze.StartCell.Y + dy[i]) * mazeWidth].State == Maze.State.Path)
                    {
                        playerPosition = new Vector2((maze.StartCell.X + dx[i]) * 48, (maze.StartCell.Y + dy[i]) * 48);
                    }
                }

                player.Initialize(playerAnimation, playerPosition);
                playerAnimation.newFrameRow = 0;

                camera.Position = new Vector2(playerPosition.X + 24, playerPosition.Y + 48);

                if (playerPosition.X < 240)
                {
                    camera._pos.X = 240 + 24;
                }
                else if (playerPosition.X > (mazeWidth * 48) - 288)
                {
                    camera._pos.X = ((mazeWidth * 48) - 288) + 24;
                }

                if (playerPosition.Y < 240)
                {
                    camera._pos.Y = 240 + 48;
                }
                else if (playerPosition.Y > (mazeHeight * 48) - 288)
                {
                    camera._pos.Y = ((mazeHeight * 48) - 288) + 48;
                }

                int x, y, width, height;

                x = (int)camera.Position.X - (GraphicsDevice.Viewport.Width / 2);
                y = (int)camera.Position.Y - (GraphicsDevice.Viewport.Height / 2) + 112;
                width = GraphicsDevice.Viewport.Width + 48;
                height = GraphicsDevice.Viewport.Height - 272;

                mazeRectangle = new Rectangle(x, y, width, height);

                mazeTexture = new RenderTarget2D(GraphicsDevice, mazeRectangle.Width, mazeRectangle.Height);

                for (x = 0; x < mazeWidth; x++)
                {
                    for (y = 0; y < mazeHeight; y++)
                    {
                        Cell c = maze.Cells[x + y * mazeWidth];
                        Vector2 position = new Vector2(x * 48, y * 48);
                        Item item = new Item();
                        if (c.State == Maze.State.Prisoner)
                        {
                            prisonerAnimation = new Animation();
                            prisonerAnimation.Initialize(prisonerTexture, Vector2.Zero, 48, 48, 2, 400, Color.White, 1f, true);
                            item.Initialize(prisonerAnimation, position, Maze.State.Prisoner);
                            items.Add(position, item);
                        }

                        if (c.State == Maze.State.Sword)
                        {
                            swordAnimation = new Animation();
                            swordAnimation.Initialize(swordTexture, Vector2.Zero, 48, 48, 1, 400, Color.White, 1f, true);
                            item.Initialize(swordAnimation, position, Maze.State.Sword);
                            items.Add(position, item);
                        }

                        if (c.State == Maze.State.Food)
                        {
                            foodAnimation = new Animation();
                            foodAnimation.Initialize(foodTexture, Vector2.Zero, 48, 48, 1, 400, Color.White, 1f, true);
                            item.Initialize(foodAnimation, position, Maze.State.Food);
                            items.Add(position, item);
                        }

                        if (c.State == Maze.State.Treasure)
                        {
                            treasureAnimation = new Animation();
                            treasureAnimation.Initialize(treasureTexture, Vector2.Zero, 48, 48, 2, 400, Color.White, 1f, true);
                            item.Initialize(treasureAnimation, position, Maze.State.Treasure);
                            items.Add(position, item);
                        }

                        if (c.State == Maze.State.Start)
                        {
                            startAnimation = new Animation();
                            startAnimation.Initialize(startTexture, Vector2.Zero, 48, 48, 2, 400, Color.White, 1f, true);
                            item.Initialize(startAnimation, position, Maze.State.Start);
                            items.Add(position, item);
                        }
                    }
                }

                enemies = new Dictionary<Vector2, Enemy>();

                for (int i = 0; i < GameSettings.Maziacs[difficulty]; i++)
                {
                    Animation enemyAnimation = new Animation();
                    enemyAnimation.Initialize(enemyTexture, Vector2.Zero, 48, 48, 2, 500, Color.White, 1f, true);

                    Vector2 enemyPosition = Vector2.Zero;
                    bool found = false;

                    while (found == false)
                    {
                        x = random.Next(0, mazeWidth - 1);
                        y = random.Next(0, mazeHeight - 1);

                        if (x % 2 == 0)
                        {
                            x++;
                        }

                        if (y % 2 == 0)
                        {
                            y++;
                        }

                        if (maze.Cells[x + y * mazeWidth].State == Maze.State.Path && enemies.ContainsKey(new Vector2(x * 48, y * 48)) == false)
                        {
                            if (x < (player.Position.X / 48) - 5 || x > (player.Position.X / 48) + 5)
                            {
                                if (y < (player.Position.Y / 48) - 5 || y > (player.Position.Y / 48) + 5)
                                {
                                    enemyPosition = new Vector2(x * 48, y * 48);
                                    found = true;
                                }
                            }
                        }
                    }

                    Enemy enemy = new Enemy();
                    enemy.Initialize(enemyAnimation, enemyPosition);

                    enemies.Add(enemyPosition, enemy);
                }

                gameState = GameState.Playing;
            }
        }

        private void UpdatePlaying(GameTime gameTime)
        {            
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                previousGameState = GameState.Playing;
                gameState = GameState.Paused;
            }
#if DEBUG
            elapsedTime += gameTime.ElapsedGameTime;

            if (elapsedTime > TimeSpan.FromSeconds(1))
            {
                elapsedTime -= TimeSpan.FromSeconds(1);
                frameRate = frameCounter;
                frameCounter = 0;
            }
#endif
            UpdatePlayer(gameTime);

            UpdateEnemies(gameTime);

            UpdateRectangles();

            RedrawMaze();

            for (int x = mazeRectangle.X / 48; x < mazeRectangle.Right / 48; x++)
            {
                for (int y = mazeRectangle.Y / 48; y < mazeRectangle.Bottom / 48; y++)
                {
                    if (items.ContainsKey(new Vector2(x * 48, y * 48)))
                    {
                        items[new Vector2(x * 48, y * 48)].Update(gameTime);
                    }
                }
            }
        }

        private void UpdatePaused(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                gameState = previousGameState;
            }

            TouchCollection touchCollection = TouchPanel.GetState();

            if (touchCollection.Count > 0)
            {
                TouchLocation touchLocation = touchCollection[0];
                Vector2 touchPosition = touchLocation.Position;

                Rectangle rectangleInput = new Rectangle((int)touchLocation.Position.X, (int)touchLocation.Position.Y, 0, 0);

                Rectangle continueRectangle = new Rectangle(0, 464, 480, 60);
                Rectangle restartRectangle = new Rectangle(0, 534, 480, 60);
                Rectangle soundRectnalge = new Rectangle(0, 604, 480, 60);
                Rectangle quitRectangle = new Rectangle(0, 674, 480, 60);

                switch (touchLocation.State)
                {
                    case TouchLocationState.Pressed:
                        {
                            previousTouchState = TouchLocationState.Pressed;
                        }
                        break;

                    case TouchLocationState.Released:
                        {
                            if (previousTouchState == TouchLocationState.Pressed)
                            {
                                if (rectangleInput.Intersects(continueRectangle))
                                {
                                    gameState = previousGameState;
                                }

                                if (rectangleInput.Intersects(restartRectangle))
                                {
                                    gameState = GameState.Loading;
                                }

                                if (rectangleInput.Intersects(soundRectnalge))
                                {
                                    if (soundEnabled == true)
                                    {
                                        soundEnabled = false;
                                    }
                                    else
                                    {
                                        soundEnabled = true;
                                    }
                                }

                                if (rectangleInput.Intersects(quitRectangle))
                                {
                                    gameState = GameState.Menu;
                                }
                            }

                            previousTouchState = TouchLocationState.Released;
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        private void UpdateFighting(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                previousGameState = GameState.Fighting;
                gameState = GameState.Paused;
            }

            if (player.Energy <= 0)
            {
                gameState = GameState.GameOver;
            }

            if (playerAnimation.Active == false)
            {
                if (fightAnimation.Active == true)
                {
                    if (fightCounter > 108 && player.HasSword == true)
                    {
                        playerAnimation.Active = true;
                        player.HasSword = false;
                        playerAnimation.elapsedTime = playerAnimation.frameTime + 1;
                        player.Move(Vector2.Zero, Player.State.Idle);
                        player.Update(gameTime);
                        playerAnimation.Active = false;

                        fightCounter = 0;
                        fightAnimation.Active = false;
                        winAnimation.Active = true;
                        winAnimation.Position = player.Position;
                        winAnimation.Update(gameTime);
                    }
                    else if (fightCounter > 216 && player.HasSword == false)
                    {
                        fightCounter = 0;
                        fightAnimation.Active = false;

                        faith = random.Next(0, 2);

                        if (faith == 1)
                        {
                            loseAnimation.Active = true;
                            loseAnimation.Position = player.Position;
                            loseAnimation.Update(gameTime);
                        }
                        else
                        {
                            winAnimation.Active = true;
                            winAnimation.Position = player.Position;
                            winAnimation.Update(gameTime);
                        }
                    }
                    else
                    {
                        if (fightCounter - energyCounter == 18 && soundEnabled == true)
                        {
                            float[] pitch = { -0.4f, 0.0f, 0.4f };
                            int index = random.Next(0, 3);

                            playerFight.Play(1f, pitch[index], 0f);
                        }

                        if (fightCounter - energyCounter == 18)
                        {
                            energyCounter = fightCounter;
                            if (player.HasSword == true)
                            {
                                player.Energy -= 6.25f;
                            }
                            else
                            {
                                player.Energy -= 7.812f;
                            }
                        }

                        fightAnimation.Update(gameTime);
                        fightCounter++;
                    }
                }
                else
                {
                    if (fightCounter > 60)
                    {
                        if (faith == 1)
                        {
                            gameState = GameState.GameOver;
                        }
                        else
                        {
                            fightCounter = 0;
                            winAnimation.Active = false;
                            playerAnimation.Active = true;
                            energyCounter = 0;

                            player.Score += GameSettings.EnemyPoints;

                            gameState = GameState.Playing;
                        }
                    }
                    else
                    {
                        if (faith == 1)
                        {
                            loseAnimation.Position = player.Position;
                            loseAnimation.Update(gameTime);
                        }
                        else
                        {
                            winAnimation.Position = player.Position;
                            winAnimation.Update(gameTime);
                        }
                        fightCounter++;
                    }
                }
            }

#if DEBUG
            elapsedTime += gameTime.ElapsedGameTime;

            if (elapsedTime > TimeSpan.FromSeconds(1))
            {
                elapsedTime -= TimeSpan.FromSeconds(1);
                frameRate = frameCounter;
                frameCounter = 0;
            }
#endif

            for (int x = mazeRectangle.X / 48; x < mazeRectangle.Right / 48; x++)
            {
                for (int y = mazeRectangle.Y / 48; y < mazeRectangle.Bottom / 48; y++)
                {
                    if (items.ContainsKey(new Vector2(x * 48, y * 48)))
                    {
                        items[new Vector2(x * 48, y * 48)].Update(gameTime);
                    }
                }
            }

            UpdateRectangles();
        }

        private void UpdateGameOver(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                if (gameState == GameState.Instructions)
                {
                    instructionsPage = 1;
                }

                gameState = GameState.Menu;
            }

            tapScreen.Update(gameTime);

            TouchCollection touchCollection = TouchPanel.GetState();

            if (touchCollection.Count > 0)
            {
                TouchLocation touchLocation = touchCollection[0];
                Vector2 touchPosition = touchLocation.Position;

                switch (touchLocation.State)
                {
                    case TouchLocationState.Pressed:
                        {
                            previousTouchState = TouchLocationState.Pressed;
                        }
                        break;

                    case TouchLocationState.Released:
                        {
                            if (previousTouchState == TouchLocationState.Pressed)
                            {
                                gameState = GameState.Menu;
                            }

                            previousTouchState = TouchLocationState.Released;
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        private void UpdateGameWon(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                if (gameState == GameState.Instructions)
                {
                    instructionsPage = 1;
                }

                gameState = GameState.Menu;
            }

            tapScreen.Update(gameTime);

            TouchCollection touchCollection = TouchPanel.GetState();

            if (touchCollection.Count > 0)
            {
                TouchLocation touchLocation = touchCollection[0];
                Vector2 touchPosition = touchLocation.Position;

                switch (touchLocation.State)
                {
                    case TouchLocationState.Pressed:
                        {
                            previousTouchState = TouchLocationState.Pressed;
                        }
                        break;

                    case TouchLocationState.Released:
                        {
                            if (previousTouchState == TouchLocationState.Pressed)
                            {
                                gameState = GameState.Menu;
                            }

                            previousTouchState = TouchLocationState.Released;
                        }
                        break;

                    default:
                        break;
                }
            }

            actualMoves = player.Steps;
            finalScore = (int)Math.Round(player.Score * ((minimumMoves / actualMoves) + 1));

            if (finalScore > highscore)
            {
                highscore = finalScore;
            }
        }

        private void UpdateAbout(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                gameState = GameState.Menu;
            }

            TouchCollection touchCollection = TouchPanel.GetState();

            if (touchCollection.Count > 0)
            {
                TouchLocation touchLocation = touchCollection[0];
                Vector2 touchPosition = touchLocation.Position;

                Rectangle rectangleInput = new Rectangle((int)touchLocation.Position.X, (int)touchLocation.Position.Y, 0, 0);

                Rectangle rateRectangle = new Rectangle(0, 464, 480, 60);
                Rectangle feedbackRectangle = new Rectangle(0, 534, 480, 60);
                Rectangle findRectangle = new Rectangle(0, 604, 480, 60);
                Rectangle quitRectangle = new Rectangle(0, 674, 480, 60);

                switch (touchLocation.State)
                {
                    case TouchLocationState.Pressed:
                        {
                            previousTouchState = TouchLocationState.Pressed;
                        }
                        break;

                    case TouchLocationState.Released:
                        {
                            if (previousTouchState == TouchLocationState.Pressed)
                            {
                                if (rectangleInput.Intersects(rateRectangle))
                                {
                                    MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();

                                    marketplaceReviewTask.Show();
                                    gameIsReviewed = true;
                                }

                                if (rectangleInput.Intersects(feedbackRectangle))
                                {
                                    EmailComposeTask emailComposeTask = new EmailComposeTask();

                                    emailComposeTask.Subject = "[" + GameSettings.GameName + " v" + GameSettings.GameVersion + "] ";
                                    emailComposeTask.To = "marceldevries@my89.nl";

                                    emailComposeTask.Show();
                                }

                                if (rectangleInput.Intersects(findRectangle))
                                {
                                    MarketplaceSearchTask marketplaceSearchTask = new MarketplaceSearchTask();

                                    marketplaceSearchTask.SearchTerms = "Marcel de Vries";

                                    marketplaceSearchTask.Show();
                                }

                                if (rectangleInput.Intersects(quitRectangle))
                                {
                                    gameState = GameState.Menu;
                                }
                            }

                            previousTouchState = TouchLocationState.Released;
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            RetryAdCreation(gameTime);

#if DEBUG
            if (gameTime.IsRunningSlowly == true)
            {
                numberOfSlowdowns++;
            }
#endif
            
            switch (gameState)
            {
                case GameState.Resume:
                    {
                        if (gameLoaded == true)
                        {
                            gameState = GameState.Playing;
                        }
                        else
                        {
                            gameState = GameState.Loading;
                        }
                    }
                    break;

                case GameState.Menu:
                    {
                        UpdateMenu(gameTime);
                    }
                    break;

                case GameState.GameSettings:
                    {
                        UpdateGameSettings(gameTime);
                    }
                    break;

                case GameState.Loading:
                    {
                        UpdateLoading(gameTime);
                        gameLoaded = true;
                    }
                    break;

                case GameState.Playing:
                    {
                        UpdatePlaying(gameTime);
                    }
                    break;

                case GameState.Paused:
                    {
                        UpdatePaused(gameTime);
                    }
                    break;

                case GameState.Fighting:
                    {
                        UpdateFighting(gameTime);
                    }
                    break;

                case GameState.About:
                    {
                        UpdateAbout(gameTime);
                    }
                    break;

                case GameState.Instructions:
                    {
                        UpdateInstructions(gameTime);
                    }
                    break;

                case GameState.GameOver:
                    {
                        UpdateGameOver(gameTime);
                    }
                    break;

                case GameState.GameWon:
                    {
                        UpdateGameWon(gameTime);
                    }
                    break;

                default:
                    break;
            }

            // Allows the game to exit
            //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            //{
            //    this.Exit();
            //}

            //if (gameTime.IsRunningSlowly == true)
            //{
            //    Debug.WriteLine("Game is running slowly! ({0})", DateTime.Now);
            //}

            base.Update(gameTime);
        }

#if DEBUG
        private void DrawDebugInfo()
        {
            //string fps = string.Format("fps: {0}", frameRate);
            //string memoryUsage = string.Format("C: {0:0.0}MB", DeviceStatus.ApplicationCurrentMemoryUsage / 1000000f);
            //string peakUsage = string.Format("P: {0:0.0}MB", DeviceStatus.ApplicationPeakMemoryUsage / 1000000f);
            //string s = "Steps: " + player.Steps;
            //string health = string.Format("Health: {0:0.00}", player.Energy);
            //string position = string.Format("X/Y: {0}/{1}", player.Position.X / 48, player.Position.Y / 48);
            //string slowDowns = string.Format("SD: {0}", numberOfSlowdowns);
            //string score = string.Format("Score: {0}", player.Score);
            //string high = string.Format("H: {0}", highscore);

            //spriteBatch.DrawString(spriteFont, slowDowns, new Vector2(overlayRectangle.X, overlayRectangle.Bottom - spriteFont.MeasureString(slowDowns).Y), Color.White);
            //spriteBatch.DrawString(spriteFont, score, new Vector2(overlayRectangle.X, overlayRectangle.Bottom - (spriteFont.MeasureString(score).Y * 2)), Color.White);
            //spriteBatch.DrawString(spriteFont, high, new Vector2(overlayRectangle.X, overlayRectangle.Bottom - (spriteFont.MeasureString(high).Y * 3)), Color.White);

            //spriteBatch.DrawString(spriteFont, s, new Vector2(overlayRectangle.Right - spriteFont.MeasureString(s).X, overlayRectangle.Bottom - spriteFont.MeasureString(s).Y), Color.White);
            //spriteBatch.DrawString(spriteFont, memoryUsage, new Vector2(overlayRectangle.Right - spriteFont.MeasureString(memoryUsage).X, overlayRectangle.Bottom - (spriteFont.MeasureString(memoryUsage).Y * 3)), Color.White);
            //spriteBatch.DrawString(spriteFont, peakUsage, new Vector2(overlayRectangle.Right - spriteFont.MeasureString(peakUsage).X, overlayRectangle.Bottom - (spriteFont.MeasureString(peakUsage).Y * 2)), Color.White);
            //spriteBatch.DrawString(spriteFont, fps, new Vector2(overlayRectangle.Right - spriteFont.MeasureString(fps).X, overlayRectangle.Bottom - (spriteFont.MeasureString(fps).Y * 4)), Color.White);
            //spriteBatch.DrawString(spriteFont, health, new Vector2(overlayRectangle.Right - spriteFont.MeasureString(health).X, overlayRectangle.Bottom - (spriteFont.MeasureString(health).Y * 5)), Color.White);
            //spriteBatch.DrawString(spriteFont, position, new Vector2(overlayRectangle.Right - spriteFont.MeasureString(position).X, overlayRectangle.Bottom - (spriteFont.MeasureString(position).Y * 6)), Color.White);
        }
#endif

        private void DrawMazeItems(SpriteBatch spriteBatch)
        {
            for (int x = mazeRectangle.X / 48; x < mazeRectangle.Right / 48; x++)
            {
                for (int y = mazeRectangle.Y / 48; y < mazeRectangle.Bottom / 48; y++)
                {
                    if (items.ContainsKey(new Vector2(x * 48, y * 48)))
                    {
                        items[new Vector2(x * 48, y * 48)].Draw(spriteBatch);
                    }
                }
            }
        }

        private void DrawEnemies(SpriteBatch spriteBatch)
        {
            for (int x = mazeRectangle.X / 48; x < mazeRectangle.Right / 48; x++)
            {
                for (int y = mazeRectangle.Y / 48; y < mazeRectangle.Bottom / 48; y++)
                {
                    if (enemies.ContainsKey(new Vector2(x * 48, y * 48)))
                    {
                        enemies[new Vector2(x * 48, y * 48)].Draw(spriteBatch);
                    }
                }
            }
        }

        private void DrawStringStroke(string str, Vector2 pos, Color color, SpriteBatch spriteBatch)
        {
            spriteBatch.DrawString(menuFont, str, new Vector2(pos.X + 2, pos.Y), color);
            spriteBatch.DrawString(menuFont, str, new Vector2(pos.X - 2, pos.Y), color);
            spriteBatch.DrawString(menuFont, str, new Vector2(pos.X, pos.Y + 2), color);
            spriteBatch.DrawString(menuFont, str, new Vector2(pos.X, pos.Y - 2), color);
        }

        // GAMESTATE DRAW FUNCTIONS

        private void DrawMenu(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            spriteBatch.Draw(backgroundTexture, Vector2.Zero, Color.White);
            spriteBatch.Draw(logoTexture, new Vector2(58, 119), Color.White);

            //if (saveGameAvailable == true || gameLoaded == true)
            //{
            //    Rectangle pixelRectangle = new Rectangle(0, 394, 480, 60);

            //    for (int i = 0; i < 5; i++)
            //    {
            //        spriteBatch.Draw(pixelTexture, pixelRectangle, Color.White);
            //        pixelRectangle.Y += 70;
            //    }
            //}
            //else
            //{
            //    Rectangle pixelRectangle = new Rectangle(0, 464, 480, 60);

            //    for (int i = 0; i < 4; i++)
            //    {
            //        spriteBatch.Draw(pixelTexture, pixelRectangle, Color.White);
            //        pixelRectangle.Y += 70;
            //    }
            //}

            string str;
            Vector2 pos;

            if (saveGameAvailable == true || gameLoaded == true)
            {
                str = "Continue";
                pos = new Vector2(
                                 (GraphicsDevice.Viewport.Width / 2) - (menuFont.MeasureString(str).X / 2),
                                 (GraphicsDevice.Viewport.Height / 2) + (menuFont.MeasureString(str).Y * 0f)

                                 );
                DrawStringStroke(str, pos, Color.White, spriteBatch);
                spriteBatch.DrawString(menuFont, str, pos, Color.Black);
            }

            str = "New Game";
            pos = new Vector2(
                             (GraphicsDevice.Viewport.Width / 2) - (menuFont.MeasureString(str).X / 2),
                             (GraphicsDevice.Viewport.Height / 2) + (menuFont.MeasureString(str).Y * 1.5f)

                             );
            DrawStringStroke(str, pos, Color.White, spriteBatch);
            spriteBatch.DrawString(menuFont, str, pos, Color.Black);

            str = "Instructions";
            pos = new Vector2(
                             (GraphicsDevice.Viewport.Width / 2) - (menuFont.MeasureString(str).X / 2),
                             (GraphicsDevice.Viewport.Height / 2) + (menuFont.MeasureString(str).Y * 3f)
                             );
            DrawStringStroke(str, pos, Color.White, spriteBatch);
            spriteBatch.DrawString(menuFont, str, pos, Color.Black);

            str = "About";
            pos = new Vector2(
                             (GraphicsDevice.Viewport.Width / 2) - (menuFont.MeasureString(str).X / 2),
                             (GraphicsDevice.Viewport.Height / 2) + (menuFont.MeasureString(str).Y * 4.5f)
                             );
            DrawStringStroke(str, pos, Color.White, spriteBatch);
            spriteBatch.DrawString(menuFont, str, pos, Color.Black);

            str = "Quit";
            pos = new Vector2(
                             (GraphicsDevice.Viewport.Width / 2) - (menuFont.MeasureString(str).X / 2),
                             (GraphicsDevice.Viewport.Height / 2) + (menuFont.MeasureString(str).Y * 6f)
                             );
            DrawStringStroke(str, pos, Color.White, spriteBatch);
            spriteBatch.DrawString(menuFont, str, pos, Color.Black);

            spriteBatch.End();
        }

        private void DrawInstructions(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            spriteBatch.Draw(backgroundTexture, Vector2.Zero, Color.White);

            switch (instructionsPage)
            {
                case 1:
                    spriteBatch.Draw(instructionsTexture1, Vector2.Zero, Color.White);
                    break;
                case 2:
                    spriteBatch.Draw(instructionsTexture2, Vector2.Zero, Color.White);
                    break;
            }

            tapScreen.Draw(spriteBatch);

            spriteBatch.End();
        }

        private void DrawGameSettings(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            spriteBatch.Draw(backgroundTexture, Vector2.Zero, Color.White);
            spriteBatch.Draw(logoTexture, new Vector2(58, 119), Color.White);

            //Rectangle pixelRectangle = new Rectangle(0, 464, 480, 60);

            //for (int i = 0; i < 4; i++)
            //{
            //    spriteBatch.Draw(pixelTexture, pixelRectangle, Color.White);
            //    pixelRectangle.Y += 70;
            //}

            string str;
            Vector2 pos;

            str = "Play";
            pos = new Vector2(
                             (GraphicsDevice.Viewport.Width / 2) - (menuFont.MeasureString(str).X / 2),
                             (GraphicsDevice.Viewport.Height / 2) + (menuFont.MeasureString(str).Y * 1.5f)

                             );
            DrawStringStroke(str, pos, Color.White, spriteBatch);
            spriteBatch.DrawString(menuFont, str, pos, Color.Black);

            switch (difficulty)
            {
                case 0:
                    str = "Difficulty: Easy";
                    break;
                case 1:
                    str = "Difficulty: Normal";
                    break;
                case 2:
                    str = "Difficulty: Hard";
                    break;
            }

            pos = new Vector2(
                             (GraphicsDevice.Viewport.Width / 2) - (menuFont.MeasureString(str).X / 2),
                             (GraphicsDevice.Viewport.Height / 2) + (menuFont.MeasureString(str).Y * 3f)
                             );
            DrawStringStroke(str, pos, Color.White, spriteBatch);
            spriteBatch.DrawString(menuFont, str, pos, Color.Black);

            str = (soundEnabled == true) ? "Sound: On" : "Sound: Off";
            pos = new Vector2(
                             (GraphicsDevice.Viewport.Width / 2) - (menuFont.MeasureString(str).X / 2),
                             (GraphicsDevice.Viewport.Height / 2) + (menuFont.MeasureString(str).Y * 4.5f)
                             );
            DrawStringStroke(str, pos, Color.White, spriteBatch);
            spriteBatch.DrawString(menuFont, str, pos, Color.Black);

            str = "Return to Menu";
            pos = new Vector2(
                             (GraphicsDevice.Viewport.Width / 2) - (menuFont.MeasureString(str).X / 2),
                             (GraphicsDevice.Viewport.Height / 2) + (menuFont.MeasureString(str).Y * 6f)
                             );
            DrawStringStroke(str, pos, Color.White, spriteBatch);
            spriteBatch.DrawString(menuFont, str, pos, Color.Black);

            spriteBatch.End();
        }

        private void DrawAbout(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            spriteBatch.Draw(backgroundTexture, Vector2.Zero, Color.White);
            spriteBatch.Draw(logoTexture, new Vector2(58, 119), Color.White);

            //Rectangle pixelRectangle = new Rectangle(0, 464, 480, 60);

            //for (int i = 0; i < 4; i++)
            //{
            //    spriteBatch.Draw(pixelTexture, pixelRectangle, Color.White);
            //    pixelRectangle.Y += 70;
            //}

            string str;
            Vector2 pos;

            str = string.Format("{0} v{1}", GameSettings.GameName, GameSettings.GameVersion);
            pos = new Vector2(
                                     (GraphicsDevice.Viewport.Width / 2) - (spriteFont.MeasureString(str).X / 2),
                                     (GraphicsDevice.Viewport.Height / 2) - (spriteFont.MeasureString(str).Y * 6f)
                                     );
            spriteBatch.DrawString(spriteFont, str, pos, Color.White);

            str = "By Marcel de Vries";
            pos = new Vector2(
                                     (GraphicsDevice.Viewport.Width / 2) - (spriteFont.MeasureString(str).X / 2),
                                     (GraphicsDevice.Viewport.Height / 2) - (spriteFont.MeasureString(str).Y * 5f)
                                     );
            spriteBatch.DrawString(spriteFont, str, pos, Color.White);

            str = "Review game";
            pos = new Vector2(
                                     (GraphicsDevice.Viewport.Width / 2) - (menuFont.MeasureString(str).X / 2),
                                     (GraphicsDevice.Viewport.Height / 2) + (menuFont.MeasureString(str).Y * 1.5f)
                                     );
            DrawStringStroke(str, pos, Color.White, spriteBatch);
            spriteBatch.DrawString(menuFont, str, pos, Color.Black);

            str = "Send feedback";
            pos = new Vector2(
                                     (GraphicsDevice.Viewport.Width / 2) - (menuFont.MeasureString(str).X / 2),
                                     (GraphicsDevice.Viewport.Height / 2) + (menuFont.MeasureString(str).Y * 3f)
                                     );
            DrawStringStroke(str, pos, Color.White, spriteBatch);
            spriteBatch.DrawString(menuFont, str, pos, Color.Black);


            str = "Find other games";
            pos = new Vector2(
                                     (GraphicsDevice.Viewport.Width / 2) - (menuFont.MeasureString(str).X / 2),
                                     (GraphicsDevice.Viewport.Height / 2) + (menuFont.MeasureString(str).Y * 4.5f)
                                     );
            DrawStringStroke(str, pos, Color.White, spriteBatch);
            spriteBatch.DrawString(menuFont, str, pos, Color.Black);

            str = "Return to menu";
            pos = new Vector2(
                                     (GraphicsDevice.Viewport.Width / 2) - (menuFont.MeasureString(str).X / 2),
                                     (GraphicsDevice.Viewport.Height / 2) + (menuFont.MeasureString(str).Y * 6f)
                                     );
            DrawStringStroke(str, pos, Color.White, spriteBatch);
            spriteBatch.DrawString(menuFont, str, pos, Color.Black);

            spriteBatch.End();
        }

        private void DrawLoading(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            spriteBatch.Draw(backgroundTexture, Vector2.Zero, Color.White);
            spriteBatch.Draw(logoTexture, new Vector2(58, 119), Color.White);

            string loading = "Loading...";
            Vector2 position = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            position = position - (menuFont.MeasureString(loading) / 2);

            spriteBatch.DrawString(menuFont, loading, new Vector2(position.X + 2, position.Y), Color.White);
            spriteBatch.DrawString(menuFont, loading, new Vector2(position.X - 2, position.Y), Color.White);
            spriteBatch.DrawString(menuFont, loading, new Vector2(position.X, position.Y + 2), Color.White);
            spriteBatch.DrawString(menuFont, loading, new Vector2(position.X, position.Y - 2), Color.White);

            spriteBatch.DrawString(menuFont, loading, position, Color.Black);

            spriteBatch.End();
        }

        private void DrawPlaying(SpriteBatch spriteBatch)
        {
#if DEBUG
            frameCounter++;
#endif

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, camera.get_transformation(graphics.GraphicsDevice));

            spriteBatch.Draw(mazeTexture, new Rectangle(mazeRectangle.X - 24, mazeRectangle.Y, mazeRectangle.Width, mazeRectangle.Height), Color.White);

            DrawMazeItems(spriteBatch);

            player.Draw(spriteBatch);

            DrawEnemies(spriteBatch);

            spriteBatch.Draw(overlayTexture, overlayRectangle, Color.White);

            spriteBatch.Draw(energyBarTexture, energyRectangle, Color.White);
#if DEBUG
            DrawDebugInfo();
#endif
            spriteBatch.End();
        }

        private void DrawPaused(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            spriteBatch.Draw(backgroundTexture, Vector2.Zero, Color.White);
            spriteBatch.Draw(logoTexture, new Vector2(58, 119), Color.White);

            //Rectangle pixelRectangle = new Rectangle(0, 464, 480, 60);

            //for (int i = 0; i < 4; i++)
            //{
            //    spriteBatch.Draw(pixelTexture, pixelRectangle, Color.White);
            //    pixelRectangle.Y += 70;
            //}

            Vector2[] positions = new Vector2[4];
            string[] pausedStrings = new string[4];

            pausedStrings[0] = "Continue";
            pausedStrings[1] = "Restart";

            if (soundEnabled == true)
            {
                pausedStrings[2] = string.Format("Sound: {0}", "On");
            }
            else
            {
                pausedStrings[2] = string.Format("Sound: {0}", "Off");
            }

            pausedStrings[3] = "Return to Menu";

            for (int i = 0; i < pausedStrings.Length; i++)
            {
                positions[i] = new Vector2(
                    (GraphicsDevice.Viewport.Width / 2) - (menuFont.MeasureString(pausedStrings[i]).X / 2),
                    (GraphicsDevice.Viewport.Height / 2) + (menuFont.MeasureString(pausedStrings[i]).Y * ((i + 1) * 1.5f))
                    );

                spriteBatch.DrawString(menuFont, pausedStrings[i], new Vector2(positions[i].X + 2, positions[i].Y), Color.White);
                spriteBatch.DrawString(menuFont, pausedStrings[i], new Vector2(positions[i].X - 2, positions[i].Y), Color.White);
                spriteBatch.DrawString(menuFont, pausedStrings[i], new Vector2(positions[i].X, positions[i].Y + 2), Color.White);
                spriteBatch.DrawString(menuFont, pausedStrings[i], new Vector2(positions[i].X, positions[i].Y - 2), Color.White);

                spriteBatch.DrawString(menuFont, pausedStrings[i], positions[i], Color.Black);
            }

            spriteBatch.End();
        }

        private void DrawFighting(SpriteBatch spriteBatch)
        {
#if DEBUG
            frameCounter++;
#endif
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, camera.get_transformation(graphics.GraphicsDevice));

            spriteBatch.Draw(mazeTexture, new Rectangle(mazeRectangle.X - 24, mazeRectangle.Y, mazeRectangle.Width, mazeRectangle.Height), Color.White);

            DrawMazeItems(spriteBatch);

            if (fightAnimation.Active == true)
            {
                fightAnimation.Draw(spriteBatch);
            }
            else
            {
                winAnimation.Draw(spriteBatch);
                loseAnimation.Draw(spriteBatch);
            }

            DrawEnemies(spriteBatch);

            spriteBatch.Draw(overlayTexture, overlayRectangle, Color.White);

            spriteBatch.Draw(energyBarTexture, energyRectangle, Color.White);
#if DEBUG
            DrawDebugInfo();
#endif
            spriteBatch.End();
        }

        private void DrawGameOver(SpriteBatch spriteBatch)
        {
            string str;
            Vector2 position;

            spriteBatch.Begin();

            spriteBatch.Draw(backgroundTexture, Vector2.Zero, Color.White);
            spriteBatch.Draw(logoTexture, new Vector2(58, 119), Color.White);

            str = "GAME OVER";
            position = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            position = position - (menuFont.MeasureString(str) / 2);

            spriteBatch.DrawString(menuFont, str, new Vector2(position.X + 2, position.Y), Color.White);
            spriteBatch.DrawString(menuFont, str, new Vector2(position.X - 2, position.Y), Color.White);
            spriteBatch.DrawString(menuFont, str, new Vector2(position.X, position.Y + 2), Color.White);
            spriteBatch.DrawString(menuFont, str, new Vector2(position.X, position.Y - 2), Color.White);

            spriteBatch.DrawString(menuFont, str, position, Color.Black);

            if (player.Energy <= 0)
            {
                str = "You starved to death!";
            }
            else
            {
                str = "A maziac killed you!";
            }

            position = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            position.X = position.X - (spriteFont.MeasureString(str).X / 2);
            position.Y = position.Y + (spriteFont.MeasureString(str).Y * 3);
            spriteBatch.DrawString(spriteFont, str, position, Color.White);

            str = "Tap the screen to continue";
            position = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height);
            position.X = position.X - (spriteFont.MeasureString(str).X / 2);
            position.Y = position.Y - (spriteFont.MeasureString(str).Y * 4);

            tapScreen.Draw(spriteBatch);

            spriteBatch.End();
        }

        private void DrawGameWon(SpriteBatch spriteBatch)
        {
            string str;
            Vector2 position;

            spriteBatch.Begin();

            spriteBatch.Draw(backgroundTexture, Vector2.Zero, Color.White);
            spriteBatch.Draw(logoTexture, new Vector2(58, 119), Color.White);

            str = "Well done!";
            position = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            position = position - (menuFont.MeasureString(str) / 2);

            spriteBatch.DrawString(menuFont, str, new Vector2(position.X + 2, position.Y), Color.White);
            spriteBatch.DrawString(menuFont, str, new Vector2(position.X - 2, position.Y), Color.White);
            spriteBatch.DrawString(menuFont, str, new Vector2(position.X, position.Y + 2), Color.White);
            spriteBatch.DrawString(menuFont, str, new Vector2(position.X, position.Y - 2), Color.White);

            spriteBatch.DrawString(menuFont, str, position, Color.Black);

            str = string.Format("Minimum moves needed: {0}", minimumMoves);
            position = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            position.X = position.X - (spriteFont.MeasureString(str).X / 2);
            position.Y = position.Y + (spriteFont.MeasureString(str).Y * 3);
            spriteBatch.DrawString(spriteFont, str, position, Color.White);

            str = string.Format("Actual moves you did: {0}", actualMoves);
            position = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            position.X = position.X - (spriteFont.MeasureString(str).X / 2);
            position.Y = position.Y + (spriteFont.MeasureString(str).Y * 4);
            spriteBatch.DrawString(spriteFont, str, position, Color.White);

            str = string.Format("Final score: {0}", finalScore);
            position = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            position.X = position.X - (spriteFont.MeasureString(str).X / 2);
            position.Y = position.Y + (spriteFont.MeasureString(str).Y * 6);
            spriteBatch.DrawString(spriteFont, str, position, Color.White);

            if (finalScore < highscore)
            {
                str = string.Format("Highscore: {0}", highscore);
            }
            else
            {
                str = string.Format("New highscore: {0}", highscore);
            }

            position = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            position.X = position.X - (spriteFont.MeasureString(str).X / 2);
            position.Y = position.Y + (spriteFont.MeasureString(str).Y * 7);
            spriteBatch.DrawString(spriteFont, str, position, Color.White);

            tapScreen.Draw(spriteBatch);

            spriteBatch.End();
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            switch (gameState)
            {
                case GameState.Menu:
                    {
                        DrawMenu(spriteBatch);
                    }
                    break;

                case GameState.Instructions:
                    {
                        DrawInstructions(spriteBatch);
                    }
                    break;

                case GameState.GameSettings:
                    {
                        DrawGameSettings(spriteBatch);
                    }
                    break;

                case GameState.About:
                    {
                        DrawAbout(spriteBatch);
                    }
                    break;

                case GameState.Resume:
                case GameState.Loading:
                    {
                        DrawLoading(spriteBatch);
                    }
                    break;

                case GameState.Playing:
                    {
                        DrawPlaying(spriteBatch);
                    }
                    break;

                case GameState.Paused:
                    {
                        DrawPaused(spriteBatch);
                    }
                    break;

                case GameState.Fighting:
                    {
                        DrawFighting(spriteBatch);
                    }
                    break;

                case GameState.GameOver:
                    {
                        DrawGameOver(spriteBatch);
                    }
                    break;

                case GameState.GameWon:
                    {
                        DrawGameWon(spriteBatch);
                    }
                    break;

                default:
                    break;
            }

            base.Draw(gameTime);            
        }

        private void ExitGame()
        {
            this.Exit();
        }

        private void RetryAdCreation(GameTime gameTime)
        {
            if (retryAdCreation == true)
            {
                if (adTimeout >= 90000)
                {
                    bannerAd = adGameComponent.CreateAd("81067", new Rectangle(0, 0, 480, 80), true);

                    bannerAd.ErrorOccurred += new EventHandler<Microsoft.Advertising.AdErrorEventArgs>(bannerAd_ErrorOccurred);
                    bannerAd.AdRefreshed += new EventHandler(bannerAd_AdRefreshed);

                    adTimeout = 0;
                }
                adTimeout += (int)gameTime.ElapsedGameTime.TotalMilliseconds;
            }
            else
            {
                return;
            }
        }

        // EVENTS

        void bannerAd_AdRefreshed(object sender, EventArgs e)
        {
            retryAdCreation = false;
        }

        void bannerAd_ErrorOccurred(object sender, Microsoft.Advertising.AdErrorEventArgs e)
        {
            retryAdCreation = true;
#if DEBUG
            Debug.WriteLine("Ad error: {0}", e.Error.Message);
#endif
        }
    }
}