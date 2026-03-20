using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2Alt.Compatibility;
using UIInfoSuite2Alt.Infrastructure;
using UIInfoSuite2Alt.Infrastructure.Extensions;
using UIInfoSuite2Alt.UIElements;

namespace UIInfoSuite2Alt.Options;

/// <summary>Manages mod options page injection into GameMenu (vanilla + BGM) and feature loading.</summary>
internal class ModOptionsPageHandler : IDisposable
{
  private const int downNeighborInInventory = 10; // above 11th inventory cell
  private const string optionsTabName = "uiinfosuite2";

  private const int bgmTabOrder = 170; // after Options (160), before Exit (200)

  private readonly PerScreen<bool> _changeToOurTabAfterTick = new(); // map page workaround (vanilla)
  private readonly List<IDisposable> _elementsToDispose;

  private readonly IModHelper _helper;
  private readonly bool _hasBgm;

  private readonly List<int> _instancesWithOptionsPageOpen = new(); // window resize workaround (vanilla)
  private readonly PerScreen<IClickableMenu?> _lastMenu = new();

  private readonly PerScreen<int?> _lastMenuTab = new();

  // Mod options page added to GameMenu.pages (vanilla only)
  private readonly PerScreen<ModOptionsPage?> _modOptionsPage = new();

  private readonly PerScreen<ModOptionsPageButton?> _modOptionsPageButton = new();

  // Gamepad nav component for our tab (not added to GameMenu.tabs — breaks game logic). Vanilla only.
  private readonly PerScreen<ClickableComponent?> _modOptionsTab = new();

  private readonly PerScreen<int?> _modOptionsTabPageNumber = new();

  private readonly List<ModOptionsElement> _optionsElements = new();
  private readonly PerScreen<ModOptionsPageState?> _savedPageState = new();
  private bool ShowPersonalConfigButton => ModEntry.ModConfig.ShowOptionsTabInMenu;

  private bool _addOurTabBeforeTick;
  private readonly PerScreen<bool> _switchToOurTabNextTick = new();
  private bool _windowResizing;

