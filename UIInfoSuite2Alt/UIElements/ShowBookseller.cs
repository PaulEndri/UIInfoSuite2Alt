using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2Alt.Infrastructure;

namespace UIInfoSuite2Alt.UIElements;

public class ShowBookseller : IDisposable
{
  #region Properties
  private bool _booksellerIsHere;
  private bool _booksellerIsVisited;
  private ClickableTextureComponent _booksellerIcon = null!;

  private bool Enabled { get; set; }
  private bool HideWhenVisited { get; set; }

  private readonly IModHelper _helper;
  #endregion


  #region Lifecycle
  public ShowBookseller(IModHelper helper)
  {
    _helper = helper;
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool showBookseller)
  {
    Enabled = showBookseller;

    _helper.Events.Display.RenderingHud -= OnRenderingHud;
    _helper.Events.Display.RenderedHud -= OnRenderedHud;
    _helper.Events.GameLoop.DayStarted -= OnDayStarted;
    _helper.Events.Display.MenuChanged -= OnMenuChanged;

    if (showBookseller)
    {
      UpdateBookseller();
      _helper.Events.Display.RenderingHud += OnRenderingHud;
      _helper.Events.Display.RenderedHud += OnRenderedHud;
      _helper.Events.GameLoop.DayStarted += OnDayStarted;
      _helper.Events.Display.MenuChanged += OnMenuChanged;
    }
  }

  public void ToggleHideWhenVisitedOption(bool hideWhenVisited)
  {
    HideWhenVisited = hideWhenVisited;
    ToggleOption(Enabled);
  }
  #endregion


  #region Event subscriptions
  private void OnDayStarted(object? sender, EventArgs e)
  {
    UpdateBookseller();
  }

  private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
  {
    if (e.NewMenu is ShopMenu menu && menu.ShopId == "Bookseller")
    {
      _booksellerIsVisited = true;
    }
  }

  private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
  {
    if (UIElementUtils.IsRenderingNormally() && ShouldDrawIcon())
    {
      Point iconPosition = IconHandler.Handler.GetNewIconPosition();
      _booksellerIcon = new ClickableTextureComponent(
        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
        Game1.mouseCursors_1_6,
        new Rectangle(52, 477, 20, 20),
        2f
      );
      _booksellerIcon.draw(Game1.spriteBatch);
    }
  }

  private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
  {
    if (ShouldDrawIcon() && (_booksellerIcon?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false))
    {
      string hoverText = I18n.BooksellerIsInTown();
      IClickableMenu.drawHoverText(Game1.spriteBatch, hoverText, Game1.dialogueFont);
    }
  }
  #endregion


  #region Logic
  private void UpdateBookseller()
  {
    var booksellerDays = Utility.getDaysOfBooksellerThisSeason();
    _booksellerIsHere = booksellerDays.Contains(Game1.dayOfMonth);
    _booksellerIsVisited = false;
  }

  private bool ShouldDrawIcon()
  {
    return _booksellerIsHere && (!_booksellerIsVisited || !HideWhenVisited);
  }
  #endregion
}
