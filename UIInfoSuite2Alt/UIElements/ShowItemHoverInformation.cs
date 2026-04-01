using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Tools;
using UIInfoSuite2Alt.Compatibility;
using UIInfoSuite2Alt.Compatibility.Helpers;
using UIInfoSuite2Alt.Infrastructure;
using UIInfoSuite2Alt.Infrastructure.Extensions;
using UIInfoSuite2Alt.Infrastructure.Helpers;
using Object = StardewValley.Object;

namespace UIInfoSuite2Alt.UIElements;

internal class ShowItemHoverInformation : IDisposable
{
  private readonly ClickableTextureComponent _bundleIcon = new(
    new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
    Game1.mouseCursors,
    new Rectangle(331, 374, 15, 14),
    3f
  );

  private readonly IModHelper _helper;

  private readonly Dictionary<int, Color?> _bundleColorCache = new();
  private readonly PerScreen<Item?> _hoverItem = new();
  private readonly ClickableTextureComponent _museumIcon;

  private ClickableTextureComponent? _aquariumIcon;
  private bool _aquariumIconInitialized;

  private (Texture2D texture, Rectangle sourceRect)? _ubIconOverride;

  private static readonly Rectangle CollectionsTabSourceRect = new(640, 81, 16, 16);

  private LibraryMuseum? _libraryMuseum;

  public ShowItemHoverInformation(IModHelper helper)
  {
    _helper = helper;

    NPC? gunther = Game1.getCharacterFromName("Gunther");
    if (gunther == null)
    {
      ModEntry.MonitorObject.Log(
        "ShowItemHoverInformation: Gunther not found, creating fallback NPC",
        LogLevel.Warn
      );
      gunther = new NPC
      {
        Name = "Gunther",
        Age = 0,
        Sprite = new AnimatedSprite("Characters\\Gunther"),
      };
    }

    _museumIcon = new ClickableTextureComponent(
      new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
      gunther.Sprite.Texture,
      gunther.GetHeadShot(),
      Game1.pixelZoom
    );

    AquariumHelper.Initialize(helper);
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool showItemHoverInformation)
  {
    _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;
    _helper.Events.Display.RenderedHud -= OnRenderedHud;
    _helper.Events.Display.Rendering -= OnRendering;

    if (showItemHoverInformation)
    {
      _libraryMuseum = Game1.getLocationFromName("ArchaeologyHouse") as LibraryMuseum;

      _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
      _helper.Events.Display.RenderedHud += OnRenderedHud;
      _helper.Events.Display.Rendering += OnRendering;
    }
  }

  private ClickableTextureComponent? GetAquariumIcon()
  {
    if (_aquariumIconInitialized)
    {
      return _aquariumIcon;
    }

    _aquariumIconInitialized = true;
    if (!AquariumHelper.IsModLoaded)
    {
      return null;
    }

    try
    {
      Texture2D curatorTexture = _helper.GameContent.Load<Texture2D>("Characters/Curator");
      _aquariumIcon = new ClickableTextureComponent(
        new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
        curatorTexture,
        new Rectangle(0, 1, 16, 16),
        Game1.pixelZoom
      );
    }
    catch (Exception)
    {
      ModEntry.MonitorObject.Log(
        "ShowItemHoverInformation: Stardew Aquarium installed but Curator sprite load failed",
        LogLevel.Warn
      );
    }

    return _aquariumIcon;
  }

  private void OnRendering(object? sender, EventArgs e)
  {
    _hoverItem.Value = Tools.GetHoveredItem();
  }

  private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
  {
    if (!Game1.displayHUD || Game1.eventUp || Game1.isFestival())
    {
      return;
    }

    if (Game1.activeClickableMenu == null)
    {
      DrawAdvancedTooltip(e.SpriteBatch);
    }
  }

  [EventPriority(EventPriority.Low)]
  private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
  {
    if (Game1.activeClickableMenu != null)
    {
      DrawAdvancedTooltip(e.SpriteBatch);
    }
  }

