using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;

namespace UIInfoSuite2Alt.Patches;

internal static class TvChannelWatcher
{
  public static readonly PerScreen<bool> HasWatchedWeather = new();
  public static readonly PerScreen<bool> HasWatchedFortune = new();

  public static void Initialize(Harmony harmony, IModHelper helper)
  {
    harmony.Patch(
      original: AccessTools.Method(typeof(TV), nameof(TV.selectChannel)),
      postfix: new HarmonyMethod(typeof(TvChannelWatcher), nameof(OnSelectChannel))
    );

    helper.Events.GameLoop.DayStarted += OnDayStarted;
  }

  private static void OnSelectChannel(string answer)
  {
    string channel = ArgUtility.SplitBySpaceAndGet(answer, 0) ?? "";
    switch (channel)
    {
      case "Weather":
        HasWatchedWeather.Value = true;
        break;
      case "Fortune":
        HasWatchedFortune.Value = true;
        break;
    }

    if (channel is "Weather" or "Fortune")
    {
      ModEntry.MonitorObject.Log(
        $"TvChannelWatcher: channel watched, channel={channel}",
        LogLevel.Trace
      );
    }
  }

  private static void OnDayStarted(object? sender, DayStartedEventArgs e)
  {
    HasWatchedWeather.Value = false;
    HasWatchedFortune.Value = false;
  }
}
