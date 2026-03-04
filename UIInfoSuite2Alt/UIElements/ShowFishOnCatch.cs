using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace UIInfoSuite2Alt.UIElements;

internal class ShowFishOnCatch : IDisposable
{
  private static readonly PerScreen<bool> _enabled = new();
  private static readonly PerScreen<bool> _showQualityStar = new();

  public static void Initialize(Harmony harmony)
  {
    harmony.Patch(
      original: AccessTools.Method(typeof(BobberBar), nameof(BobberBar.draw), new[] { typeof(SpriteBatch) }),
      prefix: new HarmonyMethod(typeof(ShowFishOnCatch), nameof(BeforeDraw)),
      postfix: new HarmonyMethod(typeof(ShowFishOnCatch), nameof(AfterDraw))
    );
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool enabled)
  {
    _enabled.Value = enabled;
  }

  public void ToggleQualityStarOption(bool enabled)
  {
    _showQualityStar.Value = enabled;
  }

  // Temporarily add SonarBobber to the bobbers list so the game's own
  // rendering code draws the fish identity inside the correct coordinate space
  private static void BeforeDraw(List<string> ___bobbers)
  {
    if (_enabled.Value && !___bobbers.Contains("(O)SonarBobber"))
    {
      ___bobbers.Add("(O)SonarBobber");
    }
  }

  // Remove the injected SonarBobber after drawing, then draw quality star
  private static void AfterDraw(
    BobberBar __instance,
    SpriteBatch b,
    List<string> ___bobbers,
    int ___fishQuality,
    Vector2 ___everythingShake)
  {
    if (!_enabled.Value)
    {
      return;
    }

    ___bobbers.Remove("(O)SonarBobber");

    // Only show star when enabled, minigame is fully visible, and actively playing
    if (!_showQualityStar.Value || __instance.scale < 1f || __instance.fadeOut)
    {
      return;
    }

    // Calculate effective quality: perfect catch boosts +1 if at least silver
    int quality = ___fishQuality;
    if (__instance.perfect && quality >= 1)
    {
      quality++;
    }
    // Quality 3 doesn't exist in Stardew — jumps to 4 (iridium)
    if (quality == 3)
    {
      quality = 4;
    }

    if (quality <= 0)
    {
      return;
    }

    // Re-enter world draw coordinate space to match the fish icon position
    Game1.StartWorldDrawInUI(b);

    int xPos = __instance.xPositionOnScreen;
    int yPos = __instance.yPositionOnScreen;

    int iconX = (xPos > Game1.viewport.Width * 0.75f)
      ? (xPos - 80)
      : (xPos + 216);
    bool flipped = iconX < xPos;

    // Fish icon is drawn at this position by the game
    Vector2 fishIconPos = new Vector2(iconX, yPos)
      + new Vector2(flipped ? -8 : -4, 4f) * 4f
      + ___everythingShake;

    // Quality star sprite from Game1.mouseCursors
    Rectangle starRect = quality < 4
      ? new Rectangle(338 + (quality - 1) * 8, 400, 8, 8)
      : new Rectangle(346, 392, 8, 8);

    // Iridium star pulsing effect
    float pulseScale = quality < 4
      ? 0f
      : ((float)Math.Cos(Game1.currentGameTime.TotalGameTime.Milliseconds * Math.PI / 512.0) + 1f) * 0.05f;

    // Draw quality star at bottom-left of the fish icon (matching inventory style)
    b.Draw(
      Game1.mouseCursors,
      fishIconPos + new Vector2(12f, 52f + pulseScale),
      starRect,
      Color.White,
      0f,
      new Vector2(4f, 4f),
      3f * (1f + pulseScale),
      SpriteEffects.None,
      0.89f
    );

    Game1.EndWorldDrawInUI(b);
  }
}