  private void DrawAdvancedTooltip(SpriteBatch spriteBatch)
  {
    // When Informant is installed, its item decorators handle sell price, museum,
    // shipping, and bundle icons directly on the vanilla tooltip - skip our overlay entirely.
    if (InformantHelper.IsLoaded)
    {
      return;
    }

    if (
      _hoverItem.Value != null
      && !(_hoverItem.Value is MeleeWeapon weapon && weapon.isScythe())
      && _hoverItem.Value is not FishingRod
    )
    {
      var hoveredObject = _hoverItem.Value as Object;

      int itemPrice = Tools.GetSellToStorePrice(_hoverItem.Value);

      var stackPrice = 0;
      if (itemPrice > 0 && _hoverItem.Value.Stack > 1)
      {
        stackPrice = itemPrice * _hoverItem.Value.Stack;
      }

      int cropPrice = Tools.GetHarvestPrice(_hoverItem.Value);

      bool notDonatedYet = _libraryMuseum?.isItemSuitableForDonation(_hoverItem.Value) ?? false;

      bool notDonatedToAquarium = AquariumHelper.IsUndonatedAquariumFish(_hoverItem.Value);

      bool notShippedYet =
        hoveredObject != null
        && hoveredObject.countsForShippedCollection()
        && !Game1.player.basicShipped.ContainsKey(hoveredObject.ItemId)
        && hoveredObject.Type != "Fish"
        && hoveredObject.Category != Object.skillBooksCategory;

      string? requiredBundleName = null;
      Color? bundleColor = null;
      int bundleId = -1;
      if (hoveredObject != null)
      {
        BundleRequiredItem? bundleDisplayData = BundleHelper.GetBundleItemIfNotDonated(
          hoveredObject
        );
        if (bundleDisplayData != null)
        {
          requiredBundleName = bundleDisplayData.Name;
          bundleId = bundleDisplayData.Id;

          if (!_bundleColorCache.TryGetValue(bundleDisplayData.Id, out bundleColor))
          {
            bundleColor = BundleHelper
              .GetRealColorFromIndex(bundleDisplayData.Id)
              ?.Desaturate(0.35f);
            _bundleColorCache[bundleDisplayData.Id] = bundleColor;
          }
        }
        else
        {
          // Check Unlockable Bundles (lower priority than CC)
          UbBundleRequiredItem? ubData = UnlockableBundleHelper.GetBundleItemIfNotDonated(
            hoveredObject
          );
          if (ubData != null)
          {
            requiredBundleName = ubData.BundleName;
            bundleColor = ParseUbColor(ubData.ColorHex);
            _ubIconOverride = ResolveUbIcon(ubData);
          }
        }
      }

      var drawPositionOffset = new Vector2();
      int windowWidth,
        windowHeight;

      var bundleHeaderWidth = 0;
      if (!string.IsNullOrEmpty(requiredBundleName))
      {
        bundleHeaderWidth = 36 + (int)Game1.smallFont.MeasureString(requiredBundleName).X;
      }

      var itemTextWidth = (int)Game1.smallFont.MeasureString(itemPrice.ToString()).X;
      var stackTextWidth = (int)Game1.smallFont.MeasureString(stackPrice.ToString()).X;
      var cropTextWidth = (int)Game1.smallFont.MeasureString(cropPrice.ToString()).X;
      var minTextWidth = (int)Game1.smallFont.MeasureString("000").X;
      int largestTextWidth =
        76
        + Math.Max(minTextWidth, Math.Max(stackTextWidth, Math.Max(itemTextWidth, cropTextWidth)));
      windowWidth = Math.Max(bundleHeaderWidth, largestTextWidth);

      windowHeight = 20 + 16;
      if (itemPrice > 0)
      {
        windowHeight += 40;
      }

      if (stackPrice > 0)
      {
        windowHeight += 40;
      }

      if (cropPrice > 0)
      {
        windowHeight += 40;
      }

      if (!string.IsNullOrEmpty(requiredBundleName))
      {
        windowHeight += 4;
        drawPositionOffset.Y += 4;
      }

      // Min window dimensions
      windowHeight = Math.Max(windowHeight, 40);
      windowWidth = Math.Max(windowWidth, 40);

      int windowY = Game1.getMouseY() + 20;
      int windowX = Game1.getMouseX() - 25 - windowWidth;

      // Avoid overlapping Ferngill Simple Economy tooltip
      if (
        hoveredObject != null
        && ApiManager.GetApi(ModCompat.FerngillEconomy, out IFerngillSimpleEconomyApi? fseApi)
        && fseApi.IsLoaded()
        && fseApi.ItemIsInEconomy(hoveredObject)
      )
      {
        windowX -= 270;
      }

      // Adjust overflow
      Rectangle safeArea = Utility.getSafeArea();

      if (windowY + windowHeight > safeArea.Bottom)
      {
        windowY = safeArea.Bottom - windowHeight;
      }

      if (Game1.getMouseX() + 300 > safeArea.Right)
      {
        windowX = safeArea.Right - 350 - windowWidth;
      }
      else if (windowX < safeArea.Left)
      {
        windowX = Game1.getMouseX() + 350;
      }

      var windowPos = new Vector2(windowX, windowY);
      Vector2 drawPosition = windowPos + new Vector2(16, 20) + drawPositionOffset;

      // 32x40 icon cells, small font cap height 18 offset (2,6)
      var rowHeight = 40;
      var iconCenterOffset = new Vector2(16, 20);
      var textOffset = new Vector2(32 + 4, (rowHeight - 18) / 2 - 6);

      if (
        itemPrice > 0
        || stackPrice > 0
        || cropPrice > 0
        || !string.IsNullOrEmpty(requiredBundleName)
        || notDonatedYet
        || notDonatedToAquarium
        || notShippedYet
      )
      {
        IClickableMenu.drawTextureBox(
          spriteBatch,
          Game1.menuTexture,
          new Rectangle(0, 256, 60, 60),
          (int)windowPos.X,
          (int)windowPos.Y,
          windowWidth,
          windowHeight,
          Color.White
        );
      }

      if (itemPrice > 0)
      {
        spriteBatch.Draw(
          Game1.debrisSpriteSheet,
          drawPosition + iconCenterOffset,
          Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
          Color.White,
          0,
          new Vector2(8, 8),
          Game1.pixelZoom,
          SpriteEffects.None,
          0.95f
        );

        DrawSmallTextWithShadow(spriteBatch, itemPrice.ToString(), drawPosition + textOffset);

        drawPosition.Y += rowHeight;
      }

      if (stackPrice > 0)
      {
        var overlapOffset = new Vector2(0, 10);
        spriteBatch.Draw(
          Game1.debrisSpriteSheet,
          drawPosition + iconCenterOffset - overlapOffset / 2,
          Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
          Color.White,
          0,
          new Vector2(8, 8),
          Game1.pixelZoom,
          SpriteEffects.None,
          0.95f
        );
        spriteBatch.Draw(
          Game1.debrisSpriteSheet,
          drawPosition + iconCenterOffset + overlapOffset / 2,
          Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
          Color.White,
          0,
          new Vector2(8, 8),
          Game1.pixelZoom,
          SpriteEffects.None,
          0.95f
        );

        DrawSmallTextWithShadow(spriteBatch, stackPrice.ToString(), drawPosition + textOffset);

        drawPosition.Y += rowHeight;
      }

      if (cropPrice > 0)
      {
        spriteBatch.Draw(
          Game1.mouseCursors,
          drawPosition + iconCenterOffset,
          new Rectangle(60, 428, 10, 10),
          Color.White,
          0.0f,
          new Vector2(5, 5),
          Game1.pixelZoom * 0.75f,
          SpriteEffects.None,
          0.85f
        );

        DrawSmallTextWithShadow(spriteBatch, cropPrice.ToString(), drawPosition + textOffset);
      }

      if (notDonatedYet)
      {
        spriteBatch.Draw(
          _museumIcon.texture,
          windowPos + new Vector2(2, windowHeight + 8),
          _museumIcon.sourceRect,
          Color.White,
          0f,
          new Vector2(_museumIcon.sourceRect.Width / 2, _museumIcon.sourceRect.Height),
          2,
          SpriteEffects.None,
          0.86f
        );
      }

      if (notDonatedToAquarium && GetAquariumIcon() is { } aquariumIcon)
      {
        spriteBatch.Draw(
          aquariumIcon.texture,
          windowPos + new Vector2(2, windowHeight + 8),
          aquariumIcon.sourceRect,
          Color.White,
          0f,
          new Vector2(aquariumIcon.sourceRect.Width / 2, aquariumIcon.sourceRect.Height),
          2,
          SpriteEffects.None,
          0.86f
        );
      }

      if (!string.IsNullOrEmpty(requiredBundleName))
      {
        // Bundle icon + banner
        DrawBundleBanner(
          spriteBatch,
          requiredBundleName,
          bundleId,
          windowPos + new Vector2(-7, -17),
          windowWidth,
          bundleColor,
          _ubIconOverride
        );
        _ubIconOverride = null;
      }

      if (notShippedYet)
      {
        // Collections tab icon on right side
        DrawCollectionsTab(spriteBatch, windowPos + new Vector2(windowWidth - 4, 12), 2f);
      }
    }
  }

