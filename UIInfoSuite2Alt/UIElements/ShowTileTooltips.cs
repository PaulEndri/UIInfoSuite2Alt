using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.FishPonds;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using UIInfoSuite2Alt.Compatibility;
using UIInfoSuite2Alt.Compatibility.CustomBush;
using UIInfoSuite2Alt.Infrastructure;
using UIInfoSuite2Alt.Infrastructure.Extensions;
using UIInfoSuite2Alt.Infrastructure.Helpers;
using Object = StardewValley.Object;

namespace UIInfoSuite2Alt.UIElements;

internal readonly struct HoverSegment
{
  public string Text { get; }
  public Color? Color { get; }

  public HoverSegment(string text, Color? color = null)
  {
    Text = text;
    Color = color;
  }

  public static implicit operator HoverSegment(string text) => new(text);
}

internal readonly struct HoverLine
{
  public IReadOnlyList<HoverSegment> Segments { get; }

  public HoverLine(string text, Color? color = null)
  {
    Segments = new[] { new HoverSegment(text, color) };
  }

  public HoverLine(params HoverSegment[] segments)
  {
    Segments = segments;
  }

  public static implicit operator HoverLine(string text) => new(text);
}

internal class ShowTileTooltips : IDisposable
{
  private const int MAX_TREE_GROWTH_STAGE = 5;

  // Colors for the different tooltip text
  private static readonly Color ReadyColor = Tools.TooltipGreen;
  private static readonly Color WaitingColor = Tools.TooltipYellow;
  private static readonly Color WateredColor = Tools.TooltipBlue;
  private static readonly Color NotWateredColor = Tools.TooltipRed;

  private static readonly List<Func<Building?, List<HoverLine>, bool>> BuildingDetailRenderers = new()
  {
    DetailRenderers.BuildingOutput
  };

  private static readonly List<Func<Object?, List<HoverLine>, bool>> MachineDetailRenderers = new()
  {
    DetailRenderers.MachineTime
  };

  private static readonly List<Func<TerrainFeature?, List<HoverLine>, bool>> CropDetailRenderers = new()
  {
    DetailRenderers.CropRender
  };

  private static readonly List<Func<TerrainFeature?, List<HoverLine>, bool>> TreeDetailRenderers = new()
  {
    DetailRenderers.TreeRender, DetailRenderers.FruitTreeRender, DetailRenderers.TeaBush
  };

  private readonly PerScreen<TerrainFeature?> _currentTerrain = new();
  private readonly PerScreen<Object?> _currentTile = new();
  private readonly PerScreen<Building?> _currentTileBuilding = new();

  private readonly IModHelper _helper;
  private readonly ShowItemEffectRanges _itemEffectRanges;
  private bool _showCropTooltip;
  private bool _showTreeTooltip;
  private bool _showBarrelTooltip;
  private bool _showFishPondTooltip;

  public ShowTileTooltips(IModHelper helper, ShowItemEffectRanges itemEffectRanges)
  {
    _helper = helper;
    _itemEffectRanges = itemEffectRanges;
  }

  public void Dispose()
  {
    ToggleCropOption(false);
    ToggleTreeOption(false);
    ToggleBarrelOption(false);
    ToggleFishPondOption(false);
  }

  public void ToggleCropOption(bool showCropTooltip)
  {
    _showCropTooltip = showCropTooltip;
    UpdateEventSubscriptions();
  }

  public void ToggleTreeOption(bool showTreeTooltip)
  {
    _showTreeTooltip = showTreeTooltip;
    UpdateEventSubscriptions();
  }

  public void ToggleBarrelOption(bool showBarrelTooltip)
  {
    _showBarrelTooltip = showBarrelTooltip;
    UpdateEventSubscriptions();
  }

  public void ToggleFishPondOption(bool showFishPondTooltip)
  {
    _showFishPondTooltip = showFishPondTooltip;
    UpdateEventSubscriptions();
  }

