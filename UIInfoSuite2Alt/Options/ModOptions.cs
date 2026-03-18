using System.Collections.Generic;

namespace UIInfoSuite2Alt.Options;

internal record ModOptions
{
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
  public bool ShowFestivalIcon { get; set; } = true;
  public bool ShowFishOnCatch { get; set; } = false;
  public bool ShowFishQualityStar { get; set; } = true;
  public bool ShowBuffTimers { get; set; } = true;
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
    { "Bookseller", 10 }
  };
}
