using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace UIInfoSuite2Alt.UIElements;

internal class ShowBuffTimers : IDisposable
{
  private const float DigitScale = 2f;
  private const int DigitWidth = 5;
  private const int DigitHeight = 7;
  private const int ColonPadding = 2; // padding on each side of the colon dots
  private const int ColonDotGap = 4; // pixel width of the colon region (dot + inner spacing)
  private static readonly Color ShadowColor = Color.Black * 0.35f;
  private static readonly Color DigitColor = Color.White * 0.8f;
  private static readonly Color DotColor = Color.White * 0.8f;

  private readonly IModHelper _helper;

  public ShowBuffTimers(IModHelper helper)
  {
    _helper = helper;
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool showBuffTimers)
  {
    _helper.Events.Display.RenderedHud -= OnRenderedHud;

    if (showBuffTimers)
    {
      _helper.Events.Display.RenderedHud += OnRenderedHud;
    }
  }

  private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
  {
    if (!UIElementUtils.IsRenderingNormally())
    {
      return;
    }

    Dictionary<ClickableTextureComponent, Buff>? buffs = GetBuffComponents();
    if (buffs == null || buffs.Count == 0)
    {
      return;
    }

    SpriteBatch b = e.SpriteBatch;

    foreach (KeyValuePair<ClickableTextureComponent, Buff> pair in buffs)
    {
      Buff buff = pair.Value;

      // Skip permanent buffs (duration -2)
      if (buff.millisecondsDuration == -2)
      {
        continue;
      }

      ClickableTextureComponent icon = pair.Key;
      int totalSeconds = Math.Max(0, buff.millisecondsDuration / 1000);
      int minutes = totalSeconds / 60;
      int seconds = totalSeconds % 60;

      int totalWidth = GetTimerWidth(minutes);

      // Center below the buff icon, nudged down 2px
      float x = icon.bounds.X + icon.bounds.Width / 2f - totalWidth / 2f;
      float y = icon.bounds.Y + icon.bounds.Height + 2;

      DrawTimer(b, minutes, seconds, new Vector2(x, y));
    }
  }

  /// <summary>Draws a timer as "M:SS" using the game's tiny digit sprites with a colon separator.</summary>
  private static void DrawTimer(SpriteBatch b, int minutes, int seconds, Vector2 position)
  {
    float xOffset = 0;
    int scaledDigitStep = (int)(DigitWidth * DigitScale) - 1;

    // Draw minutes
    DrawTinyDigits(b, minutes, position, ref xOffset, scaledDigitStep);

    // Draw colon (two dots) with padding
    xOffset += ColonPadding;
    DrawColon(b, position, xOffset);
    xOffset += ColonDotGap + ColonPadding;

    // Draw seconds (always 2 digits)
    DrawTinyDigit(b, seconds / 10, position, ref xOffset, scaledDigitStep);
    DrawTinyDigit(b, seconds % 10, position, ref xOffset, scaledDigitStep);
  }

  private static void DrawTinyDigits(SpriteBatch b, int number, Vector2 position, ref float xOffset, int step)
  {
    if (number == 0)
    {
      DrawTinyDigit(b, 0, position, ref xOffset, step);
      return;
    }

    // Count digits to draw left-to-right
    int digitCount = 0;
    int temp = number;
    while (temp > 0) { digitCount++; temp /= 10; }

    int divisor = (int)Math.Pow(10, digitCount - 1);
    for (int i = 0; i < digitCount; i++)
    {
      int digit = number / divisor % 10;
      DrawTinyDigit(b, digit, position, ref xOffset, step);
      divisor /= 10;
    }
  }

  private static void DrawTinyDigit(SpriteBatch b, int digit, Vector2 position, ref float xOffset, int step)
  {
    var sourceRect = new Rectangle(368 + digit * DigitWidth, 56, DigitWidth, DigitHeight);

    // Shadow
    b.Draw(
      Game1.mouseCursors,
      position + new Vector2(xOffset + 1, 1),
      sourceRect,
      ShadowColor,
      0f, Vector2.Zero, DigitScale, SpriteEffects.None, 0.99f
    );

    // Digit
    b.Draw(
      Game1.mouseCursors,
      position + new Vector2(xOffset, 0f),
      sourceRect,
      DigitColor,
      0f, Vector2.Zero, DigitScale, SpriteEffects.None, 1f
    );

    xOffset += step;
  }

  private static void DrawColon(SpriteBatch b, Vector2 position, float xOffset)
  {
    float dotSize = DigitScale;
    float scaledHeight = DigitHeight * DigitScale;
    float dotX = position.X + xOffset + (ColonDotGap - dotSize) / 2f;

    // Upper dot (~30% from top)
    var upperPos = new Vector2(dotX, position.Y + scaledHeight * 0.25f);
    // Lower dot (~65% from top)
    var lowerPos = new Vector2(dotX, position.Y + scaledHeight * 0.6f);

    // Shadow
    DrawDot(b, upperPos + Vector2.One, dotSize, ShadowColor, 0.99f);
    DrawDot(b, lowerPos + Vector2.One, dotSize, ShadowColor, 0.99f);

    // Dots
    DrawDot(b, upperPos, dotSize, DotColor, 1f);
    DrawDot(b, lowerPos, dotSize, DotColor, 1f);
  }

  private static void DrawDot(SpriteBatch b, Vector2 position, float size, Color color, float layerDepth)
  {
    // Use a single white pixel from the mouseCursors sheet (solid area)
    b.Draw(
      Game1.staminaRect,
      new Rectangle((int)position.X, (int)position.Y, (int)size, (int)size),
      null,
      color,
      0f, Vector2.Zero, SpriteEffects.None, layerDepth
    );
  }

  private static int GetTimerWidth(int minutes)
  {
    int digitStep = (int)(DigitWidth * DigitScale) - 1;
    int colonStep = ColonPadding + ColonDotGap + ColonPadding;

    int minuteDigits = minutes == 0 ? 1 : (int)Math.Floor(Math.Log10(minutes)) + 1;
    const int secondDigits = 2;

    return (minuteDigits + secondDigits) * digitStep + colonStep;
  }

  private Dictionary<ClickableTextureComponent, Buff>? GetBuffComponents()
  {
    try
    {
      return _helper.Reflection
        .GetField<Dictionary<ClickableTextureComponent, Buff>>(Game1.buffsDisplay, "buffs")
        .GetValue();
    }
    catch
    {
      return null;
    }
  }
}
