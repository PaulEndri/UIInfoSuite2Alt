using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using UIInfoSuite2Alt.UIElements;

namespace UIInfoSuite2Alt.Patches;

internal static class HudMessagePatch
{
  public static void Initialize(Harmony harmony, bool spaceCoreLoaded)
  {
    ModEntry.MonitorObject.Log(
      $"HudMessagePatch: spaceCoreLoaded={spaceCoreLoaded}",
      LogLevel.Trace
    );

    if (!spaceCoreLoaded)
    {
      return;
    }

    harmony.Patch(
      original: AccessTools.Method(typeof(HUDMessage), nameof(HUDMessage.draw)),
      prefix: new HarmonyMethod(typeof(HudMessagePatch), nameof(BeforeDraw))
    );
  }

  // Game loops hudMessages in reverse (Count-1 down to 0), so the first
  // draw call each frame has heightUsed == 0. Add our offset there to shift
  // the starting point for all notifications above the stacked experience bars.
  private static void BeforeDraw(ref int heightUsed)
  {
    if (heightUsed == 0)
    {
      heightUsed += ExperienceBar.GetNotificationOffset() + 2;
    }
  }
}
