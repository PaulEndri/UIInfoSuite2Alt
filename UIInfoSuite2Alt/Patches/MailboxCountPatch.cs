using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace UIInfoSuite2Alt.Patches;

internal static class MailboxCountPatch
{
  public static bool Enabled { get; set; }

  public static void Initialize(Harmony harmony)
  {
    harmony.Patch(
      original: AccessTools.Method(typeof(Farm), nameof(Farm.draw), [typeof(SpriteBatch)]),
      postfix: new HarmonyMethod(typeof(MailboxCountPatch), nameof(AfterDraw))
    );
  }

  private static void AfterDraw(SpriteBatch b)
  {
    int count = Game1.mailbox.Count;
    if (!Enabled || count <= 0)
    {
      return;
    }

    float bobbing = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
    Point mailboxPosition = Game1.player.getMailboxPosition();
    float layerDepth = (float)((mailboxPosition.X + 1) * 64) / 10000f
      + (float)(mailboxPosition.Y * 64) / 10000f + 1E-04f;

    // Position at bottom-right of the bubble, matching the vanilla bubble's coordinates
    Vector2 numberPos = Game1.GlobalToLocal(
      Game1.viewport,
      new Vector2(
        mailboxPosition.X * 64 + 56,
        mailboxPosition.Y * 64 - 96 - 48 + bobbing + 60
      )
    );

    Utility.drawTinyDigits(count, b, numberPos, 4f, layerDepth, Color.White * 0.8f);
  }
}