  private void DrawSmallTextWithShadow(SpriteBatch b, string text, Vector2 position)
  {
    b.DrawString(Game1.smallFont, text, position + new Vector2(2, 2), Game1.textShadowColor);
    b.DrawString(Game1.smallFont, text, position, Game1.textColor);
  }

  private void DrawBundleBanner(
    SpriteBatch spriteBatch,
    string bundleName,
    int bundleId,
    Vector2 position,
    int windowWidth,
    Color? color = null,
    (Texture2D texture, Rectangle sourceRect)? iconOverride = null
  )
  {
    Color drawColor = color ?? Color.Crimson;

    var bundleBannerX = (int)position.X;
    int bundleBannerY = (int)position.Y + 2;
    var cellCount = 48;
    var solidCells = 10;
    int cellWidth = windowWidth / cellCount;
    for (var cell = 0; cell < cellCount; ++cell)
    {
      float fadeAmount =
        0.97f - (cell < solidCells ? 0 : 1.0f * (cell - solidCells) / (cellCount - solidCells));
      spriteBatch.Draw(
        Game1.staminaRect,
        new Rectangle(bundleBannerX + cell * cellWidth, bundleBannerY, cellWidth, 32),
        drawColor * fadeAmount
      );
    }

    // Draw per-bundle icon at 1:1 pixel scale, fall back to generic scroll if unavailable
    var spriteInfo = iconOverride ?? BundleHelper.GetBundleSpriteInfo(bundleId);
    float iconWidth;
    const int iconDisplaySize = 32;
    if (spriteInfo is var (texture, sourceRect))
    {
      var iconPos = new Point((int)position.X, (int)position.Y + 2);

      // filled rectangle behind the icon acts as a 2px border in bundle color and 1px shadow border
      spriteBatch.Draw(
        Game1.staminaRect,
        new Rectangle(iconPos.X - 2, iconPos.Y - 2, iconDisplaySize + 4, iconDisplaySize + 4),
        drawColor
      );
      spriteBatch.Draw(
        Game1.staminaRect,
        new Rectangle(iconPos.X - 1, iconPos.Y - 1, iconDisplaySize + 2, iconDisplaySize + 2),
        Color.Black * 0.3f
      );
      // incase the icons are smaller then expected triangle fill background with drawColor
      spriteBatch.Draw(
        Game1.staminaRect,
        new Rectangle(iconPos.X, iconPos.Y, iconDisplaySize, iconDisplaySize),
        drawColor
      );

      spriteBatch.Draw(
        texture,
        new Rectangle((int)position.X, (int)position.Y + 2, iconDisplaySize, iconDisplaySize),
        sourceRect,
        Color.White,
        0f,
        Vector2.Zero,
        SpriteEffects.None,
        0.86f
      );

      // Small overlay icon (bottom-right corner) - CC icon or UB book
      Texture2D? overlayTexture = null;
      Rectangle overlayRect;
      int overlayW,
        overlayH;

      if (iconOverride == null)
      {
        // CC bundle - use community center icon from cursors
        overlayW = 13;
        overlayH = 11;
        overlayRect = new Rectangle(332, 375, overlayW, overlayH);
        overlayTexture = Game1.mouseCursors;
      }
      else
      {
        // UB bundle - load book icon from UB's content, stretched to CC overlay size
        try
        {
          overlayTexture = Game1.content.Load<Texture2D>("UnlockableBundles/UI/OverviewBookOpen");
          overlayRect = new Rectangle(0, 0, overlayTexture.Width, overlayTexture.Height);
          overlayW = 13;
          overlayH = 11;
        }
        catch (Exception ex)
        {
          ModEntry.MonitorObject.Log(
            $"ShowItemHoverInformation: failed to load UB overlay texture, {ex.Message}",
            LogLevel.Trace
          );
          overlayW = 0;
          overlayH = 0;
          overlayRect = Rectangle.Empty;
          overlayTexture = null;
        }
      }

      if (overlayTexture != null)
      {
        int overlayX = iconPos.X + iconDisplaySize - overlayW;
        int overlayY = iconPos.Y + iconDisplaySize - overlayH;

        spriteBatch.Draw(
          overlayTexture,
          new Rectangle(overlayX, overlayY, overlayW, overlayH),
          overlayRect,
          Color.White,
          0f,
          Vector2.Zero,
          SpriteEffects.None,
          1f
        );
      }

      iconWidth = iconDisplaySize;
    }
    else
    {
      spriteBatch.Draw(
        Game1.mouseCursors,
        new Rectangle((int)position.X, (int)position.Y + 5, iconDisplaySize, iconDisplaySize),
        _bundleIcon.sourceRect,
        Color.White,
        0f,
        Vector2.Zero,
        SpriteEffects.None,
        0.86f
      );
      iconWidth = iconDisplaySize;
    }

    var textPos = position + new Vector2(iconWidth + 3, 3);
    spriteBatch.DrawString(
      Game1.smallFont,
      bundleName,
      textPos + new Vector2(1, 1),
      Color.Black * 0.3f
    );
    spriteBatch.DrawString(Game1.smallFont, bundleName, textPos, Color.White);
  }