  private void UpdateEventSubscriptions()
  {
    _helper.Events.Display.RenderingHud -= OnRenderingHud;
    _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

    if (!_showCropTooltip && !_showTreeTooltip && !_showBarrelTooltip && !_showFishPondTooltip)
    {
      return;
    }

    _helper.Events.Display.RenderingHud += OnRenderingHud;
    _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
  }

  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (!e.IsMultipleOf(4))
    {
      return;
    }

    _currentTileBuilding.Value = null;
    _currentTile.Value = null;
    _currentTerrain.Value = null;

    Vector2 gamepadTile = Game1.player.CurrentTool != null
      ? Utility.snapToInt(Game1.player.GetToolLocation() / Game1.tileSize)
      : Utility.snapToInt(Game1.player.GetGrabTile());
    Vector2 mouseTile = Game1.currentCursorTile;

    Vector2 tile = Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0 ? gamepadTile : mouseTile;

    if (Game1.currentLocation == null)
    {
      return;
    }

    if (Game1.currentLocation.IsBuildableLocation())
    {
      _currentTileBuilding.Value = Game1.currentLocation.getBuildingAt(tile);
    }

    if (Game1.currentLocation.Objects?.TryGetValue(tile, out Object? currentObject) ?? false)
    {
      _currentTile.Value = currentObject;
    }

    if (Game1.currentLocation.terrainFeatures?.TryGetValue(tile, out TerrainFeature? terrain) ?? false)
    {
      _currentTerrain.Value = terrain;
    }

