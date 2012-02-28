using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maziacs
{
    public class Item
    {
        public Vector2 Position;

        public bool IsActive;

        public Maze.State Type;

        Animation itemAnimation;

        public void Initialize(Animation itemAnimation, Vector2 position, Maze.State Type)
        {
            this.itemAnimation = itemAnimation;
            this.Position = position;
            this.Type = Type;

            IsActive = true;
        }

        public void Update(GameTime gameTime)
        {
            itemAnimation.Position = Position;
            itemAnimation.Update(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsActive == false)
            {
                return;
            }

            itemAnimation.Draw(spriteBatch);
        }
    }
}
