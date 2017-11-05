using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using CollisionManager;
using SpriteAnimation;
using Box2D.XNA;

namespace OmegaRace
{
    public enum gameState
    {
        lobby,
        ready, // Flashes Ready? until the timer is up
        game, // The main game mode
        pause,
        winner // Displays the winner
    };

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        string errorMessage;
        SpriteFont font;
        ScreenManager screenManager;
        PacketReader Reader = new PacketReader();
        PacketWriter packetWriter = new PacketWriter();
        KeyboardState currentKeyboardState;
        GamePadState currentGamePadState;
        OutputQueue outputQueue = new OutputQueue();
        InputQueue inputQueue = new InputQueue();

        const int screenWidth = 1067;
        const int screenHeight = 600;

        const int maxGamers = 16;
        const int maxLocalGamers = 4;

        public GraphicsDeviceManager Graphics
        {
            get { return graphics; }
        }


        private static Game1 Game;
        public static Game1 GameInstance
        {
            get { return Game; }
        }

        private static Camera camera;
        public static Camera Camera
        {
            get { return camera; }
        }


        // Keyboard and Xbox Controller states
        KeyboardState oldState;
        KeyboardState newState;

        GamePadState P1oldPadState;
        GamePadState P1newPadState;

        GamePadState P2oldPadState;
        GamePadState P2newPadState;


        // For flipping game states
        public static gameState state;
        bool Host = false;

        // Box2D world
        World world;
        public World getWorld()
        {
            return world;
        }

        public Rectangle gameScreenSize;

        NetworkSession networkSession;


        // Quick reference for Input 
        Player player1;
        Player player2;
        Texture2D backgroundTexture;

        // Max ship speed
        int shipSpeed;


        public Game1()
        {
            Content.RootDirectory = "Content";
            Components.Add(new GamerServicesComponent(this));
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = 500;
            graphics.PreferredBackBufferWidth = 800;
            gameScreenSize = new Rectangle(0, 0, 800, 500);
            screenManager = new ScreenManager(this);
            shipSpeed = 200;
            Game = this;
            state = gameState.lobby;
            world = new World(new Vector2(0, 0), false);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            
            camera = new Camera(GraphicsDevice.Viewport, Vector2.Zero);
            //state = gameState.game;
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // TODO: use this.Content to load your game content here
            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("SpriteFont1");

            world = new World(new Vector2(0, 0), true);
            myContactListener myContactListener = new myContactListener();
             world.ContactListener = myContactListener;
            Data.Instance().createData();
            backgroundTexture = Content.Load<Texture2D>("background");
            //backgroundTexture = Content.Load<Texture2D>("MainmenuLobby");
            player1 = PlayerManager.getPlayer(PlayerID.one);
            player2 = PlayerManager.getPlayer(PlayerID.two);

        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            HandleInput();
            if ((networkSession == null && gameState.ready == state))
            {
                // If we are not in a network session, update the
                // menu screen that will let us create or join one.
                UpdateMenuScreen();
            }
            else if(state==gameState.game && networkSession!=null)
            {
                // If we are in a network session, update it.
                UpdateNetworkSession();

                // TODO: Add your update logic here
                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                    this.Exit();

                GraphicsDevice.Clear(Color.Black);
                base.Update(gameTime);

                if (state == gameState.game)
                {
                    
                    world.Step((float)gameTime.ElapsedGameTime.TotalSeconds, 5, 8);
                    checkInput();
                    PhysicsMan.Instance().Update();
                    ScoreManager.Instance().Update();
                    GameObjManager.Instance().Update(world);
                    Timer.Process(gameTime);
                }
                Game1.Camera.Update(gameTime);
            }
            base.Update(gameTime);
        }


        /// <summary>
        /// Menu screen provides options to create or join network sessions.
        /// </summary>
        void UpdateMenuScreen()
        {
            if (IsActive)
            {
                if (Gamer.SignedInGamers.Count == 0)
                {
                    // If there are no profiles signed in, we cannot proceed.
                    // Show the Guide so the user can sign in.
                    Guide.ShowSignIn(maxLocalGamers, false);
                }
                else if (IsPressed(Keys.A, Buttons.A))
                {
                    // Create a new session?
                    CreateSession();
                    Host = true;
                }
                else if (IsPressed(Keys.B, Buttons.B))
                {
                    // Join an existing session?
                    JoinSession();
                    Host = false;
                }
            }
        }
        
        

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            if (!IsActive)
                return;