  private static (Texture2D texture, Rectangle sourceRect)? ResolveUbIcon(
    UbBundleRequiredItem ubData
  )
  {
    // Try UB's BundleIconAsset first (complete icon texture)
    if (!string.IsNullOrEmpty(ubData.IconTexturePath))
    {
      try
      {
        Texture2D texture = Game1.content.Load<Texture2D>(ubData.IconTexturePath);
        return (texture, new Rectangle(0, 0, texture.Width, texture.Height));
      }
      catch (Exception ex)
      {
        // BundleIconAsset was set but failed - show error scroll (CP logs the details)
        ModEntry.MonitorObject.Log(
          $"ShowItemHoverInformation: failed to load UB BundleIconAsset '{ubData.IconTexturePath}', {ex.Message}",
          LogLevel.Trace
        );
        return (Game1.mouseCursors, new Rectangle(208, 272, 32, 32));
      }
    }
    else
    {
      ModEntry.MonitorObject.LogOnce(
        $"ShowItemHoverInformation: no BundleIconAsset for UB bundle '{ubData.BundleName}', using fallback",
        LogLevel.Trace
      );
    }

    // No BundleIconAsset set - use UB's book icon, then Cursors scroll as last resort
    try
    {
      Texture2D bookIcon = Game1.content.Load<Texture2D>("UnlockableBundles/UI/BundleOverviewIcon");
      return (bookIcon, new Rectangle(0, 0, bookIcon.Width, bookIcon.Height));
    }
    catch (Exception ex)
    {
      ModEntry.MonitorObject.Log(
        $"ShowItemHoverInformation: failed to load UB book icon, {ex.Message}",
        LogLevel.Trace
      );
      return (Game1.mouseCursors, new Rectangle(208, 272, 32, 32));
    }
  }

