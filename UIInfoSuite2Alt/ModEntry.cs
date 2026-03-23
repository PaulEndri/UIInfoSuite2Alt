using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2Alt.AdditionalFeatures;
using UIInfoSuite2Alt.Compatibility;
using UIInfoSuite2Alt.Compatibility.CustomBush;
using HarmonyLib;
using System.Collections.Generic;
using UIInfoSuite2Alt.Infrastructure;
using UIInfoSuite2Alt.Infrastructure.Extensions;
using UIInfoSuite2Alt.Patches;
using UIInfoSuite2Alt.Infrastructure.Structures;
using UIInfoSuite2Alt.Options;
using UIInfoSuite2Alt.UIElements;

namespace UIInfoSuite2Alt;

public class ModEntry : Mod
{
  private static SkipIntro _skipIntro = null!; // Needed so GC won't throw away object with subscriptions
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

    _skipIntro = new SkipIntro(helper.Events);
    ModConfig = Helper.ReadConfig<ModConfig>();

    helper.Events.Content.AssetRequested += OnAssetRequested;
    helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
    helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    helper.Events.GameLoop.DayStarted += OnDayStarted;
    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    helper.Events.Display.RenderedHud += OnRenderedHud;

    RegisterCalendarAndQuestKeyBindings(helper, true);
    RegisterMonsterEradicationKeyBindings(helper, true);

