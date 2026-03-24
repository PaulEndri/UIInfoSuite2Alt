using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2Alt.Compatibility;
using HarmonyLib;
using System.Collections.Generic;
using UIInfoSuite2Alt.Infrastructure;
using UIInfoSuite2Alt.Patches;
using UIInfoSuite2Alt.Infrastructure.Structures;
using UIInfoSuite2Alt.Options;
using UIInfoSuite2Alt.UIElements;

namespace UIInfoSuite2Alt;

public partial class ModEntry : Mod
{
  public static ModConfig ModConfig { get; set; } = null!;

  private static EventHandler<ButtonsChangedEventArgs>? _calendarAndQuestKeyBindingsHandler;
  private static EventHandler<ButtonsChangedEventArgs>? _monsterEradicationKeyBindingsHandler;

  private static IModHelper _modHelper = null!;
  private ModOptionsPageHandler? _modOptionsPageHandler;

  public static IReflectionHelper Reflection { get; private set; } = null!;

  internal const string CustomIconsAssetName = "Mods/DazUki.UIInfoSuite2Alt/CustomIcons";

  public static IMonitor MonitorObject { get; private set; } = null!;

  /// <summary>Save the global config.json to disk.</summary>
  public static void SaveConfig()
  {
    _modHelper.WriteConfig(ModConfig);
  }

  #region Entry
  public override void Entry(IModHelper helper)
  {
    I18n.Init(helper.Translation);
    Reflection = helper.Reflection;
    MonitorObject = Monitor;
    _modHelper = helper;

    var harmony = new Harmony(ModManifest.UniqueID);
    TvChannelWatcher.Initialize(harmony, helper);
    ShowFishOnCatch.Initialize(harmony);
    HudMessagePatch.Initialize(harmony, helper.ModRegistry.IsLoaded(ModCompat.SpaceCore));

    ModConfig = Helper.ReadConfig<ModConfig>();

    helper.Events.Content.AssetRequested += OnAssetRequested;
    helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
    helper.Events.GameLoop.DayStarted += OnDayStarted;
    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    helper.Events.Display.RenderedHud += OnRenderedHud;

    RegisterCalendarAndQuestKeyBindings(helper, true);
    RegisterMonsterEradicationKeyBindings(helper, true);

    IconHandler.Handler.IsQuestLogPermanent = helper.ModRegistry.IsLoaded("MolsonCAD.DeluxeJournal");

    CheckForConflictingMods(helper);
  }
  #endregion

  #region Conflict detection
  private void CheckForConflictingMods(IModHelper helper)
  {
    var conflicts = new (string ModId, string Name)[]
    {
      (ModCompat.UIInfoSuite2, "UI Info Suite 2"),
      (ModCompat.UIInfoSuite, "UI Info Suite"),
    };

    foreach (var (modId, name) in conflicts)
    {
      if (helper.ModRegistry.IsLoaded(modId))
      {
        Monitor.Log(
          $"Detected '{name}' ({modId}) installed alongside UI Info Suite 2 Alternative. " +
          "Both mods provide the same features and will conflict. " +
          "Please remove one to avoid issues.",
          LogLevel.Alert
        );
      }
    }
  }
  #endregion

  // GMCM registration is in Options/GmcmRegistration.cs (partial class)

  #region Mod recommendations
  private static void LogModRecommendations(IModHelper helper)
  {
    var recommendations = new (string ModId, string Name, int NexusId, string Reason)[]
    {
      (ModCompat.Gmcm, "Generic Mod Config Menu", 5098,
        "Required to Change Keybinds in-game"),
      (ModCompat.NpcMapLocations, "NPC Map Locations", 239,
        "NPC map tracking was Removed in v2.7.0 - Use this mod instead"),
    };

    foreach (var (modId, name, nexusId, reason) in recommendations)
    {
      if (!helper.ModRegistry.IsLoaded(modId))
      {
        MonitorObject.Log($"Recommended mod not installed: {name} [Nexus:{nexusId}] - {reason}.", LogLevel.Info);
      }
    }
  }
  #endregion