  private static readonly Color DefaultUbBundleColor = new(0xDC, 0x7B, 0x05);

  private static Color ParseUbColor(string? hex)
  {
    if (string.IsNullOrEmpty(hex))
    {
      return DefaultUbBundleColor;
    }

    try
    {
      ReadOnlySpan<char> span = hex.AsSpan().TrimStart('#');
      if (span.Length >= 6)
      {
        int r = int.Parse(span[..2], System.Globalization.NumberStyles.HexNumber);
        int g = int.Parse(span[2..4], System.Globalization.NumberStyles.HexNumber);
        int b = int.Parse(span[4..6], System.Globalization.NumberStyles.HexNumber);
        return new Color(r, g, b);
      }
    }
    catch (Exception ex)
    {
      ModEntry.MonitorObject.Log(
        $"ShowItemHoverInformation: invalid UB bundle color hex '{hex}', {ex.Message}",
        LogLevel.Trace
      );
    }

    return DefaultUbBundleColor;
  }

  private static void DrawCollectionsTab(SpriteBatch b, Vector2 position, float scale)
  {
    b.Draw(
      Game1.mouseCursors,
      position,
      CollectionsTabSourceRect,
      Color.White,
      0f,
      Vector2.Zero,
      scale,
      SpriteEffects.FlipHorizontally,
      0.86f
    );
  }
}
