using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

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
      Game1.drawWithBorder(
        experienceEarnedThisLevel + "/" + experienceDifferenceBetweenLevels,
        Color.Black,
        Color.Black,
        new Vector2(leftSide + 33, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 70)
      );
    }
    else
    {
      Game1.spriteBatch.Draw(
        iconTexture ?? Game1.mouseCursors,
        new Vector2(leftSide + 54, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 62),
        experienceIconPosition,
        Color.White,
        0,
        Vector2.Zero,
        iconScale,
        SpriteEffects.None,
        0.85f
      );

      Game1.drawWithBorder(
        currentLevel.ToString(),
        Color.Black * 0.6f,
        Color.Black,
        new Vector2(leftSide + 33, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 70)
      );
    }
  }

  #region Static helpers
  private static int GetBarWidth(int experienceEarnedThisLevel, int experienceDifferenceBetweenLevels, int maxBarWidth)
  {
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
    return GetExperienceBarTextureComponent(leftSide, boxWidth).containsPoint(Game1.getMouseX(), Game1.getMouseY());
  }

  private static ClickableTextureComponent GetExperienceBarTextureComponent(float leftSide, int boxWidth)
  {
    return new ClickableTextureComponent(
      "",
      new Rectangle((int)leftSide - 36, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 80, boxWidth + 20, 100),
      "",
      "",
      Game1.mouseCursors,
      new Rectangle(0, 0, 0, 0),
      Game1.pixelZoom
    );
  }
  #endregion
}
