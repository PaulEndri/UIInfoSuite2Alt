using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace UIInfoSuite2Alt.Patches;

internal class ShowAccurateHearts : IDisposable
{
  #region Properties
  private static bool _enabled;

  // Heart fill shape (5x4 grid of 4px squares, drawn bottom-to-top)
  // @formatter:off
  private static readonly int[][] HeartFillShape =
  [
    [1, 1, 0, 1, 1],
    [1, 1, 1, 1, 1],
    [0, 1, 1, 1, 0],
    [0, 0, 1, 0, 0],
  ];

  // @formatter:on

  private const int FillOffsetX = 4;
  private const int FillOffsetY = 4;
  private const int PixelSize = 4;
  #endregion

  #region Lifecycle
  public static void Initialize(Harmony harmony)
  {
    harmony.Patch(
      original: AccessTools.Method(
        typeof(SocialPage),
        nameof(SocialPage.draw),
        [typeof(SpriteBatch)]
      ),
      postfix: new HarmonyMethod(typeof(ShowAccurateHearts), nameof(AfterSocialPageDraw))
    );

    ModEntry.MonitorObject.Log("ShowAccurateHearts: Harmony patch applied", LogLevel.Trace);
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool showAccurateHearts)
  {
    _enabled = showAccurateHearts;
  }
  #endregion

  #region Harmony patch
  private static void AfterSocialPageDraw(SocialPage __instance)
  {
    if (!_enabled)
    {
      return;
    }

    DrawHeartFills(__instance);
  }
  #endregion

  #region Logic
  private static void DrawHeartFills(SocialPage socialPage)
  {
    for (
      int i = socialPage.slotPosition;
      i < socialPage.slotPosition + 5 && i < socialPage.SocialEntries.Count;
      ++i
    )
    {
      string internalName = socialPage.SocialEntries[i].InternalName;
      if (
        Game1.player.friendshipData.TryGetValue(internalName, out Friendship friendshipValues)
        && friendshipValues.Points > 0
        && friendshipValues.Points
          < Utility.GetMaximumHeartsForCharacter(Game1.getCharacterFromName(internalName)) * 250
      )
      {
        int pointsToNextHeart = friendshipValues.Points % 250;
        int numHearts = friendshipValues.Points / 250;
        DrawPartialHeart(socialPage, i, numHearts, pointsToNextHeart);
      }
    }
  }

  private static void DrawPartialHeart(
    SocialPage socialPage,
    int slotIndex,
    int heartLevel,
    int friendshipPoints
  )
  {
    var numberOfPointsToDraw = (int)(friendshipPoints / 12.5);

    // Match game's heart positioning from SocialPage.drawNPCSlotHeart:
    // X = xPositionOnScreen + 320 - 4 + heartIndex * 32
    // Y (row 1) = sprites[i].bounds.Y + 36
    // Y (row 2) = sprites[i].bounds.Y + 64
    int heartIndex = heartLevel < 10 ? heartLevel : heartLevel - 10;
    int heartX = socialPage.xPositionOnScreen + 320 - 4 + heartIndex * 32;
    int heartY = socialPage.sprites[slotIndex].bounds.Y + (heartLevel < 10 ? 64 - 28 : 64);

    for (int row = 3; row >= 0 && numberOfPointsToDraw > 0; --row)
    {
      for (int col = 0; col < 5 && numberOfPointsToDraw > 0; ++col, --numberOfPointsToDraw)
      {
        if (HeartFillShape[row][col] == 1)
        {
          Game1.spriteBatch.Draw(
            Game1.staminaRect,
            new Rectangle(
              heartX + FillOffsetX + col * PixelSize,
              heartY + FillOffsetY + row * PixelSize,
              PixelSize,
              PixelSize
            ),
            Color.Crimson
          );
        }
      }
    }
  }
  #endregion
}
