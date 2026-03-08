using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2Alt.AdditionalFeatures;
using UIInfoSuite2Alt.Compatibility;
using UIInfoSuite2Alt.Compatibility.CustomBush;
using HarmonyLib;
using UIInfoSuite2Alt.Infrastructure;
using UIInfoSuite2Alt.Options;
using UIInfoSuite2Alt.UIElements;

namespace UIInfoSuite2Alt;

public class ModEntry : Mod
{
  private static SkipIntro _skipIntro = null!; // Needed so GC won't throw away object with subscriptions
  public static Options.ModConfig ModConfig { get; set; } = null!;

  private static EventHandler<ButtonsChangedEventArgs>? _calendarAndQuestKeyBindingsHandler;

  private ModOptions _modOptions = null!;
  private ModOptionsPageHandler? _modOptionsPageHandler;

  public static IReflectionHelper Reflection { get; private set; } = null!;

  public static IMonitor MonitorObject { get; private set; } = null!;

  #region Entry
  public override void Entry(IModHelper helper)
  {
    I18n.Init(helper.Translation);
    Reflection = helper.Reflection;
    MonitorObject = Monitor;

    var harmony = new Harmony(ModManifest.UniqueID);
    TvChannelWatcher.Initialize(harmony, helper);
    ShowFishOnCatch.Initialize(harmony);

    _skipIntro = new SkipIntro(helper.Events);
    ModConfig = Helper.ReadConfig<Options.ModConfig>();

    helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
    helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    helper.Events.GameLoop.Saved += OnSaved;
    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    helper.Events.Display.RenderedHud += OnRenderedHud;

    IconHandler.Handler.IsQuestLogPermanent = helper.ModRegistry.IsLoaded("MolsonCAD.DeluxeJournal");
  }
  #endregion

  #region Generic mod config menu
  private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
  {
    SoundHelper.Instance.Initialize(Helper);

    // get Generic Mod Config Menu's API (if it's installed)
    var configMenu = ApiManager.TryRegisterApi<IGenericModConfigMenuApi>(Helper, ModCompat.Gmcm, "1.6.0");
    ApiManager.TryRegisterApi<ICustomBushApi>(Helper, ModCompat.CustomBush, "1.2.1", true);
    ApiManager.TryRegisterApi<ICloudySkiesApi>(Helper, ModCompat.CloudySkies);
    ApiManager.TryRegisterApi<IBetterGameMenuApi>(Helper, ModCompat.BetterGameMenu);
    ApiManager.TryRegisterApi<IFerngillSimpleEconomyApi>(Helper, ModCompat.FerngillEconomy);

    if (configMenu is null)
    {
      return;
    }

    // register mod
    configMenu.Register(ModManifest, () => ModConfig = new Options.ModConfig(), () => Helper.WriteConfig(ModConfig));

    // add some config options
    configMenu.AddBoolOption(
      ModManifest,
      name: () => I18n.Bool_ShowOptionsTabInMenu_DisplayedName(),
      tooltip: () => I18n.Bool_ShowOptionsTabInMenu_Tooltip(),
      getValue: () => ModConfig.ShowOptionsTabInMenu,
      setValue: value => ModConfig.ShowOptionsTabInMenu = value
    );
    configMenu.AddTextOption(
      ModManifest,
      name: () => I18n.Text_ApplyDefaultSettingsFromThisSave_DisplayedName(),
      tooltip: () => I18n.Text_ApplyDefaultSettingsFromThisSave_Tooltip(),
      getValue: () => ModConfig.ApplyDefaultSettingsFromThisSave,
      setValue: value => ModConfig.ApplyDefaultSettingsFromThisSave = value
    );
    configMenu.AddKeybindList(
      ModManifest,
      name: () => I18n.Keybinds_OpenCalendarKeybind_DisplayedName(),
      tooltip: () => I18n.Keybinds_OpenCalendarKeybind_Tooltip(),
      getValue: () => ModConfig.OpenCalendarKeybind,
      setValue: value => ModConfig.OpenCalendarKeybind = value
    );
    configMenu.AddKeybindList(
      ModManifest,
      name: () => I18n.Keybinds_OpenQuestBoardKeybind_DisplayedName(),
      tooltip: () => I18n.Keybinds_OpenQuestBoardKeybind_Tooltip(),
      getValue: () => ModConfig.OpenQuestBoardKeybind,
      setValue: value => ModConfig.OpenQuestBoardKeybind = value
    );
    configMenu.AddKeybindList(
      ModManifest,
      name: () => I18n.Keybinds_OpenModOptionsKeybind_DisplayedName(),
      tooltip: () => I18n.Keybinds_OpenModOptionsKeybind_Tooltip(),
      getValue: () => ModConfig.OpenModOptionsKeybind,
      setValue: value => ModConfig.OpenModOptionsKeybind = value
    );
    // Show item effect ranges
    configMenu.AddSectionTitle(
      ModManifest,
      text: () => I18n.Keybinds_Subtitle_ShowRange_DisplayedName(),
      tooltip: () => I18n.Keybinds_Subtitle_ShowRange_Tooltip()
      );
    configMenu.AddKeybindList(
      ModManifest,
      name: () => I18n.Keybinds_ShowOneRange_DisplayedName(),
      tooltip: () => I18n.Keybinds_ShowOneRange_Tooltip(),
      getValue: () => ModConfig.ShowOneRange,
      setValue: value => ModConfig.ShowOneRange = value
    );
    configMenu.AddKeybindList(
      ModManifest,
      name: () => I18n.Keybinds_ShowAllRange_DisplayedName(),
      tooltip: () => I18n.Keybinds_ShowAllRange_Tooltip(),
      getValue: () => ModConfig.ShowAllRange,
      setValue: value => ModConfig.ShowAllRange = value
      );
  }
  #endregion

  #region Event subscriptions
  private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
  {
    // Unload if the main player quits.
    if (Context.ScreenId != 0)
    {
      return;
    }

    _modOptionsPageHandler?.Dispose();
  }

  private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
  {
    // Only load once for split screen.
    if (Context.ScreenId != 0)
    {
      return;
    }

    _modOptions = Helper.Data.ReadJsonFile<ModOptions>($"data/{Constants.SaveFolderName}.json") ??
                  Helper.Data.ReadJsonFile<ModOptions>($"data/{ModConfig.ApplyDefaultSettingsFromThisSave}.json") ??
                  new ModOptions();

    IconHandler.Handler.IconOrder = _modOptions.IconOrder;

    _modOptionsPageHandler?.Dispose();
    _modOptionsPageHandler = new ModOptionsPageHandler(Helper, _modOptions);
  }

  private void OnSaved(object? sender, EventArgs e)
  {
    // Only save for the main player.
    if (Context.ScreenId != 0)
    {
      return;
    }

    Helper.Data.WriteJsonFile($"data/{Constants.SaveFolderName}.json", _modOptions);
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
      Game1.RefreshQuestOfTheDay();
      Game1.activeClickableMenu = new Billboard(true);
    }
  }
  #endregion
}
