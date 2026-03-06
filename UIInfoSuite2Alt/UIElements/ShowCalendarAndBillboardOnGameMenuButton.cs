using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
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

  private readonly IModHelper _helper;

  private readonly PerScreen<Item?> _hoverItem = new();
  private readonly PerScreen<Item?> _heldItem = new();
  #endregion

  #region Lifecycle
  public ShowCalendarAndBillboardOnGameMenuButton(IModHelper helper)
  {
    _helper = helper;
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

    if (showCalendarAndBillboard)
    {
      _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
      _helper.Events.Input.ButtonPressed += OnButtonPressed;
      _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }
  }
  #endregion


  #region Event subscriptions
  private void OnUpdateTicked(object? sender, EventArgs e)
  {
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

    int baseX = menu.xPositionOnScreen + menu.width - 120;
    int baseY = menu.yPositionOnScreen + menu.height -
                (_helper.ModRegistry.IsLoaded("spacechase0.BiggerBackpack") ? 230 : 300);

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

    if (!isCalendar && !isQuest)
    {
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