    IconHandler.Handler.IsQuestLogPermanent = helper.ModRegistry.IsLoaded("MolsonCAD.DeluxeJournal");
  }
  #endregion

  #region Generic mod config menu
  private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
  {
    SoundHelper.Instance.Initialize(Helper);

    // Register mod compatibility APIs
    var configMenu = ApiManager.TryRegisterApi<IGenericModConfigMenuApi>(Helper, ModCompat.Gmcm, "1.16.0");

    ApiManager.TryRegisterApi<IContentPatcherAPI>(Helper, ModCompat.ContentPatcher, "2.9.0");
    ApiManager.TryRegisterApi<ISpaceCoreApi>(Helper, ModCompat.SpaceCore, "1.28.4");
    ApiManager.TryRegisterApi<ICustomBushApi>(Helper, ModCompat.CustomBush, "1.2.1");
    ApiManager.TryRegisterApi<ICloudySkiesApi>(Helper, ModCompat.CloudySkies);
    ApiManager.TryRegisterApi<IBetterGameMenuApi>(Helper, ModCompat.BetterGameMenu);
    ApiManager.TryRegisterApi<IFerngillSimpleEconomyApi>(Helper, ModCompat.FerngillEconomy);

    ApiManager.LogLoadedApis();

    LogModRecommendations(Helper);

    if (configMenu is null)
    {
      return;
    }

    // Register GMCM
    configMenu.Register(
      ModManifest,
      reset: () => ModConfig = new Options.ModConfig(),
      save: () =>
      {
        Helper.WriteConfig(ModConfig);
        ApplyFeatures();
      }
    );

    // Global settings
    configMenu.AddBoolOption(
      ModManifest,
      name: () => I18n.Bool_ShowOptionsTabInMenu_DisplayedName(),
      tooltip: () => I18n.Bool_ShowOptionsTabInMenu_Tooltip(),
      getValue: () => ModConfig.ShowOptionsTabInMenu,
      setValue: value => ModConfig.ShowOptionsTabInMenu = value
    );

    // Keybinds
    configMenu.AddSectionTitle(ModManifest, text: () => I18n.Section_Keybinds());

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
      name: () => I18n.Keybinds_OpenSpecialOrdersBoardKeybind_DisplayedName(),
      tooltip: () => I18n.Keybinds_OpenSpecialOrdersBoardKeybind_Tooltip(),
      getValue: () => ModConfig.OpenSpecialOrdersBoardKeybind,
      setValue: value => ModConfig.OpenSpecialOrdersBoardKeybind = value
    );
    configMenu.AddKeybindList(
      ModManifest,
      name: () => I18n.Keybinds_OpenModOptionsKeybind_DisplayedName(),
      tooltip: () => I18n.Keybinds_OpenModOptionsKeybind_Tooltip(),
      getValue: () => ModConfig.OpenModOptionsKeybind,
      setValue: value => ModConfig.OpenModOptionsKeybind = value
    );
    configMenu.AddKeybindList(
      ModManifest,
      name: () => I18n.Keybinds_OpenMonsterEradicationKeybind_DisplayedName(),
      tooltip: () => I18n.Keybinds_OpenMonsterEradicationKeybind_Tooltip(),
      getValue: () => ModConfig.OpenMonsterEradicationKeybind,
      setValue: value => ModConfig.OpenMonsterEradicationKeybind = value
    );
    configMenu.AddKeybindList(
      ModManifest,
      name: () => I18n.Keybinds_ToggleMachineProcessingIcons_DisplayedName(),
      tooltip: () => I18n.Keybinds_ToggleMachineProcessingIcons_Tooltip(),
      getValue: () => ModConfig.ToggleMachineProcessingIcons,
      setValue: value => ModConfig.ToggleMachineProcessingIcons = value
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
    RegisterGmcmFeatureToggles(configMenu);
  }

  private void RegisterGmcmFeatureToggles(IGenericModConfigMenuApi configMenu)
  {
    // Helpers to reduce boilerplate
    void AddBool(string key, Func<bool> get, Action<bool> set) =>
      configMenu.AddBoolOption(ModManifest, name: () => Helper.SafeGetString(key), getValue: get, setValue: set);

    void AddSubBool(string key, Func<bool> get, Action<bool> set) =>
      configMenu.AddBoolOption(ModManifest, name: () => "  > " + Helper.SafeGetString(key), getValue: get, setValue: set);

    void Spacer() => configMenu.AddParagraph(ModManifest, text: () => "");

    // --- HUD Icons ---
    configMenu.AddSectionTitle(ModManifest, text: () => I18n.Section_HudIcons());
    configMenu.AddImage(ModManifest, () => Helper.ModContent.Load<Texture2D>("assets/banner_hud.png"), scale: 1);

    AddBool(nameof(ModConfig.UseVerticalIconLayout), () => ModConfig.UseVerticalIconLayout, v => ModConfig.UseVerticalIconLayout = v);

    string[] iconsPerRowValues = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };
    configMenu.AddTextOption(
      ModManifest,
      name: () => Helper.SafeGetString(nameof(ModConfig.IconsPerRow)),
      getValue: () => ModConfig.IconsPerRow.ToString(),
      setValue: v => ModConfig.IconsPerRow = int.Parse(v),
      allowedValues: iconsPerRowValues
    );

    AddBool(nameof(ModConfig.ShowLuckIcon), () => ModConfig.ShowLuckIcon, v => ModConfig.ShowLuckIcon = v);

    string[] luckIconStyles = { "0", "1", "2" };
    configMenu.AddTextOption(
      ModManifest,
      name: () => "  > " + Helper.SafeGetString(nameof(ModConfig.LuckIconStyle)),
      getValue: () => ModConfig.LuckIconStyle.ToString(),
      setValue: v => ModConfig.LuckIconStyle = int.Parse(v),
      allowedValues: luckIconStyles,
      formatAllowedValue: v => int.Parse(v) switch
      {
        0 => I18n.LuckIconStyle_Clover(),
        1 => I18n.LuckIconStyle_Dice(),
        2 => I18n.LuckIconStyle_TvFortune(),
        _ => v
      }
    );
    AddSubBool(nameof(ModConfig.ShowExactValue), () => ModConfig.ShowExactValue, v => ModConfig.ShowExactValue = v);
    AddSubBool(nameof(ModConfig.RequireTvForLuck), () => ModConfig.RequireTvForLuck, v => ModConfig.RequireTvForLuck = v);
    Spacer();
    AddBool(nameof(ModConfig.ShowRainyDay), () => ModConfig.ShowRainyDay, v => ModConfig.ShowRainyDay = v);
    AddSubBool(nameof(ModConfig.RequireTvForWeather), () => ModConfig.RequireTvForWeather, v => ModConfig.RequireTvForWeather = v);
    Spacer();
    AddBool(nameof(ModConfig.ShowBirthdayIcon), () => ModConfig.ShowBirthdayIcon, v => ModConfig.ShowBirthdayIcon = v);
    AddSubBool(nameof(ModConfig.HideBirthdayIfFullFriendShip), () => ModConfig.HideBirthdayIfFullFriendShip, v => ModConfig.HideBirthdayIfFullFriendShip = v);
    Spacer();
    AddBool(nameof(ModConfig.ShowTravelingMerchant), () => ModConfig.ShowTravelingMerchant, v => ModConfig.ShowTravelingMerchant = v);
    AddSubBool(nameof(ModConfig.HideMerchantWhenVisited), () => ModConfig.HideMerchantWhenVisited, v => ModConfig.HideMerchantWhenVisited = v);
    AddSubBool(nameof(ModConfig.ShowMerchantBundleIcon), () => ModConfig.ShowMerchantBundleIcon, v => ModConfig.ShowMerchantBundleIcon = v);
    AddSubBool(nameof(ModConfig.ShowMerchantBundleItemNames), () => ModConfig.ShowMerchantBundleItemNames, v => ModConfig.ShowMerchantBundleItemNames = v);
    Spacer();
    AddBool(nameof(ModConfig.ShowBookseller), () => ModConfig.ShowBookseller, v => ModConfig.ShowBookseller = v);
    AddSubBool(nameof(ModConfig.HideBooksellerWhenVisited), () => ModConfig.HideBooksellerWhenVisited, v => ModConfig.HideBooksellerWhenVisited = v);
    Spacer();
    AddBool(nameof(ModConfig.ShowFestivalIcon), () => ModConfig.ShowFestivalIcon, v => ModConfig.ShowFestivalIcon = v);
    AddBool(nameof(ModConfig.ShowWhenNewRecipesAreAvailable), () => ModConfig.ShowWhenNewRecipesAreAvailable, v => ModConfig.ShowWhenNewRecipesAreAvailable = v);
    AddSubBool(nameof(ModConfig.ShowRecipeItemIcon), () => ModConfig.ShowRecipeItemIcon, v => ModConfig.ShowRecipeItemIcon = v);
    Spacer();
    AddBool(nameof(ModConfig.ShowToolUpgradeStatus), () => ModConfig.ShowToolUpgradeStatus, v => ModConfig.ShowToolUpgradeStatus = v);
    AddBool(nameof(ModConfig.ShowRobinBuildingStatusIcon), () => ModConfig.ShowRobinBuildingStatusIcon, v => ModConfig.ShowRobinBuildingStatusIcon = v);
    AddBool(nameof(ModConfig.ShowSeasonalBerry), () => ModConfig.ShowSeasonalBerry, v => ModConfig.ShowSeasonalBerry = v);
    AddSubBool(nameof(ModConfig.ShowSeasonalBerryHazelnut), () => ModConfig.ShowSeasonalBerryHazelnut, v => ModConfig.ShowSeasonalBerryHazelnut = v);
    Spacer();
    AddBool(nameof(ModConfig.ShowTodaysGifts), () => ModConfig.ShowTodaysGifts, v => ModConfig.ShowTodaysGifts = v);
    AddBool(nameof(ModConfig.ShowQuestCount), () => ModConfig.ShowQuestCount, v => ModConfig.ShowQuestCount = v);
    AddBool(nameof(ModConfig.ShowBuffTimers), () => ModConfig.ShowBuffTimers, v => ModConfig.ShowBuffTimers = v);
    AddSubBool(nameof(ModConfig.PlayBuffExpireSound), () => ModConfig.PlayBuffExpireSound, v => ModConfig.PlayBuffExpireSound = v);
    Spacer();
    AddBool(nameof(ModConfig.ShowCustomIcons), () => ModConfig.ShowCustomIcons, v => ModConfig.ShowCustomIcons = v);

    // --- Farm & Field ---
    configMenu.AddSectionTitle(ModManifest, text: () => I18n.Section_FarmAndField());
    configMenu.AddImage(ModManifest, () => Helper.ModContent.Load<Texture2D>("assets/banner_ffield.png"), scale: 1);

    AddBool(nameof(ModConfig.ShowAnimalsNeedPets), () => ModConfig.ShowAnimalsNeedPets, v => ModConfig.ShowAnimalsNeedPets = v);
    AddSubBool(nameof(ModConfig.HideAnimalPetOnMaxFriendship), () => ModConfig.HideAnimalPetOnMaxFriendship, v => ModConfig.HideAnimalPetOnMaxFriendship = v);
    Spacer();
    AddBool(nameof(ModConfig.ShowCropTooltip), () => ModConfig.ShowCropTooltip, v => ModConfig.ShowCropTooltip = v);
    AddBool(nameof(ModConfig.ShowTreeTooltip), () => ModConfig.ShowTreeTooltip, v => ModConfig.ShowTreeTooltip = v);
    AddBool(nameof(ModConfig.ShowBarrelTooltip), () => ModConfig.ShowBarrelTooltip, v => ModConfig.ShowBarrelTooltip = v);
    AddBool(nameof(ModConfig.ShowFishPondTooltip), () => ModConfig.ShowFishPondTooltip, v => ModConfig.ShowFishPondTooltip = v);
    string[] machineIconModes = { "0", "1", "2" };
    configMenu.AddTextOption(
      ModManifest,
      name: () => Helper.SafeGetString(nameof(ModConfig.MachineProcessingIconsMode)),
      getValue: () => ModConfig.MachineProcessingIconsMode.ToString(),
      setValue: v => ModConfig.MachineProcessingIconsMode = int.Parse(v),
      allowedValues: machineIconModes,
      formatAllowedValue: v => int.Parse(v) switch
      {
        0 => I18n.MachineProcessingMode_Off(),
        1 => I18n.MachineProcessingMode_Toggle(),
        2 => I18n.MachineProcessingMode_Hold(),
        _ => v
      }
    );
    AddBool(nameof(ModConfig.ShowFishPondIcons), () => ModConfig.ShowFishPondIcons, v => ModConfig.ShowFishPondIcons = v);
    configMenu.AddBoolOption(
      ModManifest,
      name: () => I18n.ShowItemEffectRanges(),
      getValue: () => ModConfig.ShowItemEffectRanges,
      setValue: v => ModConfig.ShowItemEffectRanges = v
    );
    Spacer();
    configMenu.AddBoolOption(
      ModManifest,
      name: () => I18n.ShowBombRange(),
      getValue: () => ModConfig.ShowBombRange,
      setValue: v => ModConfig.ShowBombRange = v
    );
    Spacer();
    configMenu.AddBoolOption(
      ModManifest,
      name: () => I18n.EnableItemRangeKeybinds(),
      getValue: () => ModConfig.ButtonControlShow,
      setValue: v => ModConfig.ButtonControlShow = v
    );
    AddSubBool(nameof(ModConfig.ShowRangeTooltip), () => ModConfig.ShowRangeTooltip, v => ModConfig.ShowRangeTooltip = v);
    configMenu.AddComplexOption(
      ModManifest,
      name: () => "",
      draw: (spriteBatch, pos) =>
      {
        string text =
            $"{I18n.Keybinds_ShowOneRange_DisplayedName()}:\n"
          + $"  > {ModConfig.ShowOneRange}\n"
          + $"{I18n.Keybinds_ShowAllRange_DisplayedName()}:\n"
          + $"  > {ModConfig.ShowAllRange}";
        Utility.drawTextWithShadow(spriteBatch, text, Game1.smallFont, pos, Game1.textColor);
      },
      height: () => (int)(Game1.smallFont.MeasureString("T").Y * 5)
    );

    // --- Experience & Skills ---
    configMenu.AddSectionTitle(ModManifest, text: () => I18n.Section_ExperienceAndSkills());
    configMenu.AddImage(ModManifest, () => Helper.ModContent.Load<Texture2D>("assets/banner_exp.png"), scale: 1);

    AddBool(nameof(ModConfig.ShowLevelUpAnimation), () => ModConfig.ShowLevelUpAnimation, v => ModConfig.ShowLevelUpAnimation = v);
    AddBool(nameof(ModConfig.ShowExperienceBar), () => ModConfig.ShowExperienceBar, v => ModConfig.ShowExperienceBar = v);
    AddBool(nameof(ModConfig.AllowExperienceBarToFadeOut), () => ModConfig.AllowExperienceBarToFadeOut, v => ModConfig.AllowExperienceBarToFadeOut = v);
    AddBool(nameof(ModConfig.ShowExperienceGain), () => ModConfig.ShowExperienceGain, v => ModConfig.ShowExperienceGain = v);
    AddBool(nameof(ModConfig.ShowFishOnCatch), () => ModConfig.ShowFishOnCatch, v => ModConfig.ShowFishOnCatch = v);
    AddSubBool(nameof(ModConfig.ShowFishQualityStar), () => ModConfig.ShowFishQualityStar, v => ModConfig.ShowFishQualityStar = v);

    // --- Items & Shopping ---
    configMenu.AddSectionTitle(ModManifest, text: () => I18n.Section_ItemsAndShopping());
    configMenu.AddImage(ModManifest, () => Helper.ModContent.Load<Texture2D>("assets/banner_items.png"), scale: 1);

    AddBool(nameof(ModConfig.ShowExtraItemInformation), () => ModConfig.ShowExtraItemInformation, v => ModConfig.ShowExtraItemInformation = v);
    AddBool(nameof(ModConfig.ShowHarvestPricesInShop), () => ModConfig.ShowHarvestPricesInShop, v => ModConfig.ShowHarvestPricesInShop = v);

    // --- NPC & Social ---
    configMenu.AddSectionTitle(ModManifest, text: () => I18n.Section_NpcAndSocial());
    configMenu.AddImage(ModManifest, () => Helper.ModContent.Load<Texture2D>("assets/banner_npc.png"), scale: 1);

    AddBool(nameof(ModConfig.ShowHeartFills), () => ModConfig.ShowHeartFills, v => ModConfig.ShowHeartFills = v);
    AddBool(nameof(ModConfig.DisplayCalendarAndBillboard), () => ModConfig.DisplayCalendarAndBillboard, v => ModConfig.DisplayCalendarAndBillboard = v);

    // --- Icon Order ---
    configMenu.AddSectionTitle(ModManifest, text: () => I18n.Section_IconOrder());

    foreach (string key in IconHandler.IconKeys)
    {
      string capturedKey = key;

      configMenu.AddNumberOption(
        ModManifest,
        name: () => capturedKey switch
        {
          "Luck" => I18n.IconOrder_Luck(),
          "Weather" => I18n.IconOrder_Weather(),
          "Birthday" => I18n.IconOrder_Birthday(),
          "Festival" => I18n.IconOrder_Festival(),
          "QueenOfSauce" => I18n.IconOrder_QueenOfSauce(),
          "ToolUpgrade" => I18n.IconOrder_ToolUpgrade(),
          "RobinBuilding" => I18n.IconOrder_RobinBuilding(),
          "SeasonalBerry" => I18n.IconOrder_SeasonalBerry(),
          "TravelingMerchant" => I18n.IconOrder_TravelingMerchant(),
          "Bookseller" => I18n.IconOrder_Bookseller(),
          "CustomIcons" => I18n.IconOrder_CustomIcons(),
          _ => capturedKey
        },
        getValue: () => ModConfig.IconOrder.TryGetValue(capturedKey, out int v) ? v : 99,
        setValue: v => ModConfig.IconOrder[capturedKey] = v,
        min: 1,
        max: 20
      );
    }
  }
  #endregion

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

  private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
  {
    if (Context.ScreenId != 0)
    {
      return;
    }

    CleanUpLegacyPerSaveFiles();
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

  /// <summary>Rename legacy per-save .json files in data/ to .json.old.</summary>
  private void CleanUpLegacyPerSaveFiles()
  {
    string dataDir = Path.Combine(Helper.DirectoryPath, "data");
    if (!Directory.Exists(dataDir))
    {
      return;
    }

    string[] jsonFiles = Directory.GetFiles(dataDir, "*.json");
    if (jsonFiles.Length == 0)
    {
      return;
    }

    foreach (string file in jsonFiles)
    {
      string backupPath = Path.ChangeExtension(file, ".json.old");
      File.Move(file, backupPath, overwrite: true);
    }

    Monitor.Log($"Renamed {jsonFiles.Length} legacy settings file(s) to .json.old — settings are now global in config.json", LogLevel.Info);
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
