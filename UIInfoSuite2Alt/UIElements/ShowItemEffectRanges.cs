using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Network;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using UIInfoSuite2Alt.Infrastructure;
using Object = StardewValley.Object;

namespace UIInfoSuite2Alt.UIElements;

internal class ShowItemEffectRanges : IDisposable
{
  #region Properties
  private readonly PerScreen<List<Point>> _effectiveAreaCurrent = new(() => new List<Point>());
  private readonly PerScreen<HashSet<Point>> _effectiveAreaOther = new(() => new HashSet<Point>());
  private readonly PerScreen<HashSet<Point>> _effectiveAreaIntersection = new(() => new HashSet<Point>());
  private readonly PerScreen<HashSet<Point>> _seenTiles = new(() => new HashSet<Point>());

  private readonly IModHelper _helper;

  private bool _showItemEffectRanges;

  private bool ButtonControlShow { get; set; }
  private bool ShowRangeTooltip { get; set; } = true;
  private bool ShowBombRange { get; set; }

  private bool ButtonShowOneRange { get; set; }
  private bool ButtonShowAllRanges { get; set; }

  private readonly PerScreen<RangeTooltipInfo?> _rangeTooltipInfo = new(() => null);

  private sealed class RangeTooltipInfo
  {
    public string ObjectName = "";
    public bool TrackOverlap;
    public int ObjectCount;
    public bool ShowingAll;
    public int OccupiedTiles;
    public int RawTotalTiles;
  }
  #endregion


  #region Lifecycle
  public ShowItemEffectRanges(IModHelper helper)
  {
    _helper = helper;
  }

  public void Dispose()
  {
    ToggleOption(false);
    ToggleShowBombRangeOption(false);
  }

  public void ToggleOption(bool showItemEffectRanges)
  {
    _showItemEffectRanges = showItemEffectRanges;
    UpdateEventSubscriptions();
  }

  public void ToggleButtonControlShowOption(bool buttonControlShow)
  {
    ButtonControlShow = buttonControlShow;

    _helper.Events.Input.ButtonsChanged -= OnButtonChanged;
    if (buttonControlShow)
    {
      _helper.Events.Input.ButtonsChanged += OnButtonChanged;
    }

    UpdateEventSubscriptions();
  }

  public void ToggleShowRangeTooltipOption(bool showRangeTooltip)
  {
    ShowRangeTooltip = showRangeTooltip;
  }

  public void ToggleShowBombRangeOption(bool showBombRange)
  {
    ShowBombRange = showBombRange;
    UpdateEventSubscriptions();
  }

  private void UpdateEventSubscriptions()
  {
    _helper.Events.Display.RenderingHud -= OnRenderingHud;
    _helper.Events.Display.RenderedHud -= OnRenderedHud;
    _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

    if (_showItemEffectRanges || ShowBombRange || ButtonControlShow)
    {
      _helper.Events.Display.RenderingHud += OnRenderingHud;
      _helper.Events.Display.RenderedHud += OnRenderedHud;
      _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }
  }
  #endregion


  #region Event subscriptions
  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (!e.IsMultipleOf(4))
    {
      return;
    }

    // Guard against ticks during loading screen
    if (Game1.currentLocation is null)
    {
      return;
    }

    _effectiveAreaCurrent.Value.Clear();
    _effectiveAreaOther.Value.Clear();
    _effectiveAreaIntersection.Value.Clear();
    _seenTiles.Value.Clear();

