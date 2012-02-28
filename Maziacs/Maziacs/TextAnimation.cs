using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maziacs
{
    class TextAnimation
    {
        float alphaValue = 1;
        float fadeIncrement = .05f;
        double fadeDelay = .035;

        SpriteFont spriteFont;
        string text;

        Vector2 position;

        Color color;

        public void Initialize(SpriteFont font, string text, Vector2 position, Color color)
        {
            spriteFont = font;
            this.text = text;
            this.position = position;
            this.color = color;
        }

        public void Update(GameTime gameTime)
        {
            fadeDelay -= gameTime.ElapsedGameTime.TotalSeconds;

            if (fadeDelay <= 0)
            {
                fadeDelay = .035;

                alphaValue += fadeIncrement;

                if (alphaValue >= 1 || alphaValue <= 0)
                {
                    fadeIncrement *= -1;
                    alphaValue = MathHelper.Clamp(alphaValue, 0, 1);
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawString(spriteFont, text, position, Color.Lerp(Color.White, Color.Transparent, alphaValue));
        }
    }
}
