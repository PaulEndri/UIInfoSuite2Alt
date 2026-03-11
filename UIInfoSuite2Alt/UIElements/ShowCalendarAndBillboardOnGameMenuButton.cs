using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Locations;
using StardewValley.SpecialOrders;
using UIInfoSuite2Alt.Compatibility;
using UIInfoSuite2Alt.Infrastructure;

namespace UIInfoSuite2Alt.UIElements;

internal class ShowCalendarAndBillboardOnGameMenuButton : IDisposable
{
  #region Properties
  private const int IconSpacing = 8;
  private const int DrawSize = 32;

  private readonly PerScreen<Rectangle> _calendarBounds = new(() => Rectangle.Empty);
  private readonly PerScreen<Rectangle> _questBounds = new(() => Rectangle.Empty);
  private readonly PerScreen<Rectangle> _specialOrdersBounds = new(() => Rectangle.Empty);
  private readonly PerScreen<Rectangle> _qiOrdersBounds = new(() => Rectangle.Empty);

  private readonly IModHelper _helper;
  private readonly Texture2D _townTexture;

  private readonly PerScreen<Item?> _hoverItem = new();
  private readonly PerScreen<Item?> _heldItem = new();

  private int _soPulseTimer;
  private int _soPulseDelay;
  private readonly PerScreen<HashSet<string>> _viewedSpecialOrderKeys = new(() => new HashSet<string>());
  private readonly PerScreen<HashSet<string>> _viewedQiOrderKeys = new(() => new HashSet<string>());
  #endregion

  #region Lifecycle
  public ShowCalendarAndBillboardOnGameMenuButton(IModHelper helper)
  {
    _helper = helper;
    _townTexture = helper.GameContent.Load<Texture2D>("Maps/spring_town");
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool showCalendarAndBillboard)
  {
    ModEntry.RegisterCalendarAndQuestKeyBindings(_helper, showCalendarAndBillboard);

    _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;
    _helper.Events.Input.ButtonPressed -= OnButtonPressed;
    _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
    _helper.Events.GameLoop.DayStarted -= OnDayStarted;

    if (showCalendarAndBillboard)
    {
      _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
      _helper.Events.Input.ButtonPressed += OnButtonPressed;
      _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
      _helper.Events.GameLoop.DayStarted += OnDayStarted;
    }
  }
  #endregion


  #region Event subscriptions
  private void OnUpdateTicked(object? sender, EventArgs e)
  {
    // Update special orders pulse timer
    int elapsed = (int)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
    if (_soPulseTimer > 0)
    {
      _soPulseTimer -= elapsed;
    }
    else if (_soPulseDelay > 0)
    {
      _soPulseDelay -= elapsed;
    }
    else
    {
      _soPulseTimer = 1000;
      _soPulseDelay = 3000;
    }

    // Get hovered and hold item
    _hoverItem.Value = Tools.GetHoveredItem();
    IClickableMenu? menu = Game1.activeClickableMenu;
    if (!GameMenuHelper.IsGameMenu(menu))
    {
      return;
    }

    if (GameMenuHelper.IsTab(menu, GameMenu.inventoryTab) &&
        GameMenuHelper.GetCurrentPage(menu) is InventoryPage)
    {
      _heldItem.Value = Game1.player.CursorSlotItem;
    }
  }

  private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
  {
    if (e.Button == SButton.MouseLeft)
    {
      ActivateBillboard();
    }
    else if (e.Button == SButton.ControllerA)
    {
      ActivateBillboard();
    }
  }

  private void OnDayStarted(object? sender, DayStartedEventArgs e)
  {
    // Clear viewed keys if available orders have changed
    HashSet<string> currentKeys = new(
      Game1.player.team.availableSpecialOrders
        .Where(o => o.orderType.Value == "")
        .Select(o => o.questKey.Value));

    if (!_viewedSpecialOrderKeys.Value.SetEquals(currentKeys))
    {
      _viewedSpecialOrderKeys.Value.Clear();
    }

    // Clear viewed Qi keys if available Qi orders have changed
    HashSet<string> currentQiKeys = new(
      Game1.player.team.availableSpecialOrders
        .Where(o => o.orderType.Value == "Qi")
        .Select(o => o.questKey.Value));

    if (!_viewedQiOrderKeys.Value.SetEquals(currentQiKeys))
    {
      _viewedQiOrderKeys.Value.Clear();
    }
  }

  private void OnRenderedActiveMenu(object? sender, EventArgs e)
  {
    IClickableMenu? activeMenu = Game1.activeClickableMenu;

    if (GameMenuHelper.IsTab(activeMenu, GameMenu.inventoryTab) &&
        GameMenuHelper.GetChildMenu(activeMenu) == null)
    {
      DrawBillboard();
    }
  }
  #endregion