    if (Game1.activeClickableMenu == null && UIElementUtils.IsRenderingNormally())
    {
      UpdateEffectiveArea();
      GetOverlapValue();
      if (ButtonShowOneRange)
      {
        ButtonShowOneRange = false;
      }

      if (ButtonShowAllRanges)
      {
        ButtonShowAllRanges = false;
      }
    }
  }

  private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
  {
    foreach (Point point in _effectiveAreaOther.Value)
    {
      var position = new Vector2(
        point.X * Utility.ModifyCoordinateFromUIScale(Game1.tileSize),
        point.Y * Utility.ModifyCoordinateFromUIScale(Game1.tileSize)
      );
      e.SpriteBatch.Draw(
        Game1.mouseCursors,
        Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(Utility.ModifyCoordinatesForUIScale(position))),
        new Rectangle(194, 388, 16, 16),
        Color.LimeGreen * 0.8f,
        0.0f,
        Vector2.Zero,
        Utility.ModifyCoordinateForUIScale(Game1.pixelZoom),
        SpriteEffects.None,
        0.01f
      );
    }

    foreach (Point point in _effectiveAreaIntersection.Value)
    {
      var position = new Vector2(
        point.X * Utility.ModifyCoordinateFromUIScale(Game1.tileSize),
        point.Y * Utility.ModifyCoordinateFromUIScale(Game1.tileSize)
      );
      e.SpriteBatch.Draw(
        Game1.mouseCursors,
        Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(Utility.ModifyCoordinatesForUIScale(position))),
        new Rectangle(194, 388, 16, 16),
        Color.Red * 0.7f,
        0.0f,
        Vector2.Zero,
        Utility.ModifyCoordinateForUIScale(Game1.pixelZoom),
        SpriteEffects.None,
        0.01f
      );
    }
  }

  private void OnButtonChanged(object? sender, ButtonsChangedEventArgs e)
  {
    if (Context.IsPlayerFree)
    {
      if (ModEntry.ModConfig.ShowOneRange.IsDown())
      {
        ButtonShowOneRange = true;
      }

      if (ModEntry.ModConfig.ShowAllRange.IsDown())
      {
        ButtonShowAllRanges = true;
      }
    }
  }

  private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
  {
    if (!ShowRangeTooltip)
    {
      return;
    }

    RangeTooltipInfo? info = _rangeTooltipInfo.Value;
    if (info == null)
    {
      return;
    }

    int reachableTiles = info.RawTotalTiles - info.OccupiedTiles;
    int overlapTiles = _effectiveAreaIntersection.Value.Count;
    int coveredTiles = _effectiveAreaOther.Value.Count + overlapTiles - info.OccupiedTiles;

    SpriteFont font = Game1.smallFont;
    int padding = 16;
    int lineHeight = (int)font.MeasureString("T").Y + 4;

    // Build tooltip lines
    string header = info.ShowingAll
      ? $"{info.ObjectName} x{info.ObjectCount}"
      : info.ObjectName;

    var lines = new List<(string text, Color color)> { (header, Game1.textColor) };

    lines.Add((I18n.ReachableTiles(count: reachableTiles), Tools.TooltipBlue));

    if (info.ShowingAll && info.TrackOverlap)
    {
      lines.Add((I18n.CoveredTiles(count: coveredTiles), Tools.TooltipGreen));
    }

    if (info.TrackOverlap && overlapTiles > 0)
    {
      lines.Add((I18n.OverlappingTiles(count: overlapTiles), Tools.TooltipRed));
    }

    // Calculate dimensions
    float maxWidth = 0;
    foreach ((string text, Color _) in lines)
    {
      float w = font.MeasureString(text).X;
      if (w > maxWidth)
      {
        maxWidth = w;
      }
    }

    int boxWidth = (int)maxWidth + padding * 2;
    int boxHeight = lines.Count * lineHeight + padding * 2 - 4;

    // Position near mouse, keep on screen
    int x = Game1.getMouseX() + 32;
    int y = Game1.getMouseY() + 32;

    if (x + boxWidth > Game1.uiViewport.Width)
    {
      x = Game1.getMouseX() - boxWidth - 8;
    }

    if (y + boxHeight > Game1.uiViewport.Height)
    {
      y = Game1.uiViewport.Height - boxHeight;
    }

    // Draw tooltip box
    IClickableMenu.drawTextureBox(
      e.SpriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
      x, y, boxWidth, boxHeight, Color.White);

    // Draw text lines with soft shadow (same style as crop tooltips)
    Color shadowColor = Game1.textShadowColor;
    int textY = y + padding;
    foreach ((string text, Color color) in lines)
    {
      var pos = new Vector2(x + padding, textY);
      e.SpriteBatch.DrawString(font, text, pos + new Vector2(2f, 2f), shadowColor);
      e.SpriteBatch.DrawString(font, text, pos + new Vector2(0f, 2f), shadowColor);
      e.SpriteBatch.DrawString(font, text, pos + new Vector2(2f, 0f), shadowColor);
      e.SpriteBatch.DrawString(font, text, pos, color * 0.9f);
      textY += lineHeight;
    }
  }
  #endregion


  #region Logic
  private void UpdateEffectiveArea()
  {
    int[][] arrayToUse;
    List<Object> similarObjects;
    _rangeTooltipInfo.Value = null;

    if (_showItemEffectRanges && ButtonControlShow && (ButtonShowOneRange || ButtonShowAllRanges))
    {
      Building building = Game1.currentLocation.getBuildingAt(Game1.GetPlacementGrabTile());

      if (building is JunimoHut hoveredHut)
      {
        arrayToUse = GetDistanceArray(ObjectsWithDistance.JunimoHut);
        int hutTiles = CountTilesInArray(arrayToUse);

        _rangeTooltipInfo.Value = new RangeTooltipInfo
        {
          ObjectName = I18n.TileRange(),
          TrackOverlap = true,
          ObjectCount = 1,
          ShowingAll = ButtonShowAllRanges,
          OccupiedTiles = 6, // 3x2 building
          RawTotalTiles = hutTiles
        };

        AddTilesToHighlightedArea(arrayToUse, !ButtonShowAllRanges, hoveredHut.tileX.Value + 1, hoveredHut.tileY.Value + 1);

        if (ButtonShowAllRanges)
        {
          foreach (Building? nextBuilding in Game1.currentLocation.buildings)
          {
            if (nextBuilding is JunimoHut nextHut && nextHut != hoveredHut)
            {
              _rangeTooltipInfo.Value.ObjectCount++;
              _rangeTooltipInfo.Value.OccupiedTiles += 6;
              _rangeTooltipInfo.Value.RawTotalTiles += hutTiles;
              AddTilesToHighlightedArea(arrayToUse, false, nextHut.tileX.Value + 1, nextHut.tileY.Value + 1);
            }
          }
        }
      }
    }

    // Wild tree seed spread — only on Farm locations (matches game's seed spread logic)
    if (_showItemEffectRanges && ButtonControlShow && (ButtonShowOneRange || ButtonShowAllRanges)
        && Game1.currentLocation is Farm)
    {
      Vector2 gamepadTile = Game1.player.CurrentTool != null
        ? Utility.snapToInt(Game1.player.GetToolLocation() / Game1.tileSize)
        : Utility.snapToInt(Game1.player.GetGrabTile());
      Vector2 mouseTile = Game1.currentCursorTile;
      Vector2 treeTile = Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0 ? gamepadTile : mouseTile;

      if (Game1.currentLocation.terrainFeatures.TryGetValue(treeTile, out TerrainFeature? feature)
          && feature is Tree tree && tree.growthStage.Value >= 5 && !tree.stump.Value)
      {
        arrayToUse = GetDistanceArray(ObjectsWithDistance.WildTreeSeedSpread);
        int treeTiles = CountTilesInArray(arrayToUse);

        _rangeTooltipInfo.Value = new RangeTooltipInfo
        {
          ObjectName = I18n.TileRange(),
          TrackOverlap = false,
          ObjectCount = 1,
          ShowingAll = ButtonShowAllRanges,
          OccupiedTiles = 1,
          RawTotalTiles = treeTiles
        };

        AddTilesToHighlightedArea(arrayToUse, false, (int)treeTile.X, (int)treeTile.Y);

        if (ButtonShowAllRanges)
        {
          foreach (KeyValuePair<Vector2, TerrainFeature> pair in Game1.currentLocation.terrainFeatures.Pairs)
          {
            if (pair.Value is Tree otherTree && otherTree != tree
                && otherTree.growthStage.Value >= 5 && !otherTree.stump.Value)
            {
              _rangeTooltipInfo.Value.ObjectCount++;
              _rangeTooltipInfo.Value.OccupiedTiles++;
              _rangeTooltipInfo.Value.RawTotalTiles += treeTiles;
              AddTilesToHighlightedArea(arrayToUse, false, (int)pair.Key.X, (int)pair.Key.Y);
            }
          }
        }
      }
    }

    // Placed objects (button-controlled range display)
    if (_showItemEffectRanges && ButtonControlShow && (ButtonShowOneRange || ButtonShowAllRanges))
    {
      Vector2 gamepadTile = Game1.player.CurrentTool != null
        ? Utility.snapToInt(Game1.player.GetToolLocation() / Game1.tileSize)
        : Utility.snapToInt(Game1.player.GetGrabTile());
      Vector2 mouseTile = Game1.currentCursorTile;
      Vector2 tile = Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0 ? gamepadTile : mouseTile;
      if (Game1.currentLocation.Objects?.TryGetValue(tile, out Object? currentObject) ?? false)
      {
        if (currentObject != null)
        {
          Vector2 currentTile = Game1.GetPlacementGrabTile();
          Game1.isCheckingNonMousePlacement = !Game1.IsPerformingMousePlacement();
          Vector2 validTile = Utility.snapToInt(
                                Utility.GetNearbyValidPlacementPosition(
                                  Game1.player,
                                  Game1.currentLocation,
                                  currentObject,
                                  (int)currentTile.X * Game1.tileSize,
                                  (int)currentTile.Y * Game1.tileSize
                                )
                              ) /
                              Game1.tileSize;
          Game1.isCheckingNonMousePlacement = false;

          if (currentObject.Name.IndexOf("arecrow", StringComparison.OrdinalIgnoreCase) >= 0)
          {
            string itemName = currentObject.Name;
            arrayToUse = itemName.Contains("eluxe")
              ? GetDistanceArray(ObjectsWithDistance.DeluxeScarecrow, false, currentObject)
              : GetDistanceArray(ObjectsWithDistance.Scarecrow, false, currentObject);

            _rangeTooltipInfo.Value = new RangeTooltipInfo
            {
              ObjectName = I18n.TileRange(),
              TrackOverlap = true,
              ObjectCount = 1,
              ShowingAll = ButtonShowAllRanges,
              OccupiedTiles = 1,
              RawTotalTiles = CountTilesInArray(arrayToUse)
            };

            AddTilesToHighlightedArea(arrayToUse, !ButtonShowAllRanges, (int)validTile.X, (int)validTile.Y);

            if (ButtonShowAllRanges)
            {
              similarObjects = GetSimilarObjectsInLocation("arecrow");
              foreach (Object next in similarObjects)
              {
                if (!next.Equals(currentObject))
                {
                  _rangeTooltipInfo.Value.ObjectCount++;
                  _rangeTooltipInfo.Value.OccupiedTiles++;
                  int[][] arrayToUse_ = next.Name.IndexOf("eluxe", StringComparison.OrdinalIgnoreCase) >= 0
                    ? GetDistanceArray(ObjectsWithDistance.DeluxeScarecrow, false, next)
                    : GetDistanceArray(ObjectsWithDistance.Scarecrow, false, next);
                  _rangeTooltipInfo.Value.RawTotalTiles += CountTilesInArray(arrayToUse_);
                  if (!arrayToUse_.SequenceEqual(arrayToUse))
                  {
                    AddTilesToHighlightedArea(arrayToUse, false, (int)next.TileLocation.X, (int)next.TileLocation.Y);
                  }
                }
              }
            }
          }
          else if (currentObject.Name.IndexOf("sprinkler", StringComparison.OrdinalIgnoreCase) >= 0)
          {
            List<Vector2> sprinklerTilesList = currentObject.GetSprinklerTiles();

            _rangeTooltipInfo.Value = new RangeTooltipInfo
            {
              ObjectName = I18n.TileRange(),
              TrackOverlap = true,
              ObjectCount = 1,
              ShowingAll = ButtonShowAllRanges,
              OccupiedTiles = 1,
              RawTotalTiles = sprinklerTilesList.Count
            };

            IEnumerable<Vector2> unplacedSprinklerTiles = sprinklerTilesList;
            if (currentObject.TileLocation != validTile)
            {
              unplacedSprinklerTiles =
                unplacedSprinklerTiles.Select(tile => tile - currentObject.TileLocation + validTile);
            }

            AddTilesToHighlightedArea(unplacedSprinklerTiles, !ButtonShowAllRanges);

            if (ButtonShowAllRanges)
            {
              similarObjects = GetSimilarObjectsInLocation("sprinkler");
              foreach (Object next in similarObjects)
              {
                if (!next.Equals(currentObject))
                {
                  _rangeTooltipInfo.Value.ObjectCount++;
                  _rangeTooltipInfo.Value.OccupiedTiles++;
                  _rangeTooltipInfo.Value.RawTotalTiles += next.GetSprinklerTiles().Count;
                  AddTilesToHighlightedArea(next.GetSprinklerTiles(), false);
                }
              }
            }
          }
          else if (currentObject.Name.IndexOf("bee house", StringComparison.OrdinalIgnoreCase) >= 0)
          {
            arrayToUse = GetDistanceArray(ObjectsWithDistance.Beehouse);
            _rangeTooltipInfo.Value = new RangeTooltipInfo
            {
              ObjectName = I18n.TileRange(),
              TrackOverlap = false,
              ObjectCount = 1,
              OccupiedTiles = 1,
              RawTotalTiles = CountTilesInArray(arrayToUse)
            };

            AddTilesToHighlightedArea(arrayToUse, false, (int)validTile.X, (int)validTile.Y);
          }
          else if (currentObject.Name.IndexOf("mushroom log", StringComparison.OrdinalIgnoreCase) >= 0)
          {
            arrayToUse = GetDistanceArray(ObjectsWithDistance.MushroomLog);
            _rangeTooltipInfo.Value = new RangeTooltipInfo
            {
              ObjectName = I18n.TileRange(),
              TrackOverlap = false,
              ObjectCount = 1,
              OccupiedTiles = 1,
              RawTotalTiles = CountTilesInArray(arrayToUse)
            };

            AddTilesToHighlightedArea(arrayToUse, false, (int)validTile.X, (int)validTile.Y);
          }
          else if (currentObject.Name.IndexOf("mossy seed", StringComparison.OrdinalIgnoreCase) >= 0)
          {
            arrayToUse = GetDistanceArray(ObjectsWithDistance.MossySeed);
            _rangeTooltipInfo.Value = new RangeTooltipInfo
            {
              ObjectName = I18n.TileRange(),
              TrackOverlap = false,
              ObjectCount = 1,
              OccupiedTiles = 1,
              RawTotalTiles = CountTilesInArray(arrayToUse)
            };

            AddTilesToHighlightedArea(arrayToUse, false, (int)validTile.X, (int)validTile.Y);
          }
        }
      }
    }
    if (Game1.player.CurrentItem is Object currentItem && currentItem.isPlaceable())
    {
      string itemName = currentItem.Name;

      Vector2 currentTile = Game1.GetPlacementGrabTile();
      Game1.isCheckingNonMousePlacement = !Game1.IsPerformingMousePlacement();
      Vector2 validTile = Utility.snapToInt(
                            Utility.GetNearbyValidPlacementPosition(
                              Game1.player,
                              Game1.currentLocation,
                              currentItem,
                              (int)currentTile.X * Game1.tileSize,
                              (int)currentTile.Y * Game1.tileSize
                            )
                          ) /
                          Game1.tileSize;
      Game1.isCheckingNonMousePlacement = false;

      if (_showItemEffectRanges)
      {
        if (itemName.IndexOf("arecrow", StringComparison.OrdinalIgnoreCase) >= 0)
        {
          arrayToUse = itemName.Contains("eluxe")
            ? GetDistanceArray(ObjectsWithDistance.DeluxeScarecrow, false, currentItem)
            : GetDistanceArray(ObjectsWithDistance.Scarecrow, false, currentItem);
          AddTilesToHighlightedArea(arrayToUse, true, (int)validTile.X, (int)validTile.Y);

          similarObjects = GetSimilarObjectsInLocation("arecrow");
          foreach (Object next in similarObjects)
          {
            arrayToUse = next.Name.IndexOf("eluxe", StringComparison.OrdinalIgnoreCase) >= 0
              ? GetDistanceArray(ObjectsWithDistance.DeluxeScarecrow, false, next)
              : GetDistanceArray(ObjectsWithDistance.Scarecrow, false, next);
            AddTilesToHighlightedArea(arrayToUse, false, (int)next.TileLocation.X, (int)next.TileLocation.Y);
          }
        }
        else if (itemName.IndexOf("sprinkler", StringComparison.OrdinalIgnoreCase) >= 0)
        {
          // GetSprinklerTiles returns absolute positions in 1.6+ — offset to valid placement tile
          IEnumerable<Vector2> unplacedSprinklerTiles = currentItem.GetSprinklerTiles();
          if (currentItem.TileLocation != validTile)
          {
            unplacedSprinklerTiles = unplacedSprinklerTiles.Select(tile => tile - currentItem.TileLocation + validTile);
          }

          AddTilesToHighlightedArea(unplacedSprinklerTiles, true);

          similarObjects = GetSimilarObjectsInLocation("sprinkler");
          foreach (Object next in similarObjects)
          {
            AddTilesToHighlightedArea(next.GetSprinklerTiles(), false);
          }
        }
        else if (itemName.IndexOf("bee house", StringComparison.OrdinalIgnoreCase) >= 0)
        {
          arrayToUse = GetDistanceArray(ObjectsWithDistance.Beehouse);
          AddTilesToHighlightedArea(arrayToUse, false, (int)validTile.X, (int)validTile.Y);
        }
        else if (itemName.IndexOf("mushroom log", StringComparison.OrdinalIgnoreCase) >= 0)
        {
          arrayToUse = GetDistanceArray(ObjectsWithDistance.MushroomLog);
          AddTilesToHighlightedArea(arrayToUse, false, (int)validTile.X, (int)validTile.Y);
        }
        else if (itemName.IndexOf("mossy seed", StringComparison.OrdinalIgnoreCase) >= 0)
        {
          arrayToUse = GetDistanceArray(ObjectsWithDistance.MossySeed);
          AddTilesToHighlightedArea(arrayToUse, false, (int)validTile.X, (int)validTile.Y);
        }
      }

      if (ShowBombRange && itemName.IndexOf("Bomb", StringComparison.OrdinalIgnoreCase) >= 0)
      {
        if (itemName.Contains("ega"))
        {
          arrayToUse = GetDistanceArray(ObjectsWithDistance.MegaBomb);
        }
        else if (itemName.Contains("herry"))
        {
          arrayToUse = GetDistanceArray(ObjectsWithDistance.CherryBomb);
        }
        else
        {
          arrayToUse = GetDistanceArray(ObjectsWithDistance.Bomb);
        }

        AddTilesToHighlightedArea(arrayToUse, false, (int)validTile.X, (int)validTile.Y);
      }
    }
  }

  private void AddTilesToHighlightedArea(IEnumerable<Vector2> tiles, bool overlap, int xPos = 0, int yPos = 0)
  {
    foreach (Vector2 tile in tiles)
    {
      var point = tile.ToPoint();
      point.X += xPos;
      point.Y += yPos;
      if (overlap)
      {
        _effectiveAreaCurrent.Value.Add(point);
      }
      else
      {
        if (!_seenTiles.Value.Add(point))
        {
          _effectiveAreaIntersection.Value.Add(point);
        }

        _effectiveAreaOther.Value.Add(point);
      }
    }
  }

  private void AddTilesToHighlightedArea(int[][] tileMap, bool overlap, int xPos = 0, int yPos = 0)
  {
    int xOffset = tileMap.Length / 2;

    for (var i = 0; i < tileMap.Length; ++i)
    {
      int yOffset = tileMap[i].Length / 2;
      for (var j = 0; j < tileMap[i].Length; ++j)
      {
        if (tileMap[i][j] == 1)
        {
          var point = new Point(xPos + i - xOffset, yPos + j - yOffset);
          if (overlap)
          {
            _effectiveAreaCurrent.Value.Add(point);
          }
          else
          {
            if (!_seenTiles.Value.Add(point))
            {
              _effectiveAreaIntersection.Value.Add(point);
            }

            _effectiveAreaOther.Value.Add(point);
          }
        }
      }
    }
  }

  private static int CountTilesInArray(int[][] tileMap)
  {
    int count = 0;
    for (var i = 0; i < tileMap.Length; ++i)
    {
      for (var j = 0; j < tileMap[i].Length; ++j)
      {
        if (tileMap[i][j] == 1)
        {
          count++;
        }
      }
    }

    return count;
  }

  private List<Object> GetSimilarObjectsInLocation(string nameContains)
  {
    var result = new List<Object>();

    if (!string.IsNullOrEmpty(nameContains))
    {
      OverlaidDictionary? objects = Game1.currentLocation.Objects;

      foreach (Object? nextThing in objects.Values)
      {
        if (nextThing.name.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
        {
          result.Add(nextThing);
        }
      }
    }

    return result;
  }

  /// <summary>Compute intersection and exclusive areas between current and other ranges.</summary>
  private void GetOverlapValue()
  {
    if (_effectiveAreaCurrent.Value.Count == 0)
    {
      // Show-all mode: overlaps already detected via _seenTiles during tile addition
      _effectiveAreaOther.Value.ExceptWith(_effectiveAreaIntersection.Value);
      return;
    }

    // Show-one mode: compute overlap between hovered (current) and others
    _effectiveAreaIntersection.Value = _effectiveAreaOther.Value.Intersect(_effectiveAreaCurrent.Value).ToHashSet();
    HashSet<Point> temp = _effectiveAreaCurrent.Value.Except(_effectiveAreaOther.Value).ToHashSet();
    _effectiveAreaOther.Value = _effectiveAreaOther.Value.Except(_effectiveAreaCurrent.Value).ToHashSet();
    _effectiveAreaOther.Value = _effectiveAreaOther.Value.Union(temp).ToHashSet();
  }

  #region Distance map
  private enum ObjectsWithDistance
  {
    JunimoHut,
    Beehouse,
    Scarecrow,
    DeluxeScarecrow,
    Sprinkler,
    QualitySprinkler,
    IridiumSprinkler,
    PrismaticSprinkler,
    MushroomLog,
    MossySeed,
    WildTreeSeedSpread,
    CherryBomb,
    Bomb,
    MegaBomb
  }

  private int[][] GetDistanceArray(ObjectsWithDistance type, bool hasPressureNozzle = false, Object? instance = null)
  {
    switch (type)
    {
      case ObjectsWithDistance.JunimoHut:
        return GetCircularMask(100, maxDisplaySquareRadius: 8);
      case ObjectsWithDistance.Beehouse:
        return GetCircularMask(4.19, 5, true);
      case ObjectsWithDistance.Scarecrow:
        return GetCircularMask((instance?.GetRadiusForScarecrow() ?? 9) - 0.01);
      case ObjectsWithDistance.DeluxeScarecrow:
        return GetCircularMask((instance?.GetRadiusForScarecrow() ?? 17) - 0.01);
      case ObjectsWithDistance.Sprinkler:
        return hasPressureNozzle ? GetCircularMask(100, maxDisplaySquareRadius: 1) : GetCircularMask(1);
      case ObjectsWithDistance.QualitySprinkler:
        return hasPressureNozzle
          ? GetCircularMask(100, maxDisplaySquareRadius: 2)
          : GetCircularMask(100, maxDisplaySquareRadius: 1);
      case ObjectsWithDistance.IridiumSprinkler:
        return hasPressureNozzle
          ? GetCircularMask(100, maxDisplaySquareRadius: 3)
          : GetCircularMask(100, maxDisplaySquareRadius: 2);
      case ObjectsWithDistance.PrismaticSprinkler:
        return GetCircularMask(3.69, Math.Sqrt(18), false);
      case ObjectsWithDistance.MushroomLog:
        return GetCircularMask(100, maxDisplaySquareRadius: 3);
      case ObjectsWithDistance.MossySeed:
        return GetCircularMask(100, maxDisplaySquareRadius: 2);
      case ObjectsWithDistance.WildTreeSeedSpread:
        return GetCircularMask(100, maxDisplaySquareRadius: 3);
      case ObjectsWithDistance.CherryBomb:
        return GetCircularMask(3.39);
      case ObjectsWithDistance.Bomb:
        return GetCircularMask(5.52);
      case ObjectsWithDistance.MegaBomb:
        return GetCircularMask(7.45);
      default:
        throw new ArgumentOutOfRangeException(nameof(type), type, null);
    }
  }

  private static int[][] GetCircularMask(
    double maxDistance,
    double? exceptionalDistance = null,
    bool? onlyClearExceptions = null,
    int? maxDisplaySquareRadius = null
  )
  {
    int radius = Math.Max(
      (int)Math.Ceiling(maxDistance),
      exceptionalDistance.HasValue ? (int)Math.Ceiling(exceptionalDistance.Value) : 0
    );
    radius = Math.Min(radius, maxDisplaySquareRadius.HasValue ? maxDisplaySquareRadius.Value : radius);
    int size = 2 * radius + 1;

    var result = new int[size][];
    for (var i = 0; i < size; i++)
    {
      result[i] = new int[size];
      for (var j = 0; j < size; j++)
      {
        double distance = GetDistance(i, j, radius);
        int val = IsInDistance(maxDistance, distance) ||
                  (IsDistanceDirectionOK(i, j, radius, onlyClearExceptions) &&
                   IsExceptionalDistanceOK(exceptionalDistance, distance))
          ? 1
          : 0;
        result[i][j] = val;
      }
    }

    return result;
  }

  private static bool IsDistanceDirectionOK(int i, int j, int radius, bool? onlyClearExceptions)
  {
    return onlyClearExceptions.HasValue && onlyClearExceptions.Value ? radius - j == 0 || radius - i == 0 : true;
  }

  private static bool IsExceptionalDistanceOK(double? exceptionalDistance, double distance)
  {
    return exceptionalDistance.HasValue && exceptionalDistance.Value == distance;
  }

  private static bool IsInDistance(double maxDistance, double distance)
  {
    return distance <= maxDistance;
  }

  private static double GetDistance(int i, int j, int radius)
  {
    return Math.Sqrt((radius - i) * (radius - i) + (radius - j) * (radius - j));
  }
  #endregion
  #endregion
}
