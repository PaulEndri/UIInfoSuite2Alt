using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Quests;
using StardewValley.WorldMaps;
using UIInfoSuite2Alt.Compatibility;
using UIInfoSuite2Alt.Infrastructure;
using UIInfoSuite2Alt.Infrastructure.Extensions;
namespace UIInfoSuite2Alt.UIElements;

internal class LocationOfTownsfolk : IDisposable
{
  #region Properties
  private SocialPage? _socialPage;
  private readonly List<string> _friendNames = new();
  private readonly List<NPC> _townsfolk = new();
  private readonly List<OptionsCheckbox> _checkboxes = new();

  private readonly IModHelper _helper;

  private const int SocialPanelWidth = 190;
  private const int SocialPanelXOffset = 160;
  private const int MaxVisibleSlots = 5;
  private const int PixelsPerSlot = 112;
  #endregion

  #region Lifecycle
  public LocationOfTownsfolk(IModHelper helper)
  {
    _helper = helper;
  }

  public void ToggleShowNPCLocationsOnMap(bool showLocations)
  {
    InitializeProperties();
    _helper.Events.Display.MenuChanged -= OnMenuChanged;
    _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu_DrawSocialPageOptions;
    _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu_DrawNPCLocationsOnMap;
    _helper.Events.Input.ButtonPressed -= OnButtonPressed_ForSocialPage;
    _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

    if (showLocations)
    {
      _helper.Events.Display.MenuChanged += OnMenuChanged;
      _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu_DrawSocialPageOptions;
      _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu_DrawNPCLocationsOnMap;
      _helper.Events.Input.ButtonPressed += OnButtonPressed_ForSocialPage;
      _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }
  }

  public void Dispose()
  {
    ToggleShowNPCLocationsOnMap(false);
  }
  #endregion

  #region Event subscriptions
  private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
  {
    InitializeProperties();
  }

  private void OnButtonPressed_ForSocialPage(object? sender, ButtonPressedEventArgs e)
  {
    if (GameMenuHelper.IsTab(Game1.activeClickableMenu, GameMenu.socialTab) &&
        e.Button is SButton.MouseLeft or SButton.ControllerA or SButton.ControllerX)
    {
      CheckSelectedBox(e);
    }
  }

  private void OnRenderedActiveMenu_DrawSocialPageOptions(object? sender, RenderedActiveMenuEventArgs e)
  {
    if (GameMenuHelper.IsTab(Game1.activeClickableMenu, GameMenu.socialTab))
    {
      DrawSocialPageOptions();
    }
  }

  private void OnRenderedActiveMenu_DrawNPCLocationsOnMap(object? sender, RenderedActiveMenuEventArgs e)
  {
    if (GameMenuHelper.IsTab(Game1.activeClickableMenu, GameMenu.mapTab))
    {
      DrawNPCLocationsOnMap();
    }
  }

  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (!e.IsOneSecond || (Context.IsSplitScreen && Context.ScreenId != 0))
    {
      return;
    }

    _townsfolk.Clear();

    // RSV map does its own NPC tracking
    bool isRsvWorldMap =
      Game1.activeClickableMenu?.GetChildMenu()?.GetType().FullName?.Equals("RidgesideVillage.RSVWorldMap") ?? false;

    if (isRsvWorldMap)
    {
      ModEntry.MonitorObject.Log("Not Rendering Villagers, in RSV Map");
      return;
    }