  public ModOptionsPageHandler(IModHelper helper, ModConfig config, Action saveConfig)
  {
    _helper = helper;
    _hasBgm = GameMenuHelper.HasBetterGameMenu;

    // Persist config.json on each change
    Action<bool> Set(Action<bool> setter) => v => { setter(v); saveConfig(); };
    Action<int> SetInt(Action<int> setter) => v => { setter(v); saveConfig(); };

    helper.Events.Input.ButtonsChanged += OnButtonsChanged;

    if (_hasBgm)
    {
      // BGM handles tab UI natively — minimal event wiring needed
    }
    else
    {
      helper.Events.Input.ButtonPressed += OnButtonPressed;
      helper.Events.GameLoop.UpdateTicking += OnUpdateTicking;
      helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
      helper.Events.Display.RenderingActiveMenu += OnRenderingMenu;
      helper.Events.Display.RenderedActiveMenu += OnRenderedMenu;
      GameRunner.instance.Window.ClientSizeChanged += OnWindowClientSizeChanged;
      helper.Events.Display.WindowResized += OnWindowResized;
    }

    var luckOfDay = new LuckOfDay(helper);
    var showBirthdayIcon = new ShowBirthdayIcon(helper);
    var showAccurateHearts = new ShowAccurateHearts(helper.Events);
    var showWhenAnimalNeedsPet = new ShowWhenAnimalNeedsPet(helper);
    var showCalendarAndBillboardOnGameMenuButton = new ShowCalendarAndBillboardOnGameMenuButton(helper);
    var showScarecrowAndSprinklerRange = new ShowItemEffectRanges(helper);
    var experienceBar = new ExperienceBar(helper);
    var showItemHoverInformation = new ShowItemHoverInformation(helper);
    var shopHarvestPrices = new ShopHarvestPrices(helper);
    var showQueenOfSauceIcon = new ShowQueenOfSauceIcon(helper);
    var showTravelingMerchant = new ShowTravelingMerchant(helper);
    var showBookseller = new ShowBookseller(helper);
    var showRainyDayIcon = new ShowRainyDayIcon(helper);
    var showMachineProcessingItem = new ShowMachineProcessingItem(helper);
    var showTileTooltips = new ShowTileTooltips(helper);
    var showToolUpgradeStatus = new ShowToolUpgradeStatus(helper);
    var showRobinBuildingStatusIcon = new ShowRobinBuildingStatusIcon(helper);
    var showSeasonalBerry = new ShowSeasonalBerry(helper);
    var showTodaysGift = new ShowTodaysGifts(helper);
    var showQuestCount = new ShowQuestCount(helper);
    var showFestivalIcon = new ShowFestivalIcon(helper);
    var showBuffTimers = new ShowBuffTimers(helper);
    var showFishOnCatch = new ShowFishOnCatch();

    _elementsToDispose = new List<IDisposable>
    {
      luckOfDay,
      showBirthdayIcon,
      showAccurateHearts,
      showWhenAnimalNeedsPet,
      showCalendarAndBillboardOnGameMenuButton,
      showScarecrowAndSprinklerRange,
      showItemHoverInformation,
      shopHarvestPrices,
      showQueenOfSauceIcon,
      showTravelingMerchant,
      showBookseller,
      showRainyDayIcon,
      showMachineProcessingItem,
      showTileTooltips,
      showToolUpgradeStatus,
      showRobinBuildingStatusIcon,
      showSeasonalBerry,
      showTodaysGift,
      showQuestCount,
      showBuffTimers,
      showFestivalIcon,
      showFishOnCatch,
      experienceBar
    };

    var whichOption = 1;
    _optionsElements.Add(new ModOptionsElement($"UI Info Suite 2 Alt. {GetVersionString(helper)}"));

    // --- HUD Icons ---
    _optionsElements.Add(new ModOptionsElement(I18n.Section_HudIcons()));

    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.UseVerticalIconLayout)),
        whichOption++,
        v => IconHandler.Handler.UseVerticalLayout = v,
        () => config.UseVerticalIconLayout,
        Set(v => config.UseVerticalIconLayout = v)
      )
    );

    var luckIcon = new ModOptionsCheckbox(
      _helper.SafeGetString(nameof(config.ShowLuckIcon)),
      whichOption++,
      luckOfDay.ToggleOption,
      () => config.ShowLuckIcon,
      Set(v => config.ShowLuckIcon = v)
    );
    _optionsElements.Add(luckIcon);
    _optionsElements.Add(
      new ModOptionsDropdown(
        _helper.SafeGetString(nameof(config.LuckIconStyle)),
        whichOption++,
        new List<string>
        {
          I18n.LuckIconStyle_Clover(),
          I18n.LuckIconStyle_Dice(),
          I18n.LuckIconStyle_TvFortune()
        },
        () => config.LuckIconStyle,
        SetInt(v => { config.LuckIconStyle = v; luckOfDay.SetIconStyle(v); }),
        luckIcon
      )
    );
    luckOfDay.SetIconStyle(config.LuckIconStyle);
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowExactValue)),
        whichOption++,
        luckOfDay.ToggleShowExactValueOption,
        () => config.ShowExactValue,
        Set(v => config.ShowExactValue = v),
        luckIcon
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.RequireTvForLuck)),
        whichOption++,
        luckOfDay.ToggleRequireTvOption,
        () => config.RequireTvForLuck,
        Set(v => config.RequireTvForLuck = v),
        luckIcon
      )
    );
    var rainyDayIcon = new ModOptionsCheckbox(
      _helper.SafeGetString(nameof(config.ShowRainyDay)),
      whichOption++,
      showRainyDayIcon.ToggleOption,
      () => config.ShowRainyDay,
      Set(v => config.ShowRainyDay = v)
    );
    _optionsElements.Add(rainyDayIcon);
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.RequireTvForWeather)),
        whichOption++,
        showRainyDayIcon.ToggleRequireTvOption,
        () => config.RequireTvForWeather,
        Set(v => config.RequireTvForWeather = v),
        rainyDayIcon
      )
    );
    var birthdayIcon = new ModOptionsCheckbox(
      _helper.SafeGetString(nameof(config.ShowBirthdayIcon)),
      whichOption++,
      showBirthdayIcon.ToggleOption,
      () => config.ShowBirthdayIcon,
      Set(v => config.ShowBirthdayIcon = v)
    );
    _optionsElements.Add(birthdayIcon);
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.HideBirthdayIfFullFriendShip)),
        whichOption++,
        showBirthdayIcon.ToggleDisableOnMaxFriendshipOption,
        () => config.HideBirthdayIfFullFriendShip,
        Set(v => config.HideBirthdayIfFullFriendShip = v),
        birthdayIcon
      )
    );
    var travellingMerchantIcon = new ModOptionsCheckbox(
      _helper.SafeGetString(nameof(config.ShowTravelingMerchant)),
      whichOption++,
      showTravelingMerchant.ToggleOption,
      () => config.ShowTravelingMerchant,
      Set(v => config.ShowTravelingMerchant = v)
    );
    _optionsElements.Add(travellingMerchantIcon);
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.HideMerchantWhenVisited)),
        whichOption++,
        showTravelingMerchant.ToggleHideWhenVisitedOption,
        () => config.HideMerchantWhenVisited,
        Set(v => config.HideMerchantWhenVisited = v),
        travellingMerchantIcon
      )
    );
    var merchantBundleIcon = new ModOptionsCheckbox(
      _helper.SafeGetString(nameof(config.ShowMerchantBundleIcon)),
      whichOption++,
      showTravelingMerchant.ToggleShowBundleIconOption,
      () => config.ShowMerchantBundleIcon,
      Set(v => config.ShowMerchantBundleIcon = v),
      travellingMerchantIcon
    );
    _optionsElements.Add(merchantBundleIcon);
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowMerchantBundleItemNames)),
        whichOption++,
        showTravelingMerchant.ToggleShowBundleItemNamesOption,
        () => config.ShowMerchantBundleItemNames,
        Set(v => config.ShowMerchantBundleItemNames = v),
        merchantBundleIcon
      )
    );
    var booksellerIcon = new ModOptionsCheckbox(
      _helper.SafeGetString(nameof(config.ShowBookseller)),
      whichOption++,
      showBookseller.ToggleOption,
      () => config.ShowBookseller,
      Set(v => config.ShowBookseller = v)
    );
    _optionsElements.Add(booksellerIcon);
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.HideBooksellerWhenVisited)),
        whichOption++,
        showBookseller.ToggleHideWhenVisitedOption,
        () => config.HideBooksellerWhenVisited,
        Set(v => config.HideBooksellerWhenVisited = v),
        booksellerIcon
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowFestivalIcon)),
        whichOption++,
        showFestivalIcon.ToggleOption,
        () => config.ShowFestivalIcon,
        Set(v => config.ShowFestivalIcon = v)
      )
    );
    var queenOfSauceCheckbox = new ModOptionsCheckbox(
      _helper.SafeGetString(nameof(config.ShowWhenNewRecipesAreAvailable)),
      whichOption++,
      showQueenOfSauceIcon.ToggleOption,
      () => config.ShowWhenNewRecipesAreAvailable,
      Set(v => config.ShowWhenNewRecipesAreAvailable = v)
    );
    _optionsElements.Add(queenOfSauceCheckbox);
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowRecipeItemIcon)),
        whichOption++,
        showQueenOfSauceIcon.ToggleShowRecipeItemIcon,
        () => config.ShowRecipeItemIcon,
        Set(v => config.ShowRecipeItemIcon = v),
        queenOfSauceCheckbox
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowToolUpgradeStatus)),
        whichOption++,
        showToolUpgradeStatus.ToggleOption,
        () => config.ShowToolUpgradeStatus,
        Set(v => config.ShowToolUpgradeStatus = v)
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowRobinBuildingStatusIcon)),
        whichOption++,
        showRobinBuildingStatusIcon.ToggleOption,
        () => config.ShowRobinBuildingStatusIcon,
        Set(v => config.ShowRobinBuildingStatusIcon = v)
      )
    );
    var seasonalBerryIcon = new ModOptionsCheckbox(
      _helper.SafeGetString(nameof(config.ShowSeasonalBerry)),
      whichOption++,
      showSeasonalBerry.ToggleOption,
      () => config.ShowSeasonalBerry,
      Set(v => config.ShowSeasonalBerry = v)
    );
    _optionsElements.Add(seasonalBerryIcon);
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowSeasonalBerryHazelnut)),
        whichOption++,
        showSeasonalBerry.ToggleHazelnutOption,
        () => config.ShowSeasonalBerryHazelnut,
        Set(v => config.ShowSeasonalBerryHazelnut = v),
        seasonalBerryIcon
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowTodaysGifts)),
        whichOption++,
        showTodaysGift.ToggleOption,
        () => config.ShowTodaysGifts,
        Set(v => config.ShowTodaysGifts = v)
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowQuestCount)),
        whichOption++,
        showQuestCount.ToggleOption,
        () => config.ShowQuestCount,
        Set(v => { config.ShowQuestCount = v; IconHandler.Handler.ShowQuestCount = v; })
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowBuffTimers)),
        whichOption++,
        showBuffTimers.ToggleOption,
        () => config.ShowBuffTimers,
        Set(v => config.ShowBuffTimers = v)
      )
    );

    // --- Farm & Field ---
    _optionsElements.Add(new ModOptionsElement(I18n.Section_FarmAndField()));

    var animalPetIcon = new ModOptionsCheckbox(
      _helper.SafeGetString(nameof(config.ShowAnimalsNeedPets)),
      whichOption++,
      showWhenAnimalNeedsPet.ToggleOption,
      () => config.ShowAnimalsNeedPets,
      Set(v => config.ShowAnimalsNeedPets = v)
    );
    _optionsElements.Add(animalPetIcon);
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.HideAnimalPetOnMaxFriendship)),
        whichOption++,
        showWhenAnimalNeedsPet.ToggleDisableOnMaxFriendshipOption,
        () => config.HideAnimalPetOnMaxFriendship,
        Set(v => config.HideAnimalPetOnMaxFriendship = v),
        animalPetIcon
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowCropTooltip)),
        whichOption++,
        showTileTooltips.ToggleCropOption,
        () => config.ShowCropTooltip,
        Set(v => config.ShowCropTooltip = v)
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowTreeTooltip)),
        whichOption++,
        showTileTooltips.ToggleTreeOption,
        () => config.ShowTreeTooltip,
        Set(v => config.ShowTreeTooltip = v)
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowBarrelTooltip)),
        whichOption++,
        showTileTooltips.ToggleBarrelOption,
        () => config.ShowBarrelTooltip,
        Set(v => config.ShowBarrelTooltip = v)
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowFishPondTooltip)),
        whichOption++,
        showTileTooltips.ToggleFishPondOption,
        () => config.ShowFishPondTooltip,
        Set(v => config.ShowFishPondTooltip = v)
      )
    );
    showMachineProcessingItem.SetMode(config.MachineProcessingIconsMode);
    _optionsElements.Add(
      new ModOptionsDropdown(
        _helper.SafeGetString(nameof(config.MachineProcessingIconsMode)),
        whichOption++,
        new List<string>
        {
          I18n.MachineProcessingMode_Off(),
          I18n.MachineProcessingMode_Toggle(),
          I18n.MachineProcessingMode_Hold()
        },
        () => config.MachineProcessingIconsMode,
        v => { config.MachineProcessingIconsMode = v; saveConfig(); showMachineProcessingItem.SetMode(v); }
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowFishPondIcons)),
        whichOption++,
        showMachineProcessingItem.ToggleFishPondOption,
        () => config.ShowFishPondIcons,
        Set(v => config.ShowFishPondIcons = v)
      )
    );
    var ScarecrowAndSprinklerRangeIcon = new ModOptionsCheckbox(
      I18n.ShowItemEffectRanges(),
      whichOption++,
      showScarecrowAndSprinklerRange.ToggleOption,
      () => config.ShowItemEffectRanges,
      Set(v => config.ShowItemEffectRanges = v)
    );
    _optionsElements.Add(ScarecrowAndSprinklerRangeIcon);
    _optionsElements.Add(
      new ModOptionsCheckbox(
        I18n.ButtonControlShow(),
        whichOption++,
        showScarecrowAndSprinklerRange.ToggleButtonControlShowOption,
        () => config.ButtonControlShow,
        Set(v => config.ButtonControlShow = v),
        ScarecrowAndSprinklerRangeIcon
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        I18n.ShowBombRange(),
        whichOption++,
        showScarecrowAndSprinklerRange.ToggleShowBombRangeOption,
        () => config.ShowBombRange,
        Set(v => config.ShowBombRange = v)
      )
    );

    // --- Experience & Skills ---
    _optionsElements.Add(new ModOptionsElement(I18n.Section_ExperienceAndSkills()));

    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowLevelUpAnimation)),
        whichOption++,
        experienceBar.ToggleLevelUpAnimation,
        () => config.ShowLevelUpAnimation,
        Set(v => config.ShowLevelUpAnimation = v)
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowExperienceBar)),
        whichOption++,
        experienceBar.ToggleShowExperienceBar,
        () => config.ShowExperienceBar,
        Set(v => config.ShowExperienceBar = v)
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.AllowExperienceBarToFadeOut)),
        whichOption++,
        experienceBar.ToggleExperienceBarFade,
        () => config.AllowExperienceBarToFadeOut,
        Set(v => config.AllowExperienceBarToFadeOut = v)
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowExperienceGain)),
        whichOption++,
        experienceBar.ToggleShowExperienceGain,
        () => config.ShowExperienceGain,
        Set(v => config.ShowExperienceGain = v)
      )
    );
    var fishOnCatchIcon = new ModOptionsCheckbox(
      _helper.SafeGetString(nameof(config.ShowFishOnCatch)),
      whichOption++,
      showFishOnCatch.ToggleOption,
      () => config.ShowFishOnCatch,
      Set(v => config.ShowFishOnCatch = v)
    );
    _optionsElements.Add(fishOnCatchIcon);
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowFishQualityStar)),
        whichOption++,
        showFishOnCatch.ToggleQualityStarOption,
        () => config.ShowFishQualityStar,
        Set(v => config.ShowFishQualityStar = v),
        fishOnCatchIcon
      )
    );

    // --- Items & Shopping ---
    _optionsElements.Add(new ModOptionsElement(I18n.Section_ItemsAndShopping()));

    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowExtraItemInformation)),
        whichOption++,
        showItemHoverInformation.ToggleOption,
        () => config.ShowExtraItemInformation,
        Set(v => config.ShowExtraItemInformation = v)
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowHarvestPricesInShop)),
        whichOption++,
        shopHarvestPrices.ToggleOption,
        () => config.ShowHarvestPricesInShop,
        Set(v => config.ShowHarvestPricesInShop = v)
      )
    );

    // --- NPC & Social ---
    _optionsElements.Add(new ModOptionsElement(I18n.Section_NpcAndSocial()));

    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.ShowHeartFills)),
        whichOption++,
        showAccurateHearts.ToggleOption,
        () => config.ShowHeartFills,
        Set(v => config.ShowHeartFills = v)
      )
    );
    _optionsElements.Add(
      new ModOptionsCheckbox(
        _helper.SafeGetString(nameof(config.DisplayCalendarAndBillboard)),
        whichOption++,
        showCalendarAndBillboardOnGameMenuButton.ToggleOption,
        () => config.DisplayCalendarAndBillboard,
        Set(v => config.DisplayCalendarAndBillboard = v)
      )
    );

    // --- Icon Order ---
    _optionsElements.Add(new ModOptionsElement(I18n.Section_IconOrder()));
    _optionsElements.Add(new ModOptionsElement(I18n.Section_IconOrder_Subtitle(), isSubtitle: true));

    foreach (string key in IconHandler.IconKeys)
    {
      string label = key switch
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
        _ => key
      };

      string capturedKey = key;
      _optionsElements.Add(
        new ModOptionsNumberPicker(
          label,
          whichOption++,
          () => config.IconOrder.TryGetValue(capturedKey, out int v) ? v : 99,
          SetInt(v => config.IconOrder[capturedKey] = v)
        )
      );
    }

    if (_hasBgm)
    {
      RegisterBgmTab();
    }
  }


  public void Dispose()
  {
    foreach (IDisposable item in _elementsToDispose)
    {
      item.Dispose();
    }

    _helper.Events.Input.ButtonsChanged -= OnButtonsChanged;

    if (_hasBgm)
    {
      DisposeBgm();
    }
    else
    {
      _helper.Events.Input.ButtonPressed -= OnButtonPressed;
      _helper.Events.GameLoop.UpdateTicking -= OnUpdateTicking;
      _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
      _helper.Events.Display.RenderingActiveMenu -= OnRenderingMenu;
      _helper.Events.Display.RenderedActiveMenu -= OnRenderedMenu;
      GameRunner.instance.Window.ClientSizeChanged -= OnWindowClientSizeChanged;
      _helper.Events.Display.WindowResized -= OnWindowResized;
    }
  }

  #region Better Game Menu integration

  private void RegisterBgmTab()
  {
    if (!ApiManager.GetApi<IBetterGameMenuApi>(ModCompat.BetterGameMenu, out IBetterGameMenuApi? bgmApi))
    {
      return;
    }

    // Register our tab with BGM
    IBetterGameMenuApi.DrawDelegate iconDraw = bgmApi.CreateDraw(
      Game1.mouseCursors,
      new Rectangle(32, 672, 16, 16),
      scale: 2f
    );

    bgmApi.RegisterTab(
      id: optionsTabName,
      order: bgmTabOrder,
      getDisplayName: () => I18n.OptionsTabTooltip(),
      getIcon: () => (iconDraw, true),
      priority: 0,
      getPageInstance: menu => new ModOptionsPage(_optionsElements, _helper.Events, menu),
      getTabVisible: () => ShowPersonalConfigButton,
      getWidth: w => w,
      getHeight: h => h,
      onResize: ctx => new ModOptionsPage(_optionsElements, _helper.Events, ctx.Menu)
    );

    // Right-click opens GMCM if available
    bgmApi.OnTabContextMenu(OnBgmTabContextMenu);
  }

  private void OnBgmTabContextMenu(ITabContextMenuEvent evt)
  {
    if (evt.Tab != optionsTabName)
    {
      return;
    }

    if (ApiManager.GetApi<IGenericModConfigMenuApi>(ModCompat.Gmcm, out IGenericModConfigMenuApi? gmcm))
    {
      IModInfo? modInfo = _helper.ModRegistry.Get(_helper.ModRegistry.ModID);
      if (modInfo != null)
      {
        evt.Entries.Add(evt.CreateEntry(
          I18n.OpenSettings(),
          () => gmcm.OpenModMenu(modInfo.Manifest)
        ));
      }
    }
  }

  private void DisposeBgm()
  {
    if (ApiManager.GetApi<IBetterGameMenuApi>(ModCompat.BetterGameMenu, out IBetterGameMenuApi? bgmApi))
    {
      bgmApi.UnregisterImplementation(optionsTabName);
      bgmApi.OffTabContextMenu(OnBgmTabContextMenu);
    }
  }

  #endregion

  #region Vanilla GameMenu support

  private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
  {
    if (!ShowPersonalConfigButton)
    {
      return;
    }

    if (Game1.activeClickableMenu is GameMenu gameMenu)
    {
      // Right trigger → our tab (left trigger is handled by the game)
      if (e.Button == SButton.RightTrigger && !e.IsSuppressed())
      {
        if (gameMenu.currentTab + 1 == _modOptionsTabPageNumber.Value && gameMenu.readyToClose())
        {
          ChangeToOurTab(gameMenu);
          _helper.Input.Suppress(SButton.RightTrigger);
        }
      }

      // Based on GameMenu.receiveLeftClick / Game1.updateActiveMenu
      if ((e.Button == SButton.MouseLeft || e.Button == SButton.ControllerA) && !e.IsSuppressed())
      {
        // Workaround: map page calls GameMenu.changeTab which fails for our tab
        if (gameMenu.currentTab == GameMenu.mapTab && gameMenu.lastOpenedNonMapTab == _modOptionsTabPageNumber.Value)
        {
          _changeToOurTabAfterTick.Value = true;
          gameMenu.lastOpenedNonMapTab = GameMenu.optionsTab;
          ModEntry.MonitorObject.Log(
            $"{GetType().Name}: The map page is about to close and the menu will switch to our tab, applying workaround"
          );
        }

        if (!gameMenu.invisible && !GameMenu.forcePreventClose)
        {
          const bool uiScale = true;
          if (_modOptionsTab.Value?.containsPoint(Game1.getMouseX(uiScale), Game1.getMouseY(uiScale)) == true &&
              gameMenu.currentTab != _modOptionsTabPageNumber.Value &&
              gameMenu.readyToClose())
          {
            ChangeToOurTab(gameMenu);
            _helper.Input.Suppress(e.Button);
          }
        }
      }
    }
  }

  #endregion

  private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
  {
    KeybindList keybind = ModEntry.ModConfig.OpenModOptionsKeybind;
    if (!keybind.JustPressed())
    {
      return;
    }

    _helper.Input.SuppressActiveKeybinds(keybind);

    if (_hasBgm)
    {
      OnButtonsChanged_Bgm();
    }
    else
    {
      OnButtonsChanged_Vanilla();
    }
  }

  private void OnButtonsChanged_Bgm()
  {
    if (!ApiManager.GetApi<IBetterGameMenuApi>(ModCompat.BetterGameMenu, out IBetterGameMenuApi? bgmApi))
    {
      return;
    }

    IClickableMenu? menu = Game1.activeClickableMenu;
    IBetterGameMenu? bgmMenu = menu != null ? bgmApi.AsMenu(menu) : null;

    if (bgmMenu != null)
    {
      // Already in BGM — switch to our tab
      bgmMenu.TryChangeTab(optionsTabName);
    }
    else if (Context.IsPlayerFree)
    {
      // Open BGM to our tab
      Game1.activeClickableMenu = bgmApi.CreateMenu(optionsTabName);
    }
  }

  private void OnButtonsChanged_Vanilla()
  {
    if (Game1.activeClickableMenu is GameMenu gameMenu)
    {
      // Already in GameMenu — switch to our tab
      if (_modOptionsTabPageNumber.Value != null && gameMenu.readyToClose())
      {
        ChangeToOurTab(gameMenu);
      }
    }
    else if (Context.IsPlayerFree)
    {
      // Open GameMenu and defer tab switch to next tick (page not yet added)
      Game1.activeClickableMenu = new GameMenu();
      _switchToOurTabNextTick.Value = true;
    }
  }

  #region Vanilla-only event handlers

  private void OnUpdateTicking(object? sender, EventArgs e)
  {
    // Window resize workaround: re-add our tab before the tick
    if (_addOurTabBeforeTick)
    {
      _addOurTabBeforeTick = false;
      GameRunner.instance.ExecuteForInstances(
        instance =>
        {
          if (_lastMenu.Value != Game1.activeClickableMenu)
          {
            EarlyOnMenuChanged(_lastMenu.Value, Game1.activeClickableMenu);
            _lastMenu.Value = Game1.activeClickableMenu;
          }
        }
      );
      ModEntry.MonitorObject.Log(
        $"{GetType().Name}: Our tab was added back as the final step of the window resize workaround"
      );
    }
  }

  private void OnUpdateTicked(object? sender, EventArgs e)
  {
    var gameMenu = Game1.activeClickableMenu as GameMenu;

    // Map closed → switch back to our tab
    if (_changeToOurTabAfterTick.Value)
    {
      _changeToOurTabAfterTick.Value = false;
      if (gameMenu != null)
      {
        ChangeToOurTab(gameMenu);
        ModEntry.MonitorObject.Log($"{GetType().Name}: Changed back to our tab");
      }
    }

    if (_lastMenu.Value != Game1.activeClickableMenu)
    {
      EarlyOnMenuChanged(_lastMenu.Value, Game1.activeClickableMenu);
      _lastMenu.Value = Game1.activeClickableMenu;
      gameMenu = Game1.activeClickableMenu as GameMenu;
    }

    // Deferred tab switch from keybind open
    if (_switchToOurTabNextTick.Value)
    {
      _switchToOurTabNextTick.Value = false;
      if (gameMenu != null && _modOptionsTabPageNumber.Value != null)
      {
        ChangeToOurTab(gameMenu);
      }
    }

    if (_lastMenuTab.Value != gameMenu?.currentTab)
    {
      OnGameMenuTabChanged(gameMenu);
      _lastMenuTab.Value = gameMenu?.currentTab;
    }
  }

  // Called during UpdateTicked (earlier than Display.MenuChanged)
  private void EarlyOnMenuChanged(IClickableMenu? oldMenu, IClickableMenu? newMenu)
  {
    // Remove from old menu
    if (oldMenu is GameMenu oldGameMenu)
    {
      if (_modOptionsPage.Value != null)
      {
        oldGameMenu.pages.Remove(_modOptionsPage.Value);
        _modOptionsPage.Value = null;
      }

      if (_modOptionsPageButton.Value != null)
      {
        _modOptionsPageButton.Value = null;
      }

      _modOptionsTabPageNumber.Value = null;
      _modOptionsTab.Value = null;
    }

    // Add to new menu
    if (newMenu is GameMenu newGameMenu)
    {
      // Requires Game1.activeClickableMenu to not be null
      if (_modOptionsPage.Value == null)
      {
        _modOptionsPage.Value = new ModOptionsPage(_optionsElements, _helper.Events);
      }

      if (ShowPersonalConfigButton && _modOptionsPageButton.Value == null)
      {
        _modOptionsPageButton.Value = new ModOptionsPageButton();
        _modOptionsPageButton.Value.xPositionOnScreen = GetButtonXPosition(newGameMenu);
      }

      List<IClickableMenu> tabPages = newGameMenu.pages;
      _modOptionsTabPageNumber.Value = tabPages.Count;
      tabPages.Add(_modOptionsPage.Value);

      // Restore saved page state (from resize)
      if (_savedPageState.Value != null)
      {
        _modOptionsPage.Value.LoadState(_savedPageState.Value);
        _savedPageState.Value = null;
      }

      // name = tab id, label = hover text
      _modOptionsTab.Value = new ClickableComponent(
          new Rectangle(
            GetButtonXPosition(newGameMenu),
            newGameMenu.yPositionOnScreen + IClickableMenu.tabYPositionRelativeToMenuY + 64,
            64,
            64
          ),
          optionsTabName,
          "ui2_mod_options"
        )
      {
        myID = 12348, // exit page tab is 12347

        leftNeighborID = 12347,
        tryDefaultIfNoDownNeighborExists = true,
        fullyImmutable = true
      };

      // Don't add to GameMenu.tabs — GameMenu.draw breaks when our page is current tab
      ClickableComponent? exitTab = newGameMenu.tabs.Find(tab => tab.myID == 12347);
      if (exitTab != null)
      {
        exitTab.rightNeighborID = _modOptionsTab.Value.myID;
        AddOurTabToClickableComponents(newGameMenu, _modOptionsTab.Value);
      }
      else
      {
        ModEntry.MonitorObject.LogOnce(
          $"{GetType().Name}: Did not find the ExitPage tab in the new GameMenu.tabs",
          LogLevel.Error
        );
      }
    }
  }

  private void OnGameMenuTabChanged(GameMenu? gameMenu)
  {
    if (gameMenu != null)
    {
      if (ShowPersonalConfigButton && _modOptionsTab.Value != null)
      {
        // Based on GameMenu.setTabNeighborsForCurrentPage
        if (gameMenu.currentTab == GameMenu.inventoryTab)
        {
          _modOptionsTab.Value.downNeighborID = downNeighborInInventory;
        }
        else if (gameMenu.currentTab == GameMenu.exitTab)
        {
          _modOptionsTab.Value.downNeighborID = 535;
        }
        else
        {
          _modOptionsTab.Value.downNeighborID = ClickableComponent.SNAP_TO_DEFAULT;
        }

        AddOurTabToClickableComponents(gameMenu, _modOptionsTab.Value);
      }
    }
  }

  private void OnRenderingMenu(object? sender, RenderingActiveMenuEventArgs e)
  {
    if (!ShowPersonalConfigButton)
    {
      return;
    }

    if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.GetChildMenu() == null)
    {
      // Draw behind the menu so it's visible during transitions (e.g. collections letter view)
      DrawButton(gameMenu);
    }
  }

  private void OnRenderedMenu(object? sender, RenderedActiveMenuEventArgs e)
  {
    if (!ShowPersonalConfigButton)
    {
      return;
    }

    if (Game1.activeClickableMenu is not GameMenu gameMenu ||
        gameMenu.currentTab == GameMenu.mapTab ||
        gameMenu.GetChildMenu() != null ||
        gameMenu.GetCurrentPage() is CollectionsPage { letterviewerSubMenu: not null })
    {
      return;
    }

    DrawButton(gameMenu);

    Tools.DrawMouseCursor();

    // Re-draw game menu hover text above our tab
    if (!gameMenu.hoverText.Equals(""))
    {
      IClickableMenu.drawHoverText(Game1.spriteBatch, gameMenu.hoverText, Game1.smallFont);
    }

    // Our tab's hover text
    if (_modOptionsTab.Value?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) == true)
    {
      IClickableMenu.drawHoverText(Game1.spriteBatch, I18n.OptionsTabTooltip(), Game1.smallFont);

      if (!gameMenu.hoverText.Equals(""))
      {
        ModEntry.MonitorObject.LogOnce(
          $"{GetType().Name}: Both our mod options tab and the game are displaying hover text",
          LogLevel.Warn
        );
      }
    }
  }

  private void OnWindowClientSizeChanged(object? sender, EventArgs e)
  {
    _windowResizing = true;
    GameRunner.instance.ExecuteForInstances(
      instance =>
      {
        if (Game1.activeClickableMenu is GameMenu gameMenu &&
            gameMenu.currentTab == _modOptionsTabPageNumber.GetValueForScreen(instance.instanceId))
        {
          // Swap to game's options tab — GameMenu is recreated before we can re-add our page
          if (gameMenu.GetCurrentPage() is ModOptionsPage modOptionsPage)
          {
            _savedPageState.Value = new ModOptionsPageState();
            modOptionsPage.SaveState(_savedPageState.Value);
          }

          gameMenu.currentTab = GameMenu.optionsTab;
          _instancesWithOptionsPageOpen.Add(instance.instanceId);
        }
      }
    );
    if (_instancesWithOptionsPageOpen.Count > 0)
    {
      ModEntry.MonitorObject.Log(
        $"{GetType().Name}: The window is being resized while our options page is opened, applying workaround"
      );
    }
  }

  // Called between frames (after Display.Rendered, before Update.Ticking)
  private void OnWindowResized(object? sender, EventArgs e)
  {
    if (_windowResizing)
    {
      _windowResizing = false;
      if (_instancesWithOptionsPageOpen.Count > 0)
      {
        GameRunner.instance.ExecuteForInstances(
          instance =>
          {
            if (_instancesWithOptionsPageOpen.Remove(instance.instanceId))
            {
              if (Game1.activeClickableMenu is GameMenu gameMenu)
              {
                gameMenu.currentTab = (int)_modOptionsTabPageNumber.GetValueForScreen(instance.instanceId)!;
              }
            }
          }
        );

        ModEntry.MonitorObject.Log($"{GetType().Name}: The window was resized, reverting to our tab");
        _addOurTabBeforeTick = true;
      }
    }
  }

  /// <summary>Based on <see cref="GameMenu.changeTab" /></summary>
  private void ChangeToOurTab(GameMenu gameMenu)
  {
    var modOptionsTabIndex = (int)_modOptionsTabPageNumber.Value!;
    gameMenu.currentTab = modOptionsTabIndex;
    gameMenu.lastOpenedNonMapTab = modOptionsTabIndex;
    gameMenu.initializeUpperRightCloseButton();
    gameMenu.invisible = false;
    Game1.playSound("smallSelect");

    // populateClickableComponentList handles AddTabsToClickableComponents; we just add our tab for snap support
    gameMenu.GetCurrentPage().populateClickableComponentList();
    AddOurTabToClickableComponents(gameMenu, _modOptionsTab.Value!);

    gameMenu.setTabNeighborsForCurrentPage();
    if (Game1.options.SnappyMenus)
    {
      gameMenu.snapToDefaultClickableComponent();
    }
  }

  /// <summary>Add our tab to the page's clickable components (initializes list if needed, skips duplicates).</summary>
  private void AddOurTabToClickableComponents(GameMenu gameMenu, ClickableComponent modOptionsTab)
  {
    IClickableMenu currentPage = gameMenu.GetCurrentPage()!;
    if (currentPage.allClickableComponents == null)
    {
      currentPage.populateClickableComponentList();
    }

    if (!currentPage.allClickableComponents!.Contains(modOptionsTab))
    {
      currentPage.allClickableComponents.Add(modOptionsTab);
    }
  }

  private int GetButtonXPosition(GameMenu gameMenu)
  {
    return gameMenu.xPositionOnScreen + gameMenu.width - 165;
  }

  private void DrawButton(GameMenu gameMenu)
  {
    ModOptionsPageButton? button = _modOptionsPageButton.Value;

    if (button == null || _modOptionsTabPageNumber.Value == null)
    {
      return;
    }

    button.yPositionOnScreen = gameMenu.yPositionOnScreen +
                               (gameMenu.currentTab == _modOptionsTabPageNumber.Value ? 24 : 16);

    button.draw(Game1.spriteBatch);
  }

  #endregion

  /// <summary>Returns version string from SMAPI manifest, assembly, or "(unknown version)".</summary>
  private static string GetVersionString(IModHelper helper)
  {
    IModInfo? modInfo = helper.ModRegistry.Get(helper.ModRegistry.ModID);
    if (modInfo != null)
    {
      return $"v{modInfo.Manifest.Version}";
    }

    ModEntry.MonitorObject.LogOnce(
      $"{typeof(ModOptionsPageHandler).Name}: Couldn't retrieve our own mod information",
      LogLevel.Info
    );

    Version? assemblyVersion = Assembly.GetAssembly(typeof(ModEntry))?.GetName().Version;
    if (assemblyVersion != null)
    {
      return $"v={assemblyVersion}";
    }

    ModEntry.MonitorObject.LogOnce(
      $"{typeof(ModOptionsPageHandler).Name}: Couldn't retrieve our own assembly version information"
    );

    return "(unknown version)";
  }
}

/// <summary>Saved/restored state across game menu resizes.</summary>
internal class ModOptionsPageState
{
  public int? currentComponent;
  public int? currentIndex;
}