  #region Logic
  private void DrawBillboard()
  {
    IClickableMenu menu = Game1.activeClickableMenu;
    if (menu == null) return;

    // Library Mods
    bool biggerBackpack = _helper.ModRegistry.IsLoaded("spacechase0.BiggerBackpack");
    bool fullInventoryView = _helper.ModRegistry.IsLoaded("CpdnCristiano.FullInventoryView");

    // Content Patcher Mods
    bool cpCatValley = _helper.ModRegistry.IsLoaded("RimeNovi.CatValley");

    // Vanilla offset
    int offset = 294;

    if (biggerBackpack)
      offset -= 64;

    if (fullInventoryView)
      offset -= 64;

    if (cpCatValley)
      offset -= 8;

    ModEntry.MonitorObject.LogOnce($"offset: {offset}", LogLevel.Warn);

    int baseX = menu.xPositionOnScreen + menu.width - 120;
    int baseY = menu.yPositionOnScreen + menu.height - offset;

    SpriteBatch b = Game1.spriteBatch;
    int mouseX = Game1.getMouseX();
    int mouseY = Game1.getMouseY();

    ParsedItemData calendarData = ItemRegistry.GetDataOrErrorItem("(F)1402");
    Rectangle calendarSrc = calendarData.GetSourceRect();
    Rectangle calendarDest = new(baseX, baseY - 28, calendarSrc.Width * 2, calendarSrc.Height * 2);
    Rectangle questDest = new(baseX + DrawSize + IconSpacing, baseY - 6, DrawSize, DrawSize);

    _calendarBounds.Value = new Rectangle(baseX, baseY - 6, DrawSize, DrawSize);
    _questBounds.Value = questDest;

    b.Draw(calendarData.GetTexture(), calendarDest, calendarSrc, Color.White);
    b.Draw(Game1.objectSpriteSheet, questDest, new Rectangle(144, 592, 16, 16), Color.White);

    // Draw exclamation mark when a daily quest is available
    if (Game1.CanAcceptDailyQuest())
    {
      float scale = 1.6f;
      b.Draw(
        Game1.mouseCursors,
        new Vector2(questDest.X + questDest.Width - 3f, questDest.Y - 5f),
        new Rectangle(403, 496, 5, 14),
        Color.White,
        0f,
        Vector2.Zero,
        scale,
        SpriteEffects.None,
        1f
      );
    }

    // Draw Special Orders board icon below billboard (only when unlocked)
    if (SpecialOrder.IsSpecialOrdersBoardUnlocked())
    {
      int soWidth = DrawSize * 17 / 13;
      Rectangle specialOrdersDest = new(
        questDest.X - 4, questDest.Y + DrawSize + IconSpacing, soWidth, DrawSize
      );
      _specialOrdersBounds.Value = specialOrdersDest;

      b.Draw(
        _townTexture, specialOrdersDest,
        new Rectangle(480, 1001, 17, 13), Color.White
      );

      // Draw animated exclamation mark when special orders are available
      bool hasUnviewedOrders = Game1.player.team.availableSpecialOrders
        .Where(o => o.orderType.Value == "")
        .Any(o => !_viewedSpecialOrderKeys.Value.Contains(o.questKey.Value));
      if (hasUnviewedOrders && !Game1.player.team.acceptedSpecialOrderTypes.Contains(""))
      {
        DrawPulsingExclamation(b, new Vector2(
          specialOrdersDest.X + specialOrdersDest.Width - 4f,
          specialOrdersDest.Y + 5f));
      }
    }
    else
    {
      _specialOrdersBounds.Value = Rectangle.Empty;
    }

    // Draw Qi's Special Orders board icon to the left of SO icon (only when Qi room unlocked)
    if (IslandWest.IsQiWalnutRoomDoorUnlocked(out _))
    {
      int qiWidth = 15 * 2;
      int qiHeight = 14 * 2;
      Rectangle soBounds = _specialOrdersBounds.Value;
      Rectangle qiOrdersDest = new(
        soBounds != Rectangle.Empty
          ? soBounds.X - qiWidth - IconSpacing + 4
          : questDest.X - 4,
        (soBounds != Rectangle.Empty
          ? soBounds.Y
          : questDest.Y + DrawSize + IconSpacing) + 2,
        qiWidth, qiHeight
      );
      _qiOrdersBounds.Value = qiOrdersDest;

      b.Draw(
        Game1.objectSpriteSheet, qiOrdersDest,
        new Rectangle(288, 561, 15, 14), Color.White
      );

      // Draw animated exclamation mark when Qi orders are available
      bool hasUnviewedQiOrders = Game1.player.team.availableSpecialOrders
        .Where(o => o.orderType.Value == "Qi")
        .Any(o => !_viewedQiOrderKeys.Value.Contains(o.questKey.Value));
      if (hasUnviewedQiOrders && !Game1.player.team.acceptedSpecialOrderTypes.Contains("Qi"))
      {
        DrawPulsingExclamation(b, new Vector2(
          qiOrdersDest.X + qiOrdersDest.Width - 4f,
          qiOrdersDest.Y + 3f));
      }
    }
    else
    {
      _qiOrdersBounds.Value = Rectangle.Empty;
    }

    if (_heldItem.Value != null)
    {
      _heldItem.Value.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 16, Game1.getOldMouseY() + 16), 1f);
    }

    if (_hoverItem.Value != null)
    {
      IClickableMenu.drawToolTip(
          b,
          _hoverItem.Value.getDescription(),
          _hoverItem.Value.DisplayName,
          _hoverItem.Value,
          _heldItem.Value != null
      );
    }

    menu.drawMouse(b);

    if (calendarDest.Contains(mouseX, mouseY))
    {
      IClickableMenu.drawHoverText(b, I18n.Calendar(), Game1.dialogueFont);
    }
    else if (questDest.Contains(mouseX, mouseY))
    {
      IClickableMenu.drawHoverText(b, I18n.Billboard(), Game1.dialogueFont);
    }
    else if (_specialOrdersBounds.Value.Contains(mouseX, mouseY))
    {
      IClickableMenu.drawHoverText(b, I18n.SpecialOrders(), Game1.dialogueFont);
    }
    else if (_qiOrdersBounds.Value.Contains(mouseX, mouseY))
    {
      IClickableMenu.drawHoverText(b, I18n.QiSpecialOrders(), Game1.dialogueFont);
    }
  }

  private void DrawPulsingExclamation(SpriteBatch b, Vector2 position)
  {
    float baseScale = 1.6f;
    float scale = baseScale;
    Vector2 shake = Vector2.Zero;

    if (_soPulseTimer > 0)
    {
      float pulseScale = 1f / (Math.Max(300f, Math.Abs(_soPulseTimer % 1000 - 500)) / 500f);
      scale = baseScale * pulseScale;
      if (pulseScale > 1f)
      {
        shake = new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
      }
    }

    b.Draw(
      Game1.mouseCursors,
      position + shake,
      new Rectangle(403, 496, 5, 14),
      Color.White,
      0f,
      new Vector2(2.5f, 7f),
      scale,
      SpriteEffects.None,
      1f
    );
  }

  private void ActivateBillboard()
  {
    if (!GameMenuHelper.IsTab(Game1.activeClickableMenu, GameMenu.inventoryTab) ||
        _heldItem.Value != null)
    {
      return;
    }

    int mouseX = (int)Utility.ModifyCoordinateForUIScale(Game1.getMouseX());
    int mouseY = (int)Utility.ModifyCoordinateForUIScale(Game1.getMouseY());

    bool isCalendar = _calendarBounds.Value.Contains(mouseX, mouseY);
    bool isQuest = _questBounds.Value.Contains(mouseX, mouseY);
    bool isSpecialOrders = _specialOrdersBounds.Value.Contains(mouseX, mouseY);
    bool isQiOrders = _qiOrdersBounds.Value.Contains(mouseX, mouseY);

    if (!isCalendar && !isQuest && !isSpecialOrders && !isQiOrders)
    {
      return;
    }

    if (isQiOrders)
    {
      _viewedQiOrderKeys.Value = new HashSet<string>(
        Game1.player.team.availableSpecialOrders
          .Where(o => o.orderType.Value == "Qi")
          .Select(o => o.questKey.Value));
      Game1.activeClickableMenu = new SpecialOrdersBoard("Qi");
      return;
    }

    if (isSpecialOrders)
    {
      _viewedSpecialOrderKeys.Value = new HashSet<string>(
        Game1.player.team.availableSpecialOrders
          .Where(o => o.orderType.Value == "")
          .Select(o => o.questKey.Value));
      Game1.activeClickableMenu = new SpecialOrdersBoard();
      return;
    }

    if (Game1.questOfTheDay != null && string.IsNullOrEmpty(Game1.questOfTheDay.currentObjective))
    {
      Game1.questOfTheDay.currentObjective = "wat?";
    }

    Game1.activeClickableMenu = new Billboard(dailyQuest: isQuest);
  }
  #endregion
}
