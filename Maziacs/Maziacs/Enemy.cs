using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maziacs
{
    public class Enemy
    {
        public Vector2 Position;

        public int Width
        {
            get { return enemyAnimation.FrameWidth; }
        }

        public int Height
        {
            get { return enemyAnimation.FrameHeight; }
        }

        Animation enemyAnimation;

        public void Initialize(Animation enemyAnimation, Vector2 position)
        {
            this.enemyAnimation = enemyAnimation;
            Position = position;
        }

        public void Move(Vector2 position)
        {
            Position += position;
        }

        public void Update(GameTime gameTime)
        {
            enemyAnimation.Position = Position;
            enemyAnimation.Update(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            enemyAnimation.Draw(spriteBatch);
        }
    }
}