  #region Event subscriptions
  private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
  {
    // Only main player screen
    if (Context.ScreenId != 0)
    {
      return;
    }

    _modOptionsPageHandler?.Dispose();
  }

  private void OnDayStarted(object? sender, DayStartedEventArgs e)
  {
    if (Context.ScreenId != 0)
    {
      return;
    }

    // Re-read config (may have been edited externally)
    ModConfig = Helper.ReadConfig<ModConfig>();
    ApplyFeatures();
  }

  /// <summary>Recreate feature handler to re-apply all toggles from current config.</summary>
  private void ApplyFeatures()
  {
    if (!Context.IsWorldReady)
    {
      return;
    }

    IconHandler.Handler.IconOrder = ModConfig.IconOrder;
    IconHandler.Handler.UseVerticalLayout = ModConfig.UseVerticalIconLayout;
    IconHandler.Handler.IconsPerRow = ModConfig.IconsPerRow;
    IconHandler.Handler.ShowQuestCount = ModConfig.ShowQuestCount;
    _modOptionsPageHandler?.Dispose();
    _modOptionsPageHandler = new ModOptionsPageHandler(Helper, ModConfig, SaveConfig);
  }


  private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
  {
    if (e.NameWithoutLocale.IsEquivalentTo(CustomIconsAssetName))
    {
      e.LoadFrom(
        () => new Dictionary<string, CustomIconData>(),
        AssetLoadPriority.Low
      );
    }
  }

  private static void OnRenderedHud(object? sender, RenderedHudEventArgs e)
  {
    IconHandler.Handler.DrawQueuedIcons(e.SpriteBatch);
  }

  public static void RegisterCalendarAndQuestKeyBindings(IModHelper helper, bool subscribe)
  {
    if (_calendarAndQuestKeyBindingsHandler == null)
    {
      _calendarAndQuestKeyBindingsHandler = (sender, e) => HandleCalendarAndQuestKeyBindings(helper);
    }

    helper.Events.Input.ButtonsChanged -= _calendarAndQuestKeyBindingsHandler;

    if (subscribe)
    {
      helper.Events.Input.ButtonsChanged += _calendarAndQuestKeyBindingsHandler;
    }
  }

  private static void HandleCalendarAndQuestKeyBindings(IModHelper helper)
  {
    if (Context.IsPlayerFree && ModConfig.OpenCalendarKeybind.JustPressed())
    {
      helper.Input.SuppressActiveKeybinds(ModConfig.OpenCalendarKeybind);
      Game1.activeClickableMenu = new Billboard();
    }
    else if (Context.IsPlayerFree && ModConfig.OpenQuestBoardKeybind.JustPressed())
    {
      helper.Input.SuppressActiveKeybinds(ModConfig.OpenQuestBoardKeybind);
      ShowCalendarAndBillboardOnGameMenuButton.OpenQuestBoardFromKeybind();
    }
    else if (Context.IsPlayerFree && ModConfig.OpenSpecialOrdersBoardKeybind.JustPressed())
    {
      helper.Input.SuppressActiveKeybinds(ModConfig.OpenSpecialOrdersBoardKeybind);
      ShowCalendarAndBillboardOnGameMenuButton.OpenSpecialOrdersBoardFromKeybind();
    }
  }

  public static void RegisterMonsterEradicationKeyBindings(IModHelper helper, bool subscribe)
  {
    if (_monsterEradicationKeyBindingsHandler == null)
    {
      _monsterEradicationKeyBindingsHandler = (sender, e) => HandleMonsterEradicationKeyBindings(helper);
    }

    helper.Events.Input.ButtonsChanged -= _monsterEradicationKeyBindingsHandler;

    if (subscribe)
    {
      helper.Events.Input.ButtonsChanged += _monsterEradicationKeyBindingsHandler;
    }
  }

  private static void HandleMonsterEradicationKeyBindings(IModHelper helper)
  {
    if (Context.IsPlayerFree && ModConfig.OpenMonsterEradicationKeybind.JustPressed())
    {
      helper.Input.SuppressActiveKeybinds(ModConfig.OpenMonsterEradicationKeybind);
      MonsterQuestHelper.ShowMonsterKillList();
    }
  }
  #endregion
}
