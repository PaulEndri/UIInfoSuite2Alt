using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace UIInfoSuite2Alt.Options;

public class ModConfig
{
  // --- Global settings ---
  public bool ShowOptionsTabInMenu { get; set; } = true;
  public KeybindList OpenCalendarKeybind { get; set; } = KeybindList.ForSingle(SButton.B);
  public KeybindList OpenQuestBoardKeybind { get; set; } = KeybindList.ForSingle(SButton.H);
  public KeybindList ShowOneRange { get; set; } = KeybindList.ForSingle(SButton.LeftControl);
  public KeybindList ShowAllRange { get; set; } = KeybindList.Parse("LeftControl + LeftAlt");
  public KeybindList OpenModOptionsKeybind { get; set; } = KeybindList.ForSingle(SButton.F8);
  public KeybindList OpenMonsterEradicationKeybind { get; set; } = KeybindList.ForSingle(SButton.F9);

  // --- Feature toggles (migrated from per-save ModOptions) ---
  public bool AllowExperienceBarToFadeOut { get; set; } = true;
  public bool ShowExperienceBar { get; set; } = true;
  public bool ShowExperienceGain { get; set; } = true;
  public bool ShowLevelUpAnimation { get; set; } = true;
  public bool ShowHeartFills { get; set; } = true;
  public bool ShowExtraItemInformation { get; set; } = true;
  public bool ShowLocationOfTownsPeople { get; set; } = true;
  public bool ShowLuckIcon { get; set; } = true;
  public bool ShowTravelingMerchant { get; set; } = true;
  public bool ShowBookseller { get; set; } = true;
  public bool ShowRainyDay { get; set; } = true;
  public bool ShowCropTooltip { get; set; } = true;
  public bool ShowTreeTooltip { get; set; } = true;
  public bool ShowBarrelTooltip { get; set; } = true;
  public bool ShowFishPondTooltip { get; set; } = true;
  public int MachineProcessingIconsMode { get; set; } = 1;
  public bool MachineProcessingIconsVisible { get; set; } = true;
  public KeybindList ToggleMachineProcessingIcons { get; set; } = KeybindList.ForSingle(SButton.F10);
  public bool ShowFishPondIcons { get; set; } = false;
  public bool ShowBirthdayIcon { get; set; } = true;
  public bool ShowAnimalsNeedPets { get; set; } = true;
  public bool HideAnimalPetOnMaxFriendship { get; set; } = true;
  public bool ShowItemEffectRanges { get; set; } = true;
  public bool ButtonControlShow { get; set; } = false;
  public bool ShowBombRange { get; set; } = false;
  public bool ShowHarvestPricesInShop { get; set; } = true;
  public bool DisplayCalendarAndBillboard { get; set; } = true;
  public bool ShowWhenNewRecipesAreAvailable { get; set; } = true;
  public bool ShowRecipeItemIcon { get; set; } = true;
  public bool ShowToolUpgradeStatus { get; set; } = true;
  public bool HideMerchantWhenVisited { get; set; } = false;
  public bool ShowMerchantBundleIcon { get; set; } = false;
  public bool ShowMerchantBundleItemNames { get; set; } = false;
  public bool HideBooksellerWhenVisited { get; set; } = false;
  public int LuckIconStyle { get; set; } = 0;
  public bool ShowExactValue { get; set; } = false;
  public bool RequireTvForLuck { get; set; } = false;
  public bool RequireTvForWeather { get; set; } = false;
  public bool ShowRobinBuildingStatusIcon { get; set; } = true;
  public bool ShowSeasonalBerry { get; set; } = true;
  public bool ShowSeasonalBerryHazelnut { get; set; } = false;
  public bool ShowTodaysGifts { get; set; } = true;
  public bool HideBirthdayIfFullFriendShip { get; set; } = true;
  public bool ShowQuestCount { get; set; } = true;
  public bool UseVerticalIconLayout { get; set; } = false;
  public int IconsPerRow { get; set; } = 10;
  public bool ShowFestivalIcon { get; set; } = true;
  public bool ShowFishOnCatch { get; set; } = false;
  public bool ShowFishQualityStar { get; set; } = true;
  public bool ShowBuffTimers { get; set; } = true;
  public bool PlayBuffExpireSound { get; set; } = true;
  public bool ShowCustomIcons { get; set; } = true;
  public Dictionary<string, bool> ShowLocationOfFriends { get; set; } = new();
  public Dictionary<string, int> IconOrder { get; set; } = new()
  {
    { "Luck", 1 },
    { "Weather", 2 },
    { "Birthday", 3 },
    { "Festival", 4 },
    { "QueenOfSauce", 5 },
    { "ToolUpgrade", 6 },
    { "RobinBuilding", 7 },
    { "SeasonalBerry", 8 },
    { "TravelingMerchant", 9 },
    { "Bookseller", 10 },
    { "CustomIcons", 11 }
  };
}