    foreach (GameLocation? loc in Game1.locations)
    {
      foreach (NPC? character in loc.characters)
      {
        if (character.IsVillager)
        {
          _townsfolk.Add(character);
        }
      }
    }
  }
  #endregion

  #region Logic
  private void InitializeProperties()
  {
    IClickableMenu? menu = Game1.activeClickableMenu;
    if (GameMenuHelper.IsGameMenu(menu))
    {
      _friendNames.Clear();
      SocialPage? socialPage = GameMenuHelper.FindPage<SocialPage>(menu);
      if (socialPage != null)
      {
        _socialPage = socialPage;
        foreach (SocialPage.SocialEntry? SocialEntries in socialPage.SocialEntries)
        {
          _friendNames.Add(SocialEntries.InternalName);
        }
      }

      _checkboxes.Clear();
      for (var i = 0; i < _friendNames.Count; i++)
      {
        string friendName = _friendNames[i];
        var checkbox = new OptionsCheckbox("", i);
        if (Game1.player.friendshipData.ContainsKey(friendName))
        {
          // npc
          checkbox.greyedOut = false;
          checkbox.isChecked = ModEntry.ModConfig.ShowLocationOfFriends.GetOrDefault(friendName, true);
        }
        else
        {
          // player
          checkbox.greyedOut = true;
          checkbox.isChecked = true;
        }

        _checkboxes.Add(checkbox);
      }
    }
  }

  private void CheckSelectedBox(ButtonPressedEventArgs e)
  {
    if (_socialPage is null || _checkboxes.Count == 0)
    {
      ModEntry.MonitorObject.LogOnce(
        $"Social page not ready during checkbox input (socialPage: {_socialPage is not null}, checkboxes: {_checkboxes.Count}, activeMenu: {Game1.activeClickableMenu?.GetType().FullName ?? "null"}). Another mod may be interfering.",
        LogLevel.Warn
      );
      return;
    }

    for (int i = _socialPage.slotPosition; i < _socialPage.slotPosition + MaxVisibleSlots; ++i)
    {
      OptionsCheckbox checkbox = _checkboxes[i];
      var rect = new Rectangle(checkbox.bounds.X, checkbox.bounds.Y, checkbox.bounds.Width, checkbox.bounds.Height);
      if (e.Button == SButton.ControllerX)
      {
        rect.Width += SocialPanelWidth + _socialPage.width;
      }

      if (rect.Contains(
            (int)Utility.ModifyCoordinateForUIScale(Game1.getMouseX()),
            (int)Utility.ModifyCoordinateForUIScale(Game1.getMouseY())
          ) &&
          !checkbox.greyedOut)
      {
        checkbox.isChecked = !checkbox.isChecked;
        ModEntry.ModConfig.ShowLocationOfFriends[_friendNames[checkbox.whichOption]] = checkbox.isChecked;
        ModEntry.SaveConfig();
        Game1.playSound("drumkit6");
      }
    }
  }

  private void DrawSocialPageOptions()
  {
    if (_socialPage is null || _checkboxes.Count == 0)
    {
      ModEntry.MonitorObject.LogOnce(
        $"Social page not ready during draw (socialPage: {_socialPage is not null}, checkboxes: {_checkboxes.Count}, activeMenu: {Game1.activeClickableMenu?.GetType().FullName ?? "null"}). Another mod may be interfering.",
        LogLevel.Warn
      );
      return;
    }

    Game1.drawDialogueBox(
      _socialPage.xPositionOnScreen - SocialPanelXOffset,
      _socialPage.yPositionOnScreen,
      SocialPanelWidth,
      _socialPage.height,
      false,
      true
    );

    var yOffset = 0;

    for (int i = _socialPage.slotPosition; i < _socialPage.slotPosition + MaxVisibleSlots && i < _friendNames.Count; ++i)
    {
      OptionsCheckbox checkbox = _checkboxes[i];
      checkbox.bounds.X = _socialPage.xPositionOnScreen - 60;

      checkbox.bounds.Y = _socialPage.yPositionOnScreen + 130 + yOffset;

      checkbox.draw(Game1.spriteBatch, 0, 0);
      yOffset += PixelsPerSlot;
      Color color = checkbox.isChecked ? Color.White : Color.Gray;

      Game1.spriteBatch.Draw(
        Game1.mouseCursors,
        new Vector2(checkbox.bounds.X - 50, checkbox.bounds.Y),
        new Rectangle(80, 0, 16, 16),
        color,
        0.0f,
        Vector2.Zero,
        3f,
        SpriteEffects.None,
        1f
      );

      if (yOffset != MaxVisibleSlots * PixelsPerSlot)
      {
        // separator line
        Game1.spriteBatch.Draw(
          Game1.staminaRect,
          new Rectangle(checkbox.bounds.X - 50, checkbox.bounds.Y + 72, SocialPanelWidth / 2 - 6, 4),
          Color.SaddleBrown
        );
        Game1.spriteBatch.Draw(
          Game1.staminaRect,
          new Rectangle(checkbox.bounds.X - 50, checkbox.bounds.Y + 76, SocialPanelWidth / 2 - 6, 4),
          Color.BurlyWood
        );
      }

      if (!Game1.options.hardwareCursor)
      {
        Game1.spriteBatch.Draw(
          Game1.mouseCursors,
          new Vector2(Game1.getMouseX(), Game1.getMouseY()),
          Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, Game1.mouseCursor, 16, 16),
          Color.White,
          0.0f,
          Vector2.Zero,
          Game1.pixelZoom + Game1.dialogueButtonScale / 150.0f,
          SpriteEffects.None,
          1f
        );
      }

      if (checkbox.bounds.Contains(Game1.getMouseX(), Game1.getMouseY()))
      {
        IClickableMenu.drawHoverText(Game1.spriteBatch, I18n.TrackOnMap(), Game1.dialogueFont);
      }
    }
  }

  private void DrawNPCLocationsOnMap()
  {
    var namesToShow = new List<string>();
    foreach (NPC character in _townsfolk)
    {
      try
      {
        bool shouldDrawCharacter = Game1.player.friendshipData.ContainsKey(character.Name) &&
                                   ModEntry.ModConfig.ShowLocationOfFriends.GetOrDefault(character.Name, true) &&
                                   character.id != -1 &&
                                   character.IsInvisible != true;
        if (shouldDrawCharacter)
        {
          DrawNPC(character, namesToShow);
        }
      }
      catch (Exception ex)
      {
        ModEntry.MonitorObject.Log(ex.Message + Environment.NewLine + ex.StackTrace, LogLevel.Error);
      }
    }

    DrawNPCNames(namesToShow);

    // Cursor must render above character faces
    Tools.DrawMouseCursor();

    if (GameMenuHelper.GetCurrentPage(Game1.activeClickableMenu) is MapPage mapPage)
    {
      IClickableMenu.drawHoverText(Game1.spriteBatch, mapPage.hoverText, Game1.smallFont);
    }
  }

  private static void DrawNPC(NPC character, List<string> namesToShow)
  {
    Vector2? location = GetMapCoordinatesForNPC(character);
    if (location is null)
    {
      return;
    }

    Rectangle headShot = character.GetHeadShot();
    MapAreaPosition? mapPosition = Tools.GetMapPositionDataSafe(
      Game1.player.currentLocation,
      new Point((int)location.Value.X, (int)location.Value.Y)
    );

    if (mapPosition is null)
    {
      ModEntry.MonitorObject.LogOnce($"Unable to draw headshot for {character.Name}");
      return;
    }

    MapRegion mapRegion = mapPosition.Region;
    Rectangle mapBounds = mapRegion.GetMapPixelBounds();
    var offsetLocation = new Vector2(
      location.Value.X + mapBounds.X - headShot.Width,
      location.Value.Y + mapBounds.Y - headShot.Height
    );
    // Game uses constant 32 (player face size); we use headShot.Width instead
    Color color = character.CurrentDialogue.Count <= 0 ? Color.Gray : Color.White;
    var headShotScale = 2f;
    Game1.spriteBatch.Draw(
      character.Sprite.Texture,
      offsetLocation,
      headShot,
      color,
      0.0f,
      Vector2.Zero,
      headShotScale,
      SpriteEffects.None,
      1f
    );

    int mouseX = Game1.getMouseX();
    int mouseY = Game1.getMouseY();
    if (mouseX >= offsetLocation.X &&
        mouseX - offsetLocation.X <= headShot.Width * headShotScale &&
        mouseY >= offsetLocation.Y &&
        mouseY - offsetLocation.Y <= headShot.Height * headShotScale)
    {
      namesToShow.Add(character.displayName);
    }

    DrawQuestsForNPC(character, (int)offsetLocation.X, (int)offsetLocation.Y);
  }

  private static Vector2? GetMapCoordinatesForNPC(NPC character)
  {
    var playerNormalizedTile = new Point(Math.Max(0, Game1.player.TilePoint.X), Math.Max(0, Game1.player.TilePoint.Y));
    // Falls back to farm position for buildings where GetPositionData returns null
    MapAreaPosition? playerMapAreaPosition = Tools.GetMapPositionDataSafe(Game1.player.currentLocation, playerNormalizedTile);

    var characterNormalizedTile = new Point(Math.Max(0, character.TilePoint.X), Math.Max(0, character.TilePoint.Y));
    MapAreaPosition? characterMapAreaPosition = Tools.GetMapPositionDataSafe(character.currentLocation, characterNormalizedTile);

    if (playerMapAreaPosition != null &&
        characterMapAreaPosition != null &&
        characterMapAreaPosition.Region.Id == playerMapAreaPosition.Region.Id)
    {
      return characterMapAreaPosition.GetMapPixelPosition(character.currentLocation, characterNormalizedTile);
    }

    return null;
  }

  private static void DrawQuestsForNPC(NPC character, int x, int y)
  {
    foreach (Quest? quest in Game1.player.questLog)
    {
      if (!quest.accepted.Value || !quest.dailyQuest.Value || quest.completed.Value)
      {
        continue;
      }

      if ((quest is ItemDeliveryQuest idq && idq.target.Value == character.Name) ||
          (quest is SlayMonsterQuest smq && smq.target.Value == character.Name) ||
          (quest is FishingQuest fq && fq.target.Value == character.Name) ||
          (quest is ResourceCollectionQuest rq && rq.target.Value == character.Name))
      {
        Game1.spriteBatch.Draw(
          Game1.mouseCursors,
          new Vector2(x + 10, y - 12),
          new Rectangle(394, 495, 4, 10),
          Color.White,
          0.0f,
          Vector2.Zero,
          3f,
          SpriteEffects.None,
          1f
        );
      }
    }
  }

  private static void DrawNPCNames(List<string> namesToShow)
  {
    if (namesToShow.Count == 0)
    {
      return;
    }

    var text = new StringBuilder();
    var longestLength = 0;
    foreach (string name in namesToShow)
    {
      text.AppendLine(name);
      longestLength = Math.Max(longestLength, (int)Math.Ceiling(Game1.smallFont.MeasureString(name).Length()));
    }

    int windowHeight = Game1.smallFont.LineSpacing * namesToShow.Count + 25;
    var windowPos = new Vector2(Game1.getMouseX() + 40, Game1.getMouseY() - windowHeight);
    IClickableMenu.drawTextureBox(
      Game1.spriteBatch,
      (int)windowPos.X,
      (int)windowPos.Y,
      longestLength + 30,
      Game1.smallFont.LineSpacing * namesToShow.Count + 25,
      Color.White
    );

    Game1.spriteBatch.DrawString(
      Game1.smallFont,
      text,
      new Vector2(windowPos.X + 17, windowPos.Y + 17),
      Game1.textShadowColor
    );

    Game1.spriteBatch.DrawString(
      Game1.smallFont,
      text,
      new Vector2(windowPos.X + 15, windowPos.Y + 15),
      Game1.textColor
    );
  }
  #endregion
}
