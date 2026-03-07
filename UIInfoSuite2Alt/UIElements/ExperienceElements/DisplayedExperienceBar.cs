using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace UIInfoSuite2Alt.UIElements.ExperienceElements;

public class DisplayedExperienceBar
{
  private const int DefaultBoxWidth = 240;
  private const int WideBoxWidth = 310;
  private const int DefaultMaxBarWidth = 175;
  private const int WideMaxBarWidth = 245;

  public void Draw(
    Color experienceFillColor,
    Rectangle experienceIconPosition,
    int experienceEarnedThisLevel,
    int experienceDifferenceBetweenLevels,
    int currentLevel,
    Texture2D? iconTexture = null,
    bool isWide = false,
    float iconScale = 2.9f
  )
  {
    int maxBarWidth = isWide ? WideMaxBarWidth : DefaultMaxBarWidth;
    int boxWidth = isWide ? WideBoxWidth : DefaultBoxWidth;
    int barWidth = GetBarWidth(experienceEarnedThisLevel, experienceDifferenceBetweenLevels, maxBarWidth);
    float leftSide = GetExperienceBarLeftSide();

    Game1.drawDialogueBox(
      (int)leftSide,
      Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 160,
      boxWidth,
      160,
      false,
      true
    );

    Game1.spriteBatch.Draw(
      Game1.staminaRect,
      new Rectangle((int)leftSide + 32, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 63, barWidth, 31),
      experienceFillColor
    );

    Game1.spriteBatch.Draw(
      Game1.staminaRect,
      new Rectangle(
        (int)leftSide + 32,
        Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 63,
        Math.Min(4, barWidth),
        31
      ),
      experienceFillColor
    );

    Game1.spriteBatch.Draw(
      Game1.staminaRect,
      new Rectangle((int)leftSide + 32, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 63, barWidth, 4),
      experienceFillColor
    );

    Game1.spriteBatch.Draw(
      Game1.staminaRect,
      new Rectangle((int)leftSide + 32, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 36, barWidth, 4),
      experienceFillColor
    );

    if (IsMouseOverExperienceBar(leftSide, boxWidth))
    {
      Vector2 pos = new Vector2(leftSide + 36, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 62);

      string text = experienceEarnedThisLevel + "/" + experienceDifferenceBetweenLevels;

      // Shadow
      Game1.spriteBatch.DrawString(
          Game1.smallFont,
          text,
          pos + new Vector2(1f, 1f),
          Color.Black * 0.4f
      );

      // Text
      Game1.spriteBatch.DrawString(
          Game1.smallFont,
          text,
          pos,
          new Color(28, 28, 28, 255)
      );
    }
    else
    {
      Game1.spriteBatch.Draw(
        iconTexture ?? Game1.mouseCursors,
        new Vector2(leftSide + 58, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 62),
        experienceIconPosition,
        Color.White,
        0,
        Vector2.Zero,
        iconScale,
        SpriteEffects.None,
        0.85f
      );

      Vector2 levelPos = new Vector2(leftSide + 36, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 62);
      string levelText = currentLevel.ToString();

      // Shadow
      Game1.spriteBatch.DrawString(
          Game1.smallFont,
          levelText,
          levelPos + new Vector2(1f, 1f),
          Color.Black * 0.4f
      );

      // Text
      Game1.spriteBatch.DrawString(
          Game1.smallFont,
          levelText,
          levelPos,
          new Color(28, 28, 28, 255)
      );
    }
  }

  #region Static helpers
  private static int GetBarWidth(int experienceEarnedThisLevel, int experienceDifferenceBetweenLevels, int maxBarWidth)
  {
    if (experienceDifferenceBetweenLevels <= 0)
    {
      return maxBarWidth;
    }

    return (int)((double)experienceEarnedThisLevel / experienceDifferenceBetweenLevels * maxBarWidth);
  }

  private static float GetExperienceBarLeftSide()
  {
    float leftSide = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Left;

    if (Game1.isOutdoorMapSmallerThanViewport())
    {
      int num3 = Game1.currentLocation.map.Layers[0].LayerWidth * Game1.tileSize;
      leftSide += (Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right - num3) / 2;
    }

    return leftSide;
  }

  private static bool IsMouseOverExperienceBar(float leftSide, int boxWidth)
  {
    int mouseX = Game1.getMouseX();
    int mouseY = Game1.getMouseY();
    int x = (int)leftSide - 36;
    int y = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 80;
    return mouseX >= x && mouseX < x + boxWidth + 20 && mouseY >= y && mouseY < y + 100;
  }
  #endregion
}
