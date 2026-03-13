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
using UIInfoSuite2Alt.Infrastructure.Extensions;
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
  private int _bundlePulseTimer;
  private int _bundlePulseDelay;

  private bool _rsvIsLoaded;
  private bool _rsvMerchantIsHere;
  private bool _rsvMerchantIsVisited;
  private Texture2D? _rsvIconTexture;
  private const string RsvModId = "Rafseazz.RidgesideVillage";
  private const string RsvMerchantLocation = "Custom_Ridgeside_RSVTheHike";
  private const float RsvHueShift = -50f;

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
    _helper.Events.GameLoop.DayStarted -= OnDayStarted;
    _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
    _helper.Events.Display.MenuChanged -= OnMenuChanged;

    if (showTravelingMerchant)
    {
      _rsvIsLoaded = _helper.ModRegistry.IsLoaded(RsvModId);
      UpdateTravelingMerchant();
      _helper.Events.Display.RenderingHud += OnRenderingHud;
      _helper.Events.GameLoop.DayStarted += OnDayStarted;
      _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
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
    if (e.NewMenu is ShopMenu menu && menu.forSale.Any(s => !(s is Hat)))
    {
      string locationName = Game1.currentLocation.Name;

      if (locationName == "Forest")
      {
        _travelingMerchantIsVisited = true;
        _merchantHasBundleItems = false;
        _bundleItemNames.Clear();
      }
      else if (locationName == RsvMerchantLocation)
      {
        _rsvMerchantIsVisited = true;
        _merchantHasBundleItems = false;
        _bundleItemNames.Clear();
      }
    }
  }

  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (!_merchantHasBundleItems || !ShowBundleIcon)
    {
      return;
    }

    int elapsed = (int)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;

    if (_bundlePulseTimer > 0)
    {
      _bundlePulseTimer -= elapsed;
    }
    else if (_bundlePulseDelay > 0)
    {
      _bundlePulseDelay -= elapsed;
    }
    else
    {
      _bundlePulseTimer = 1000;
      _bundlePulseDelay = 3000;
    }
  }

  private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
  {
    if (UIElementUtils.IsRenderingNormally() && ShouldDrawIcon())
    {
      IconHandler.Handler.EnqueueIcon(
        "TravelingMerchant",
        (batch, pos) =>
        {
          bool useRsvIcon = _rsvMerchantIsHere && (!_rsvMerchantIsVisited || !HideWhenVisited)
            && (!_travelingMerchantIsHere || _travelingMerchantIsVisited);
          Texture2D iconTexture = useRsvIcon ? GetRsvIconTexture() : Game1.mouseCursors;
          Rectangle iconSource = useRsvIcon
            ? new Rectangle(0, 0, 20, 20)
            : new Rectangle(192, 1411, 20, 20);

          _travelingMerchantIcon = new ClickableTextureComponent(
            new Rectangle(pos.X, pos.Y, 40, 40),
            iconTexture,
            iconSource,
            2f
          );
          _travelingMerchantIcon.draw(batch);

          if (_merchantHasBundleItems && ShowBundleIcon)
          {
            float baseScale = 1.6f;
            float scale = baseScale;
            Vector2 shake = Vector2.Zero;

            if (_bundlePulseTimer > 0)
            {
              float pulseScale = 1f / (Math.Max(300f, Math.Abs(_bundlePulseTimer % 1000 - 500)) / 500f);
              scale = baseScale * pulseScale;
              if (pulseScale > 1f)
              {
                shake = new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
              }
            }

            batch.Draw(
              Game1.mouseCursors,
              new Vector2(pos.X + 27 + 2.5f * baseScale, pos.Y + 11 + 7f * baseScale) + shake,
              new Rectangle(403, 496, 5, 14),
              Color.White,
              0f,
              new Vector2(2.5f, 7f),
              scale,
              SpriteEffects.None,
              1f
            );
          }
        },
        batch =>
        {
          if (_travelingMerchantIcon?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false)
          {
            var lines = new List<string>();

            if (_travelingMerchantIsHere && (!_travelingMerchantIsVisited || !HideWhenVisited))
            {
              lines.Add(I18n.TravelingMerchantIsInTown());

              if (_merchantHasBundleItems && ShowBundleIcon)
              {
                lines.Add(I18n.TravelingMerchantHasBundleItem());

                if (ShowBundleItemNames && _bundleItemNames.Count > 0)
                {
                  lines.Add(string.Join(", ", _bundleItemNames));
                }
              }
            }

            if (_rsvMerchantIsHere && (!_rsvMerchantIsVisited || !HideWhenVisited))
            {
              lines.Add(I18n.RsvTravelingMerchantIsAtHike());

              if (!_travelingMerchantIsHere && _merchantHasBundleItems && ShowBundleIcon)
              {
                lines.Add(I18n.TravelingMerchantHasBundleItem());

                if (ShowBundleItemNames && _bundleItemNames.Count > 0)
                {
                  lines.Add(string.Join(", ", _bundleItemNames));
                }
              }
            }

            IClickableMenu.drawHoverText(batch, string.Join("\n", lines), Game1.dialogueFont);
          }
        }
      );
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

    _rsvMerchantIsHere = _rsvIsLoaded && Game1.dayOfMonth % 7 == 3;
    _rsvMerchantIsVisited = false;

    if (_travelingMerchantIsHere || _rsvMerchantIsHere)
    {
      CheckMerchantForBundleItems();
    }
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
    bool vanillaVisible = _travelingMerchantIsHere && (!_travelingMerchantIsVisited || !HideWhenVisited);
    bool rsvVisible = _rsvMerchantIsHere && (!_rsvMerchantIsVisited || !HideWhenVisited);
    return vanillaVisible || rsvVisible;
  }

  private Texture2D GetRsvIconTexture()
  {
    if (_rsvIconTexture == null)
    {
      var sourceRect = new Rectangle(192, 1411, 20, 20);
      var pixels = new Color[sourceRect.Width * sourceRect.Height];
      Game1.mouseCursors.GetData(0, sourceRect, pixels, 0, pixels.Length);

      for (int i = 0; i < pixels.Length; i++)
      {
        if (pixels[i].A > 0)
        {
          pixels[i] = pixels[i].ShiftHue(RsvHueShift);
        }
      }

      _rsvIconTexture = new Texture2D(Game1.graphics.GraphicsDevice, sourceRect.Width, sourceRect.Height);
      _rsvIconTexture.SetData(pixels);
    }

    return _rsvIconTexture;
  }
  #endregion
}
