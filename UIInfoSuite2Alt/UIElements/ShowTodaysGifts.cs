using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2Alt.Compatibility;

namespace UIInfoSuite2Alt.UIElements;

internal class ShowTodaysGifts : IDisposable
{
  #region Properties
  private SocialPage? _socialPage;
  private readonly IModHelper _helper;
  #endregion

  #region Lifecycle
  public ShowTodaysGifts(IModHelper helper)
  {
    _helper = helper;
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool showTodaysGift)
  {
    _helper.Events.Display.MenuChanged -= OnMenuChanged;
    _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;

    if (showTodaysGift)
    {
      _helper.Events.Display.MenuChanged += OnMenuChanged;
      _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
    }
  }
  #endregion

  #region Event subscriptions
  private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
  {
    if (_socialPage == null)
    {
      GetSocialPage();
      return;
    }

    IClickableMenu? menu = Game1.activeClickableMenu;
    if (GameMenuHelper.IsTab(menu, GameMenu.socialTab))
    {
      DrawTodaysGifts();

      string hoverText = GameMenuHelper.GetHoverText(menu);
      if (!string.IsNullOrEmpty(hoverText))
      {
        IClickableMenu.drawHoverText(Game1.spriteBatch, hoverText, Game1.smallFont);
      }
    }
  }

  private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
  {
    GetSocialPage();
  }
  #endregion

  #region Logic
  private void GetSocialPage()
  {
    IClickableMenu? menu = Game1.activeClickableMenu;
    if (!GameMenuHelper.IsGameMenu(menu))
    {
      return;
    }

    SocialPage? page = GameMenuHelper.FindPage<SocialPage>(menu);
    if (page != null)
    {
      _socialPage = page;
    }
  }

  private void DrawTodaysGifts()
  {
    if (_socialPage == null)
    {
      return;
    }

    var yOffset = 25;

    for (int i = _socialPage.slotPosition; i < _socialPage.slotPosition + 5 && i < _socialPage.SocialEntries.Count; ++i)
    {
      int yPosition = Game1.activeClickableMenu.yPositionOnScreen + 130 + yOffset;
      yOffset += 112;
      string internalName = _socialPage.SocialEntries[i].InternalName;
      if (Game1.player.friendshipData.TryGetValue(internalName, out Friendship? data) &&
          data.GiftsToday != 0 &&
          data.GiftsThisWeek < 2)
      {
        Game1.spriteBatch.Draw(
          Game1.mouseCursors,
          new Vector2(_socialPage.xPositionOnScreen + 384 + 296 + 4, yPosition + 6),
          new Rectangle(106, 442, 9, 9),
          Color.LightGray,
          0.0f,
          Vector2.Zero,
          3f,
          SpriteEffects.None,
          0.22f
        );
      }
    }
  }
  #endregion
}
