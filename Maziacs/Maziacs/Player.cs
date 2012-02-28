using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace Maziacs
{
    public class Player
    {
        public enum State
        {
            Idle = 0,
            Up = 3,
            Right = 6,
            Down = 9,
            Left = 12
        }

        public Player()
        {

        }
        
        public Vector2 Position;

        public int Steps;
        public int Score;

        public bool HasSword;
        public bool HasTreasure;

        private float energy;

        public float Energy
        {
            set { energy = MathHelper.Clamp(value, 0, 480); }
            get { return energy; }
        }        

        public int Width
        {
            get { return playerAnimation.FrameWidth; }
        }

        public int Height
        {
            get { return playerAnimation.FrameHeight; }
        }

        Animation playerAnimation;

        public void Initialize(Animation playerAnimation, Vector2 position)
        {
            this.playerAnimation = playerAnimation;
            Position = position;
            Steps = 0;
            HasSword = true;
            HasTreasure = false;
            Energy = 480;
            Score = 0;
        }

        public void Move(Vector2 position, State state)
        {
            if (HasTreasure == true)
            {
                state += 2;
            }
            else if (HasSword == false)
            {
                state += 1;
            }

            if (position == Vector2.Zero)
            {
                if (playerAnimation.FrameRow != (int)state)
                {
                    playerAnimation.newFrameRow = (int)state;
                }
            }
            else
            {
                Position += position;
                Steps++;
                Energy -= 1.25f;

                playerAnimation.ChangeRow((int)state);
            }
        }

        public void Update(GameTime gameTime)
        {
            playerAnimation.Position = Position;
            playerAnimation.Update(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            playerAnimation.Draw(spriteBatch);
        }
    }
}
