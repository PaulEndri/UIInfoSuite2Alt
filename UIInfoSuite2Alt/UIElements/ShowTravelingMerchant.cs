using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Internal;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using UIInfoSuite2Alt.Infrastructure;
using UIInfoSuite2Alt.Infrastructure.Helpers;

namespace UIInfoSuite2Alt.UIElements;

public class ShowTravelingMerchant : IDisposable
{
  #region Properties
  private bool _travelingMerchantIsHere;
  private bool _travelingMerchantIsVisited;
  private bool _merchantHasBundleItems;
  private readonly List<string> _bundleItemNames = new();
  private ClickableTextureComponent _travelingMerchantIcon = null!;

  private bool Enabled { get; set; }
  private bool HideWhenVisited { get; set; }
  private bool ShowBundleIcon { get; set; }
  private bool ShowBundleItemNames { get; set; }

  private readonly IModHelper _helper;
  #endregion


  #region Lifecycle
  public ShowTravelingMerchant(IModHelper helper)
  {
    _helper = helper;
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool showTravelingMerchant)
  {
    Enabled = showTravelingMerchant;

    _helper.Events.Display.RenderingHud -= OnRenderingHud;
    _helper.Events.Display.RenderedHud -= OnRenderedHud;
    _helper.Events.GameLoop.DayStarted -= OnDayStarted;
    _helper.Events.Display.MenuChanged -= OnMenuChanged;

    if (showTravelingMerchant)
    {
      UpdateTravelingMerchant();
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

  public void ToggleShowBundleIconOption(bool showBundleIcon)
  {
    ShowBundleIcon = showBundleIcon;
    ToggleOption(Enabled);
  }

  public void ToggleShowBundleItemNamesOption(bool showBundleItemNames)
  {
    ShowBundleItemNames = showBundleItemNames;
    ToggleOption(Enabled);
  }
  #endregion


  #region Event subscriptions
  private void OnDayStarted(object? sender, EventArgs e)
  {
    UpdateTravelingMerchant();
  }

  private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
  {
    if (e.NewMenu is ShopMenu menu && menu.forSale.Any(s => !(s is Hat)) && Game1.currentLocation.Name == "Forest")
    {
      _travelingMerchantIsVisited = true;
      _merchantHasBundleItems = false;
      _bundleItemNames.Clear();
    }
  }

  private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
  {
    // Draw icon
    if (UIElementUtils.IsRenderingNormally() && ShouldDrawIcon())
    {
      Point iconPosition = IconHandler.Handler.GetNewIconPosition();
      _travelingMerchantIcon = new ClickableTextureComponent(
        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
        Game1.mouseCursors,
        new Rectangle(192, 1411, 20, 20),
        2f
      );
      _travelingMerchantIcon.draw(Game1.spriteBatch);

      // Draw bundle overlay icon at bottom-right corner
      if (_merchantHasBundleItems && ShowBundleIcon)
      {
        Game1.spriteBatch.Draw(
          Game1.mouseCursors,
          new Vector2(iconPosition.X + 18, iconPosition.Y + 18),
          new Rectangle(331, 374, 15, 14),
          Color.White,
          0f,
          Vector2.Zero,
          1.6f,
          SpriteEffects.None,
          1f
        );
      }
    }
  }

  private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
  {
    // Show text on hover
    if (ShouldDrawIcon() && (_travelingMerchantIcon?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false))
    {
      string hoverText = I18n.TravelingMerchantIsInTown();

      if (_merchantHasBundleItems && ShowBundleIcon)
      {
        hoverText += "\n" + I18n.TravelingMerchantHasBundleItem();

        if (ShowBundleItemNames && _bundleItemNames.Count > 0)
        {
          hoverText += "\n" + string.Join(", ", _bundleItemNames);
        }
      }

      IClickableMenu.drawHoverText(Game1.spriteBatch, hoverText, Game1.dialogueFont);
    }
  }
  #endregion


  #region Logic
  private void UpdateTravelingMerchant()
  {
    _travelingMerchantIsHere = ((Forest)Game1.getLocationFromName(nameof(Forest))).ShouldTravelingMerchantVisitToday();
    _travelingMerchantIsVisited = false;
    _merchantHasBundleItems = false;
    _bundleItemNames.Clear();

    if (_travelingMerchantIsHere)
    {
      CheckMerchantForBundleItems();
    }

    // // DEBUG: Force bundle indicator for testing (remove before release)
    // _merchantHasBundleItems = true;
    // _bundleItemNames.Clear();
    // _bundleItemNames.Add("Caviar");
  }

  private void CheckMerchantForBundleItems()
  {
    try
    {
      Dictionary<ISalable, ItemStockInformation> stock = ShopBuilder.GetShopStock("Traveler");
      _bundleItemNames.Clear();

      foreach (ISalable salable in stock.Keys)
      {
        if (salable is Item item && BundleHelper.GetBundleItemIfNotDonated(item) != null)
        {
          _bundleItemNames.Add(item.DisplayName);
        }
      }

      _merchantHasBundleItems = _bundleItemNames.Count > 0;
    }
    catch (Exception e)
    {
      ModEntry.MonitorObject.Log("Failed to check merchant stock for bundle items: " + e.Message, LogLevel.Warn);
      _merchantHasBundleItems = false;
    }
  }

  private bool ShouldDrawIcon()
  {
    return _travelingMerchantIsHere && (!_travelingMerchantIsVisited || !HideWhenVisited);
  }
  #endregion
}