    if (_currentTile.Value is IndoorPot pot)
    {
      if (pot.hoeDirt.Value != null)
      {
        _currentTerrain.Value = pot.hoeDirt.Value;
      }

      if (pot.bush.Value != null)
      {
        _currentTerrain.Value = pot.bush.Value;
      }
    }
  }

  private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
  {
    if (!UIElementUtils.IsRenderingNormally() || Game1.activeClickableMenu != null)
    {
      return;
    }

    List<HoverLine> lines = new();
    Vector2 tile = Vector2.Zero;
    Building? currentTileBuilding = _currentTileBuilding.Value;
    Object? currentTile = _currentTile.Value;
    TerrainFeature? terrain = _currentTerrain.Value;

    int overrideX = -1;
    int overrideY = -1;

    if (_showBarrelTooltip && currentTileBuilding is not null)
    {
      foreach (Func<Building?, List<HoverLine>, bool> buildingDetailRenderer in BuildingDetailRenderers)
      {
        if (!buildingDetailRenderer(currentTileBuilding, lines))
        {
          continue;
        }

        Vector2 buildingTile = new(currentTileBuilding.tileX.Value, currentTileBuilding.tileY.Value);
        tile = Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(buildingTile * Game1.tileSize));
      }
    }

    if (_showFishPondTooltip && currentTileBuilding is FishPond fishPond)
    {
      if (DetailRenderers.FishPondRender(fishPond, lines))
      {
        Vector2 buildingTile = new(fishPond.tileX.Value, fishPond.tileY.Value);
        tile = Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(buildingTile * Game1.tileSize));
      }
    }

    if (_showBarrelTooltip && currentTile is not null && !_itemEffectRanges.IsRangeTooltipActive)
    {
      foreach (Func<Object?, List<HoverLine>, bool> machineDetailRenderer in MachineDetailRenderers)
      {
        if (machineDetailRenderer(currentTile, lines))
        {
          tile = Utility.ModifyCoordinatesForUIScale(
            Game1.GlobalToLocal(new Vector2(currentTile.TileLocation.X, currentTile.TileLocation.Y) * Game1.tileSize)
          );
        }
      }
    }

    if (_showCropTooltip && terrain is not null)
    {
      foreach (Func<TerrainFeature?, List<HoverLine>, bool> cropDetailRenderer in CropDetailRenderers)
      {
        if (cropDetailRenderer(terrain, lines))
        {
          tile = Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(terrain.Tile * Game1.tileSize));
        }
      }
    }

    if (_showTreeTooltip && terrain is not null && !_itemEffectRanges.IsRangeTooltipActive)
    {
      foreach (Func<TerrainFeature?, List<HoverLine>, bool> treeDetailRenderer in TreeDetailRenderers)
      {
        if (treeDetailRenderer(terrain, lines))
        {
          tile = Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(terrain.Tile * Game1.tileSize));
        }
      }
    }

    if (lines.Count <= 0)
    {
      return;
    }

    if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
    {
      overrideX = (int)(tile.X + Utility.ModifyCoordinateForUIScale(32));
      overrideY = (int)(tile.Y + Utility.ModifyCoordinateForUIScale(32));
    }

    DrawColoredHoverText(Game1.spriteBatch, lines, Game1.smallFont, overrideX, overrideY);
  }

  private static void DrawColoredHoverText(
    SpriteBatch b, List<HoverLine> lines, SpriteFont font, int overrideX = -1, int overrideY = -1)
  {
    float maxWidth = 0;
    foreach (HoverLine line in lines)
    {
      float lineWidth = 0;
      foreach (HoverSegment segment in line.Segments)
      {
        if (segment.Text.Length > 0)
        {
          lineWidth += font.MeasureString(segment.Text).X;
        }
      }

      maxWidth = Math.Max(maxWidth, lineWidth);
    }

    int width = (int)maxWidth + 32;
    int height = Math.Max(60, lines.Count * font.LineSpacing + 32);

    int x = Game1.getOldMouseX() + 32;
    int y = Game1.getOldMouseY() + 32;

    if (overrideX != -1)
    {
      x = overrideX;
    }

    if (overrideY != -1)
    {
      y = overrideY;
    }

    Rectangle safeArea = Utility.getSafeArea();
    if (x + width > safeArea.Right)
    {
      x = safeArea.Right - width;
      y += 16;
    }

    if (y + height > safeArea.Bottom)
    {
      x += 16;
      if (x + width > safeArea.Right)
      {
        x = safeArea.Right - width;
      }

      y = safeArea.Bottom - height;
    }

    width += 4;

    IClickableMenu.drawTextureBox(
      b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
      x, y, width, height, Color.White);

    Color defaultColor = Game1.textColor;
    Color shadowColor = Game1.textShadowColor;
    float lineY = y + 16 + 4;

    foreach (HoverLine line in lines)
    {
      float segX = x + 16;
      foreach (HoverSegment segment in line.Segments)
      {
        if (segment.Text.Length > 0)
        {
          Color segColor = segment.Color ?? defaultColor;
          Vector2 pos = new(segX, lineY);
          b.DrawString(font, segment.Text, pos + new Vector2(2f, 2f), shadowColor);
          b.DrawString(font, segment.Text, pos + new Vector2(0f, 2f), shadowColor);
          b.DrawString(font, segment.Text, pos + new Vector2(2f, 0f), shadowColor);
          b.DrawString(font, segment.Text, pos, segColor * 0.9f);
          segX += font.MeasureString(segment.Text).X;
        }
      }

      lineY += font.LineSpacing;
    }
  }

  private static IEnumerable<string> GetFertilizerList(HoeDirt dirtTile)
  {
    if (string.IsNullOrWhiteSpace(dirtTile.fertilizer.Value))
      return Enumerable.Empty<string>();

    var fertilizerNames = new Dictionary<string, int>();

    // Supports Ultimate Fertilizer's pipe-delimited format
    foreach (string fertilizerStr in dirtTile.fertilizer.Value.Split('|'))
    {
      string name = ItemRegistry.GetData(fertilizerStr)?.DisplayName ?? "Unknown Fertilizer";
      int count = fertilizerNames.GetValueOrDefault(name, 0);
      fertilizerNames[name] = count + 1;
    }

    return fertilizerNames.OrderBy(kv => kv.Value)
                          .ThenBy(kv => kv.Key)
                          .Select(kv =>
                          {
                            string quantityStr = kv.Value == 1 ? "" : $" x{kv.Value}";
                            return $"{kv.Key}{quantityStr}";
                          });
  }

  // See: stardewvalleywiki.com/Trees
  internal static string GetTreeTypeName(string treeType)
  {
    switch (treeType)
    {
      case "1":
        return I18n.Oak();
      case "2":
        return I18n.Maple();
      case "3":
        return I18n.Pine();
      case "6":
        return I18n.Palm();
      case "7":
        return I18n.Mushroom();
      case "8":
        return I18n.Mahogany();
      case "9":
        return I18n.PalmJungle();
      case "10":
        return I18n.GreenRainType1();
      case "11":
        return I18n.GreenRainType2();
      case "12":
        return I18n.GreenRainType3();
      case "13":
        return I18n.Mystic();
      case "Lumisteria.MtVapius.Birchtree":
        return I18n.VmvBirch();
      case "Lumisteria.MtVapius.HazelnutTree":
        return I18n.VmvHazelnut();
      case "Lumisteria.MtVapius.SkyshardPineTree":
        return I18n.VmvSkyshardPine();
      case "Lumisteria.MtVapius.AmberTree":
        return I18n.VmvAmber();
      case "Lumisteria.MtVapius.BlackChanterelleTree":
        return I18n.VmvBlackChanterelle();
      case "FlashShifter.StardewValleyExpandedCP_Birch_Tree":
        return I18n.SVEBirch();
      case "FlashShifter.StardewValleyExpandedCP_Fir_Tree":
        return I18n.SVEFir();
      case "Cornucopia_SapodillaSeed":
        return I18n.CORSapodilla();
      case "Cornucopia_CorpseFlowerSeed":
        return I18n.CORCorpseFlower();
      case "Cornucopia_DatePalmSeed":
        return I18n.CORDatePalm();
      case "skellady.SBVCP.CinderTree":
        return I18n.SbvCinder();
      case "Wildflour.SASS_Stout_Funnel_Tree":
        return I18n.SbvStoutFunnel();
      case "Wildflour.SASS_Sparkling_Agaric_Tree":
        return I18n.SbvSparklingAgaric();
      case "Wildflour.SASS_Seafoam_Waxcap_Tree":
        return I18n.SbvSeafoamWaxcap();
      case "Wildflour.SASS_Lunar_Poof_Tree":
        return I18n.SbvLunarPoof();
      case "Wildflour.SASS_Indigo_Cap_Tree":
        return I18n.SbvIndigoCap();
      case "Wildflour.SASS_Lilac_Funnel_Tree":
        return I18n.SbvLilacFunnel();
      case "Wildflour.SASS_Limey_Bonnet_Tree":
        return I18n.SbvLimeyBonnet();
      case "Wildflour.SASS_Coral_Fungus_Tree":
        return I18n.SbvCoralFungus();
      case "Wildflour.SASS_Ghostly_Parasol_Tree":
        return I18n.SbvGhostlyParasol();
      case "Wildflour.SASS_Frilly_Gilly_Tree":
        return I18n.SbvFrillyGilly();
      default:
        ModEntry.MonitorObject.LogOnce($"Unknown tree type: {treeType} (Post a Bug Report on NexusMods)", LogLevel.Warn);
        return $"Unknown (#{treeType})";
    }
  }

  private static class DetailRenderers
  {
    private static HoverLine GetInfoStringForDrop(PossibleDroppedItem item)
    {
      (int nextDayToProduce, ParsedItemData? parsedItemData, float chance, string? _) = item;

      string chanceStr = 1.0f.Equals(chance) ? "" : $" ({chance * 100:2F}%)";
      int daysUntilReady = nextDayToProduce - Game1.dayOfMonth;
      return daysUntilReady <= 0
        ? new HoverLine($"{parsedItemData.DisplayName}: ", new HoverSegment(I18n.ReadyToHarvest(), ReadyColor))
        : new HoverLine($"{parsedItemData.DisplayName}: ", new HoverSegment($"{daysUntilReady} {I18n.Days()}{chanceStr}", WaitingColor));
    }

    private static Dictionary<string, int> GetItemCountMap(List<Item?> items)
    {
      Dictionary<string, int> itemCounter = new();
      foreach (Item? outputItem in items)
      {
        if (outputItem is null)
        {
          continue;
        }

        int count = itemCounter.GetOrDefault(outputItem.DisplayName, 0) + outputItem.Stack;
        itemCounter[outputItem.DisplayName] = count;
      }

      return itemCounter;
    }

    public static bool FishPondRender(FishPond fishPond, List<HoverLine> entries)
    {
      if (fishPond.fishType.Value == null || fishPond.currentOccupants.Value <= 0)
      {
        return false;
      }

      // Fish name
      string fishName = fishPond.GetFishObject().DisplayName;
      entries.Add(fishName);

      // Population: current/max
      int current = fishPond.currentOccupants.Value;
      int max = fishPond.maxOccupants.Value;
      Color populationColor = current >= max ? ReadyColor : WaitingColor;
      entries.Add(new HoverLine(I18n.FishPondPopulation(current, max: max), populationColor));

      // Quest item needed
      if (fishPond.neededItem.Value != null && fishPond.HasUnresolvedNeeds())
      {
        string itemName = fishPond.neededItem.Value.DisplayName;
        int itemCount = fishPond.neededItemCount.Value;
        entries.Add(new HoverLine(I18n.FishPondQuestItem(itemName, count: itemCount), WaitingColor));
      }

      // Next spawn / quest timing
      FishPondData? pondData = fishPond.GetFishPondData();
      if (pondData != null)
      {
        int daysUntilSpawn = pondData.SpawnTime - fishPond.daysSinceSpawn.Value;

        if (current < max && !fishPond.hasSpawnedFish.Value && daysUntilSpawn > 0)
        {
          // Not at max — show days until next fish spawns
          entries.Add(new HoverLine(I18n.FishPondNextSpawn(daysUntilSpawn), WaitingColor));
        }
        else if (current >= max && fishPond.neededItem.Value == null && daysUntilSpawn > 0
                 && pondData.PopulationGates != null
                 && pondData.PopulationGates.ContainsKey(max + 1))
        {
          // At max, no quest yet, but a gate exists — show days until quest appears
          entries.Add(new HoverLine(I18n.FishPondNextQuest(daysUntilSpawn), WaitingColor));
        }
      }

      // Golden Animal Cracker
      if (fishPond.goldenAnimalCracker.Value)
      {
        entries.Add(new HoverLine(I18n.FishPondGoldenCracker(), WaitingColor));
      }

      return true;
    }

    public static bool BuildingOutput(Building? building, List<HoverLine> entries)
    {
      if (building is null)
      {
        return false;
      }

      List<Item?> inputItems = new();
      List<Item?> outputItems = new();
      MachineHelper.GetBuildingChestItems(building, inputItems, outputItems);

      Dictionary<string, int> inputItemsMap = GetItemCountMap(inputItems);
      Dictionary<string, int> outputItemsMap = GetItemCountMap(outputItems);

      if (inputItemsMap.Count > 0)
      {
        entries.Add($"{I18n.MachineProcessing()}:");
        foreach ((string displayName, int count) in inputItemsMap)
        {
          entries.Add($"{displayName} x{count}");
        }
      }

      if (outputItemsMap.Count <= 0)
      {
        return true;
      }

      if (inputItemsMap.Count > 0)
      {
        entries.Add("");
      }

      entries.Add($"{I18n.MachineDone()}:");
      foreach ((string displayName, int count) in outputItemsMap)
      {
        entries.Add($"{displayName} x{count}");
      }


      return true;
    }

    public static bool MachineTime(Object? tileObject, List<HoverLine> entries)
    {
      if (tileObject == null ||
          !tileObject.bigCraftable.Value ||
          tileObject.MinutesUntilReady <= 0 ||
          tileObject.heldObject.Value == null ||
          tileObject.Name == "Heater")
      {
        return false;
      }

      entries.Add(tileObject.heldObject.Value.DisplayName);
      if (tileObject is Cask cask)
      {
        entries.Add($"{(int)Math.Ceiling(cask.daysToMature.Value / cask.agingRate.Value)} {I18n.DaysToMature()}");
        return true;
      }

      int timeLeft = tileObject.MinutesUntilReady;
      int longTime = timeLeft / 60;
      string longText = I18n.Hours();
      int shortTime = timeLeft % 60;
      string shortText = I18n.Minutes();

      // ~1600 minutes per day — approximate since overnight time varies
      if (timeLeft >= 1600)
      {
        longText = I18n.Days();
        longTime = timeLeft / 1600;

        shortText = I18n.Hours();
        shortTime = timeLeft % 1600;

        // Fudged: 60min/hr daytime, 100min/hr overnight — prevents "25 hours" display
        if (shortTime <= 1200)
        {
          shortTime /= 60;
        }
        else
        {
          shortTime = 20 + (shortTime - 1200) / 100;
        }
      }

      StringBuilder builder = new();

      if (longTime > 0)
      {
        builder.Append($"{longTime} {longText}, ");
      }

      builder.Append($"{shortTime} {shortText}");
      entries.Add(builder.ToString());
      return true;
    }

    public static bool CropRender(TerrainFeature? terrain, List<HoverLine> entries)
    {
      if (terrain is not HoeDirt hoeDirt)
      {
        return false;
      }

      IEnumerable<string> fertilizers = Enumerable.Empty<string>();

      if (!string.IsNullOrEmpty(hoeDirt.fertilizer.Value) && !"0".Equals(hoeDirt.fertilizer.Value))
      {
        fertilizers = GetFertilizerList(hoeDirt);
      }

      if (hoeDirt.crop is not null && !hoeDirt.crop.dead.Value)
      {
        Crop crop = hoeDirt.crop;
        var daysLeft = 0;

        if (hoeDirt.crop.fullyGrown.Value)
        {
          daysLeft = Math.Max(0, hoeDirt.crop.dayOfCurrentPhase.Value);
        }
        else
        {
          for (int i = hoeDirt.crop.currentPhase.Value; i < hoeDirt.crop.phaseDays.Count - 1; i++)
          {
            daysLeft += hoeDirt.crop.phaseDays[i];
          }
          daysLeft -= hoeDirt.crop.dayOfCurrentPhase.Value;
        }

        string cropName = DropsHelper.GetCropHarvestName(crop);
        string daysLeftStr = daysLeft <= 0 ? I18n.ReadyToHarvest() : $"{daysLeft} {I18n.Days()}";
        Color cropColor = daysLeft <= 0 ? ReadyColor : WaitingColor;
        entries.Add(new HoverLine($"{cropName}: ", new HoverSegment(daysLeftStr, cropColor)));

        bool isWatered = hoeDirt.state.Value == 1;
        string waterStatus = isWatered ? I18n.Watered() : I18n.NotWatered();
        Color waterColor = isWatered ? WateredColor : NotWateredColor;
        entries.Add(new HoverLine(waterStatus, waterColor));
      }

      if (fertilizers.Any())
      {
        var fertList = fertilizers.ToList();

        for (int i = 0; i < fertList.Count; i++)
        {
          string currentName = fertList[i];
          string lineText;

          if (fertList.Count == 1)
          {
            lineText = $"({I18n.With()} {currentName})";
          }
          else if (i == 0)
          {
            lineText = $"({I18n.With()} {currentName},";
          }
          else if (i == fertList.Count - 1)
          {
            lineText = $"{currentName})";
          }
          else
          {
            lineText = $"{currentName},";
          }
          entries.Add(new HoverLine(lineText));
        }
      }

      return true;
    }

    public static bool TreeRender(TerrainFeature? terrain, List<HoverLine> entries)
    {
      if (terrain is not Tree tree)
      {
        return false;
      }

      bool isStump = tree.stump.Value;
      string treeTypeName = GetTreeTypeName(tree.treeType.Value);
      string stumpText = isStump ? $" ({I18n.Stump()})" : "";
      entries.Add($"{treeTypeName}{I18n.Tree()}{stumpText}");

      if (tree.growthStage.Value >= MAX_TREE_GROWTH_STAGE)
      {
        return true;
      }

      entries.Add($"{I18n.Stage()} {tree.growthStage.Value} / {MAX_TREE_GROWTH_STAGE}");
      if (tree.fertilized.Value)
      {
        entries.Add($"({I18n.Fertilized()})");
      }

      return true;
    }

    public static bool FruitTreeRender(TerrainFeature? terrain, List<HoverLine> entries)
    {
      if (terrain is not FruitTree fruitTree)
      {
        return false;
      }

      FruitTreeInfo treeInfo = DropsHelper.GetFruitTreeInfo(fruitTree);
      entries.Add(treeInfo.TreeName);
      if (fruitTree.daysUntilMature.Value > 0)
      {
        entries.Add($"{fruitTree.daysUntilMature.Value} {I18n.DaysToMature()}");
        return true;
      }

      if (treeInfo.Items.Count <= 1)
      {
        return true;
      }

      entries.AddRange(treeInfo.Items.Select(GetInfoStringForDrop));
      return true;
    }

    public static bool TeaBush(TerrainFeature? terrain, List<HoverLine> entries)
    {
      if (terrain is not Bush bush || bush.size.Value != Bush.greenTeaBush)
      {
        return false;
      }

      var ageToMature = 20;
      bool willProduceThisSeason = Game1.season != Season.Winter || bush.IsSheltered();
      string bushName = ItemRegistry.GetData("(O)251").DisplayName;
      bool inProductionPeriod = Game1.dayOfMonth >= 22;
      int daysUntilProductionPeriod = inProductionPeriod ? 0 : 22 - Game1.dayOfMonth;
      List<PossibleDroppedItem> droppedItems = new();

      if (bush.tileSheetOffset.Value == 1)
      {
        droppedItems.Add(new PossibleDroppedItem(Game1.dayOfMonth, ItemRegistry.GetData("(O)815"), 1.0f));
      }
      else if (Game1.dayOfMonth >= 21 && Game1.dayOfMonth < 28)
      {
        droppedItems.Add(new PossibleDroppedItem(Game1.dayOfMonth + 1, ItemRegistry.GetData("(O)815"), 1.0f));
      }

      if (ApiManager.GetApi(ModCompat.CustomBush, out ICustomBushApi? customBushApi))
      {
        if (customBushApi.TryGetCustomBush(bush, out ICustomBush? customBushData, out string? id))
        {
          droppedItems.Clear();
          willProduceThisSeason = customBushData.Seasons.Contains(Game1.season) || bush.IsSheltered();
          string displayName = customBushData.DisplayName;
          if (displayName.Contains("LocalizedText"))
          {
            displayName = TokenParser.ParseText(displayName);
          }

          ageToMature = customBushData.AgeToProduce;
          inProductionPeriod = Game1.dayOfMonth >= customBushData.DayToBeginProducing;
          daysUntilProductionPeriod = inProductionPeriod ? 0 : 22 - Game1.dayOfMonth;

          if (customBushData.GetShakeOffItemIfReady(bush, out ParsedItemData? shakeOffItemData))
          {
            droppedItems.Add(new PossibleDroppedItem(Game1.dayOfMonth, shakeOffItemData, 1.0f, id));
          }
          else
          {
            droppedItems = customBushApi.GetCustomBushDropItems(customBushData, id);
          }

          // Single-drop bush: use item name as bush name
          if (droppedItems.Count == 1)
          {
            string suffix = bush.getAge() >= ageToMature ? "Bush" : "Sapling";
            bushName = $"{droppedItems[0].Item.DisplayName} {suffix}";
          }
          else
          {
            bushName = displayName;
          }
        }
      }

      entries.Add(bushName);
      bool isMature = bush.getAge() >= ageToMature;
      if (!isMature || !willProduceThisSeason)
      {
        if (!isMature)
        {
          entries.Add($"{ageToMature - bush.getAge()} {I18n.DaysToMature()}");
        }

        if (!willProduceThisSeason)
        {
          entries.Add(I18n.DoesNotProduceThisSeason());
        }

        return true;
      }

      // Not yet in production period
      if (!inProductionPeriod)
      {
        entries.Add($"{daysUntilProductionPeriod} {I18n.Days()}");
        return true;
      }

      entries.AddRange(droppedItems.Select(GetInfoStringForDrop));
      return true;
    }
  }
}