            if (state == gameState.lobby)
            {
                DrawLobby();
            }
            else if (state == gameState.winner)
            {
                DrawEndScreen();
            }
            // If we are in a network session, draw it.
            
            else if (networkSession == null && state==gameState.ready)
            {
                DrawMenuScreen();
            }

            else if (state == gameState.game && networkSession != null)
            {
                SpriteBatchManager.Instance().process();
                DrawNetworkSession();
            }
            base.Draw(gameTime);
        }

        /// <summary>
        /// Helper draws notification messages before calling blocking network methods.
        /// </summary>
        void DrawMessage(string message)
        {
            if (!BeginDraw())
                return;

            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            spriteBatch.DrawString(font, message, new Vector2(161, 161), Color.Black);
            spriteBatch.DrawString(font, message, new Vector2(160, 160), Color.White);

            spriteBatch.End();

            EndDraw();
        }

        /// <summary>
        /// Draws the startup screen used to create and join network sessions.
        /// </summary>
        void DrawMenuScreen()
        {
            string message = string.Empty;

            if (!string.IsNullOrEmpty(errorMessage)) 
            {
                message += "Error:\n" + errorMessage.Replace(". ", ".\n") + "\n\n";
                
            }
                

            message += "A- Create game\n" +
                       "B- Join game";

            spriteBatch.Begin();

            spriteBatch.DrawString(font, message, new Vector2(161, 161), Color.Red);
            spriteBatch.DrawString(font, message, new Vector2(160, 160), Color.Red);

            spriteBatch.End();
        }
        
        
                /// <summary>
        /// Draws the startup screen used to create and join network sessions.
        /// </summary>
        void DrawLobby()
        {
            string message = string.Empty;
            string msg1 = string.Empty;

            

            message += "Press 'ENTER' to start\n"+
                       "Press 'ESC' to quit";
            msg1 += " WELCOME TO 'OMEGA RACE' MULTIPLAYER GAME";

            spriteBatch.Begin();
            spriteBatch.DrawString(font, msg1, new Vector2(130, 130), Color.Aqua);
            spriteBatch.DrawString(font, message, new Vector2(200, 200), Color.Aqua);
            spriteBatch.DrawString(font, message, new Vector2(200, 200), Color.Red);

            spriteBatch.End(); 

            if (IsPressed(Keys.Enter, Buttons.B))
            {
                DrawMenuScreen();
                state = gameState.ready;
            }
            else if (IsPressed(Keys.Escape, Buttons.Back))
            {
                Exit();
                state = gameState.ready;
            }
        }
        /// <summary>
        /// Draws the state of an active network session.
        /// </summary>
        void DrawNetworkSession()
        {
            spriteBatch.Begin();

            // For each person in the session...
            foreach (NetworkGamer gamer in networkSession.AllGamers)
            {
                // Draw a gamertag label.
                string label = gamer.Gamertag;
                Color labelColor = Color.Black;
                Vector2 labelOffset = new Vector2(100, 150);

                if (gamer.IsHost)
                    label += " (host)";

                // Flash the gamertag to yellow when the player is talking.
                if (gamer.IsTalking)
                    labelColor = Color.Yellow;

            }

            spriteBatch.End();
        }

        /// <summary>
        /// Starts hosting a new network session.
        /// </summary>
        void CreateSession()
        {
            DrawMessage("Creating game...");

            try
            {
                networkSession = NetworkSession.Create(NetworkSessionType.SystemLink,
                                                       maxLocalGamers, maxGamers);

                HookSessionEvents();
                state = gameState.game;
                errorMessage = null;
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
            }
        }


        /// <summary>
        /// Joins an existing network session.
        /// </summary>
        void JoinSession()
        {
            DrawMessage("Joining game...");

            try
            {
                // Search for sessions.
                using (AvailableNetworkSessionCollection availableSessions =
                            NetworkSession.Find(NetworkSessionType.SystemLink,
                                                maxLocalGamers, null))
                {
                    if (availableSessions.Count == 0)
                    {
                        errorMessage = "Server not found";
                        return;
                    }

                    // Join the first session we found.
                    networkSession = NetworkSession.Join(availableSessions[0]);
                    state = gameState.game;
                    HookSessionEvents();
                    errorMessage = null;
                }
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
            }
        }


        /// <summary>
        /// After creating or joining a network session, we must subscribe to
        /// some events so we will be notified when the session changes state.
        /// </summary>
        void HookSessionEvents()
        {
            networkSession.GamerJoined += GamerJoinedEventHandler;
            networkSession.SessionEnded += SessionEndedEventHandler;
        }


        /// <summary>
        /// This event handler will be called whenever a new gamer joins the session.
        /// We use it to allocate a Tank object, and associate it with the new gamer.
        /// </summary>
        void GamerJoinedEventHandler(object sender, GamerJoinedEventArgs e)
        {
            int gamerIndex = networkSession.AllGamers.IndexOf(e.Gamer);

            if (Host == true && gamerIndex == 0)
                e.Gamer.Tag = PlayerManager.getPlayer(PlayerID.one);
            else if (Host == true && gamerIndex == 1)
                e.Gamer.Tag = PlayerManager.getPlayer(PlayerID.two);
            else if (Host == false && gamerIndex == 0)
                e.Gamer.Tag = PlayerManager.getPlayer(PlayerID.two);
            else if (Host == false && gamerIndex == 1)
                e.Gamer.Tag = PlayerManager.getPlayer(PlayerID.one);

        }


        /// <summary>
        /// Event handler notifies us when the network session has ended.
        /// </summary>
        void SessionEndedEventHandler(object sender, NetworkSessionEndedEventArgs e)
        {
            errorMessage = e.EndReason.ToString();

            networkSession.Dispose();
            networkSession = null;
        }




        /// <summary>
        /// Updates the state of the network session, moving the tanks
        /// around and synchronizing their state over the network.
        /// </summary>
        void UpdateNetworkSession()
        {
            LocalNetworkGamer localGamer = null;

            // Update our locally controlled Birds, and send their
            // latest position data to everyone in the session.
            foreach (LocalNetworkGamer gamer in networkSession.LocalGamers)
            {
                localGamer = gamer;
            }

            outputQueue.pushToNetwork(localGamer);

            // Pump the underlying session object.
            networkSession.Update();

            // Make sure the session has not ended.
            if (networkSession == null)
                return;

            // Read any incoming network packets.
            foreach (LocalNetworkGamer gamer in networkSession.LocalGamers)
            {
                if (gamer.IsHost)
                {
                    ReadInputFromClients(gamer);
                }
                else
                {
                    ReadFromServer(gamer);
                }
            }

            // Process the input Queue
            inputQueue.process();
        }

        void ReadInputFromClients(LocalNetworkGamer gamer)
        {
            while (gamer.IsDataAvailable)
            {
                NetworkGamer sender;

                // Read a single packet from the network.
                gamer.ReceiveData(Reader, out sender);

                if (sender.IsLocal)
                    continue;

                qHeader qH;
                Ship Player2Ship;
                Player player;

                qH.type = (QueueType)Reader.ReadInt32();
                qH.packetOwner = (PlayerID)Reader.ReadInt32();
                qH.inseq = Reader.ReadInt32();
                qH.outseq = Reader.ReadInt32();
                qH.obj = null;

                if (qH.packetOwner == PlayerID.one) 
                {
                    Player2Ship = player1.playerShip;
                    player = player1;
                }
                else 
                {
                    Player2Ship = player2.playerShip;
                    player = player2;
                }
                

                switch (qH.type)
                {
                    case QueueType.ship_impulse:
                        float x = Reader.ReadInt32();
                        float y = Reader.ReadInt32();

                        float m = Reader.ReadInt32();
                        float n = Reader.ReadInt32();
                        float r = Reader.ReadInt32();
                        Vector2 direction = new Vector2((float)(Math.Cos(Player2Ship.physicsObj.body.GetAngle())), (float)(Math.Sin(Player2Ship.physicsObj.body.GetAngle())));
                        direction.Normalize();
                        direction *= shipSpeed;
                        ship_impulse p = new ship_impulse(player, direction);

                        //update server values
                        p.rot = player.playerShip.rotation;
                        p.x = player.playerShip.location.X;
                        p.y = player.playerShip.location.Y;

                        p.impulse.X = x;
                        p.impulse.Y = y;
                        qH.obj = p;
                        break;

                    case QueueType.ship_rot_anti:
                        float x1 = (int)Reader.ReadInt32();
                        Ship_rot_message pShip = new Ship_rot_message(player, x1);
                        pShip.rot = x1;
                        
                        //server values
                        pShip.x = player.playerShip.location.X;
                        pShip.y = player.playerShip.location.Y;
                        pShip.serverRotvalue = player.playerShip.rotation;
                        
                        qH.obj = pShip;
                        break;

                    case QueueType.ship_rot_clock:
                        float x2 = (int)Reader.ReadInt32();
                        Ship_rot_message pShip1 = new Ship_rot_message(player, x2);
                        pShip1.rot = x2;

                        //server values
                        pShip1.x = player.playerShip.location.X;
                        pShip1.y = player.playerShip.location.Y;
                        pShip1.serverRotvalue = player.playerShip.rotation;
                        
                        qH.obj = pShip1;
                        break;

                    case QueueType.ship_bomb:
                        Ship_Create_Bomb_Message p2 = new Ship_Create_Bomb_Message(player);
                        //server values
                        p2.x = player.playerShip.location.X;
                        p2.y = player.playerShip.location.Y;
                        p2.rot = player.playerShip.rotation;
                        qH.obj = p2;
                        break;

                    case QueueType.ship_missile:
                        Ship_Create_Missile_Message p3 = new Ship_Create_Missile_Message(player);
                        //server values
                        p3.x = player.playerShip.location.X;
                        p3.y = player.playerShip.location.Y;
                        p3.rot = player.playerShip.rotation;
                        qH.obj = p3;
                        break;

                    case QueueType.physicsBuffer:
                        //physics_buffer_message p4 = new physics_buffer_message();
                        //qH.ob = p3;
                        break;
                    case QueueType.EventMessage:
                        int a =(int) Reader.ReadInt32();
                        int b = (int)Reader.ReadInt32();
                        Vector2 pt = (Vector2)Reader.ReadVector2();
                        EvenMessage e = new EvenMessage(a,b,pt);
                        qH.obj = e;
                        break;
                }
                inQueue.add(qH.obj, qH.type, qH.outseq, qH.packetOwner);
                OutQueue.add(qH.type, qH.obj,qH.packetOwner);
            }
        }


        void ReadFromServer(LocalNetworkGamer gamer)
        {
            // Keep reading as long as incoming packets are available.
            while (gamer.IsDataAvailable)
            {
                NetworkGamer sender;
                gamer.ReceiveData(Reader, out sender);
                Player p = gamer.Tag as Player;

                // This packet contains data about all the players in the session.
                // We keep reading from it until we have processed all the data.
                while (Reader.Position < Reader.Length)
                {
                    qHeader qH;
                    Player player ;
                    Ship Player2Ship ;

                    qH.type = (QueueType)Reader.ReadInt32();
                    qH.packetOwner = (PlayerID)Reader.ReadInt32();
                    qH.inseq = Reader.ReadInt32();
                    qH.outseq = Reader.ReadInt32();
                    qH.obj = null;

                    if (qH.packetOwner == PlayerID.one)
                    {
                        Player2Ship = player1.playerShip;
                        player = player1;
                    }
                    else
                    {
                        Player2Ship = player2.playerShip;
                        player = player2;
                    }

                    switch (qH.type)
                    {
                        case QueueType.ship_impulse:
                            float x = Reader.ReadInt32();
                            float y = Reader.ReadInt32();

                            player.playerShip.location.X = Reader.ReadInt32();
                            player.playerShip.location.Y = Reader.ReadInt32();
                            player.playerShip.rotation = Reader.ReadInt32();
                            
                            Vector2 direction = new Vector2((float)(Math.Cos(Player2Ship.physicsObj.body.GetAngle())), (float)(Math.Sin(Player2Ship.physicsObj.body.GetAngle())));
                            direction.Normalize();
                            direction *= shipSpeed;
                            ship_impulse p1 = new ship_impulse(player, direction);
                            p1.impulse.X = x;
                            p1.impulse.Y = y;
                            qH.obj = p1;
                            break;

                        case QueueType.ship_rot_anti:
                            float x1 = (int)Reader.ReadInt32();
                            player.playerShip.location.X = Reader.ReadInt32();
                            player.playerShip.location.Y = Reader.ReadInt32();
                            player.playerShip.rotation = Reader.ReadInt32();
                            Ship_rot_message pShip = new Ship_rot_message(player, x1);
                            pShip.rot = x1;
                            qH.obj = pShip;
                            break;

                        case QueueType.ship_rot_clock:
                            float x2 = (int)Reader.ReadInt32();
                            player.playerShip.location.X = Reader.ReadInt32();
                            player.playerShip.location.Y = Reader.ReadInt32();
                            player.playerShip.rotation = Reader.ReadInt32();
                            Ship_rot_message pShip1 = new Ship_rot_message(player, x2);
                            pShip1.rot = x2;
                            qH.obj = pShip1;
                            break;

                        case QueueType.ship_bomb:
                            player.playerShip.location.X = Reader.ReadInt32();
                            player.playerShip.location.Y = Reader.ReadInt32();
                            player.playerShip.rotation = Reader.ReadInt32();
                            Ship_Create_Bomb_Message p2 = new Ship_Create_Bomb_Message(player);
                            qH.obj = p2;
                            break;

                        case QueueType.ship_missile:
                            player.playerShip.location.X = Reader.ReadInt32();
                            player.playerShip.location.Y = Reader.ReadInt32();
                            player.playerShip.rotation = Reader.ReadInt32();
                            Ship_Create_Missile_Message p3 = new Ship_Create_Missile_Message(player);
                            qH.obj = p3;
                            break;
                        case QueueType.EventMessage:
                            int a = (int)Reader.ReadInt32();
                            int b = (int)Reader.ReadInt32();
                            Vector2 pt = (Vector2)Reader.ReadVector2();
                            EvenMessage e = new EvenMessage(a, b, pt);
                            qH.obj = e;
                            break;
                    }
                    inQueue.add(qH.obj, qH.type, qH.outseq, qH.packetOwner);
                }
            }
        }

        // Helper for updating a locally controlled gamer.
        void UpdateLocalGamer(LocalNetworkGamer gamer)
        {
            checkInput();
        }



        /// <summary>
        /// Handles input.
        /// </summary>
        private void HandleInput()
        {
            currentKeyboardState = Keyboard.GetState();
            currentGamePadState = GamePad.GetState(PlayerIndex.One);

            // Check for exit.
            if (IsActive && IsPressed(Keys.Escape, Buttons.Back))
            {
                Exit();
            }
        }


        /// <summary>
        /// Checks if the specified button is pressed on either keyboard or gamepad.
        /// </summary>
        bool IsPressed(Keys key, Buttons button)
        {
            return (currentKeyboardState.IsKeyDown(key) ||
                    currentGamePadState.IsButtonDown(button));
        }

        /// <summary>
        /// Draws the startup screen used to create and join network sessions.
        /// </summary>
        void DrawEndScreen()
        {
            string escMsg = string.Empty;
            
            newState = Keyboard.GetState();
            spriteBatch.Begin();

            if (!string.IsNullOrEmpty(errorMessage))
                escMsg += "Error:\n" + errorMessage.Replace(". ", ".\n") + "\n\n";

            escMsg = "Game Over";
            spriteBatch.DrawString(font, escMsg, new Vector2(351, 100), Color.Red);
            spriteBatch.DrawString(font, escMsg, new Vector2(350, 101), Color.Red);
            
            escMsg = "Player 1  Score";
            escMsg += "\n" + ScoreManager.Instance().getTotalInfo(PlayerID.one);
            spriteBatch.DrawString(font, escMsg, new Vector2(101, 161), Color.Blue);
            spriteBatch.DrawString(font, escMsg, new Vector2(100, 160), Color.Blue);

            escMsg = "Player 2  Score";
            escMsg += "\n" + ScoreManager.Instance().getTotalInfo(PlayerID.two);
            spriteBatch.DrawString(font, escMsg, new Vector2(501, 161), Color.Green);
            spriteBatch.DrawString(font, escMsg, new Vector2(500, 160), Color.Green);

            escMsg = "Press [M] for menu\n\n";
            spriteBatch.DrawString(font, escMsg, new Vector2(101, 350), Color.Aqua);
            spriteBatch.DrawString(font, escMsg, new Vector2(100, 351), Color.Aqua);

            spriteBatch.End();


            if (newState.IsKeyDown(Keys.M))
            {
                ScoreManager.Instance().clear();
                resetData();
            }
        }


        public void GameOver()
        {
            state = gameState.winner;
            DrawEndScreen();
        }


        private void checkInput()
        {
            newState = Keyboard.GetState();
            P1newPadState = GamePad.GetState(PlayerIndex.One);
            P2newPadState = GamePad.GetState(PlayerIndex.Two);
            Player p;

            if (Host) 
            {
                p = PlayerManager.getPlayer(PlayerID.one);
            }
            else
            {
                p = PlayerManager.getPlayer(PlayerID.two);
            }

            ////Player 1 controls
            if (oldState.IsKeyDown(Keys.D) || P1oldPadState.IsButtonDown(Buttons.DPadRight))
            {
                Ship_rot_message pShip = new Ship_rot_message(p, 0.1f);
                pShip.serverRotvalue = p.playerShip.rotation;
                pShip.x= p.playerShip.location.X;
                pShip.y = p.playerShip.location.Y;
                OutQueue.add(QueueType.ship_rot_clock, pShip, p.id);
            }

            if (oldState.IsKeyDown(Keys.A) || P1oldPadState.IsButtonDown(Buttons.DPadLeft))
            {
                Ship_rot_message pShip = new Ship_rot_message(p, -0.1f);
                pShip.serverRotvalue = p.playerShip.rotation;
                pShip.x = p.playerShip.location.X;
                pShip.y = p.playerShip.location.Y;
                OutQueue.add(QueueType.ship_rot_anti, pShip,p.id);
            }

            if (oldState.IsKeyDown(Keys.W) || P1oldPadState.IsButtonDown(Buttons.DPadUp))
            {
                Ship Player1Ship = p.playerShip;
                Vector2 direction = new Vector2((float)(Math.Cos(Player1Ship.physicsObj.body.GetAngle())), (float)(Math.Sin(Player1Ship.physicsObj.body.GetAngle())));
                direction.Normalize();
                direction *= shipSpeed;
                ship_impulse p1 = new ship_impulse(p, direction);
                p1.rot = p.playerShip.rotation;
                OutQueue.add(QueueType.ship_impulse, p1, p.id);
            }

            if ((oldState.IsKeyDown(Keys.X) && newState.IsKeyUp(Keys.X)) || (P1oldPadState.IsButtonDown(Buttons.A) && P1newPadState.IsButtonUp(Buttons.A)))
            {
                if (player1.state == PlayerState.alive && player1.missileAvailable())
                {
                    Ship_Create_Bomb_Message p1 = new Ship_Create_Bomb_Message(p);
                    p1.x = p.playerShip.location.X;
                    p1.y = p.playerShip.location.Y;
                    p1.rot = p.playerShip.rotation;
                    OutQueue.add(QueueType.ship_bomb, p1, p.id);
                }
            }

            if (oldState.IsKeyDown(Keys.C) && newState.IsKeyUp(Keys.C) || (P1oldPadState.IsButtonDown(Buttons.B) && P1newPadState.IsButtonUp(Buttons.B)))
            {
                if (player1.state == PlayerState.alive && BombManager.Instance().bombAvailable(p.id)) 
                {
                    Ship_Create_Missile_Message p1 = new Ship_Create_Missile_Message(p);
                    p1.x = p.playerShip.location.X;
                    p1.y = p.playerShip.location.Y;
                    p1.rot = p.playerShip.rotation;
                    OutQueue.add(QueueType.ship_missile, p1, p.id);
                }
            }

            P1oldPadState = P1newPadState;
            P2oldPadState = P2newPadState;
            oldState = newState;
        }

        private void clearData()
        {
            TextureManager.Instance().clear();
            ImageManager.Instance().clear();
            SpriteBatchManager.Instance().clear();
            SpriteProxyManager.Instance().clear();
            DisplayManager.Instance().clear();
            AnimManager.Instance().clear();
            GameObjManager.Instance().clear();
            Timer.Clear();
            PlayerManager.Instance().clear();
            BombManager.Instance().clear();
        }

        public void resetData()
        {
            clearData();
            LoadContent();
            ScoreManager.Instance().createData();
            networkSession.Dispose();
            networkSession = null;
            state = gameState.lobby;
            errorMessage = null;
        }
    }
}
