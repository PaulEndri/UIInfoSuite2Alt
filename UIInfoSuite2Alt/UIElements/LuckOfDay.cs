using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2Alt.Infrastructure;

namespace UIInfoSuite2Alt.UIElements;

internal class LuckOfDay : IDisposable
{
  #region Properties
  private const int CloverFrameSize = 26;
  private const float CloverScale = Game1.pixelZoom / 2.6f;

  private readonly PerScreen<string> _hoverText = new(() => string.Empty);
  private readonly PerScreen<int> _cloverFrame = new(() => 4);
  private readonly PerScreen<Color> _diceColor = new(() => new Color(Color.White.ToVector4()));

  private readonly Texture2D _cloverTexture;
  private readonly Texture2D _tvLuckTexture;

  private readonly PerScreen<ClickableTextureComponent> _icon;

  private readonly IModHelper _helper;

  private const int IconStyleDice = 1;
  private const int IconStyleTvFortune = 2;
  private const int TvFrameWidth = 42;
  private const int TvFrameHeight = 28;
  private const float TvIconScale = 1.5f;

  private readonly PerScreen<int> _tvFrame = new(() => 3);

  private bool Enabled { get; set; }
  private bool ShowExactValue { get; set; }
  private bool RequireTv { get; set; }
  private int IconStyle { get; set; }

  // Classic dice icon colors
  private static readonly Color Luck1Color = new(87, 255, 106, 255);
  private static readonly Color Luck2Color = new(148, 255, 210, 255);
  private static readonly Color Luck3Color = new(246, 255, 145, 255);
  private static readonly Color Luck4Color = new(255, 255, 255, 255);
  private static readonly Color Luck5Color = new(255, 155, 155, 255);
  private static readonly Color Luck6Color = new(165, 165, 165, 204);
  #endregion

  #region Lifecycle
  public LuckOfDay(IModHelper helper)
  {
    _helper = helper;
    _cloverTexture = Texture2D.FromFile(
      Game1.graphics.GraphicsDevice,
      Path.Combine(helper.DirectoryPath, "assets", "clover_group.png")
    );
    _tvLuckTexture = Texture2D.FromFile(
      Game1.graphics.GraphicsDevice,
      Path.Combine(helper.DirectoryPath, "assets", "tv_luck.png")
    );
    _icon = new PerScreen<ClickableTextureComponent>(() => CreateIcon());
  }

  public void Dispose()
  {
    ToggleOption(false);
    _cloverTexture.Dispose();
    _tvLuckTexture.Dispose();
  }

  public void ToggleOption(bool showLuckOfDay)
  {
    Enabled = showLuckOfDay;

    _helper.Events.Player.Warped -= OnWarped;
    _helper.Events.Display.RenderingHud -= OnRenderingHud;
    _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

    if (showLuckOfDay)
    {
      AdjustIconXToBlackBorder();
      _helper.Events.Player.Warped += OnWarped;
      _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
      _helper.Events.Display.RenderingHud += OnRenderingHud;
    }
  }

  public void ToggleShowExactValueOption(bool showExactValue)
  {
    ShowExactValue = showExactValue;
    ToggleOption(Enabled);
  }

  public void ToggleRequireTvOption(bool requireTv)
  {
    RequireTv = requireTv;
    ToggleOption(Enabled);
  }

  public void SetIconStyle(int iconStyle)
  {
    IconStyle = iconStyle;
    AdjustIconXToBlackBorder();
  }
  #endregion

  #region Event subscriptions
  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    CalculateLuck(e);
  }

  private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
  {
    if (UIElementUtils.IsRenderingNormally() && (!RequireTv || TvChannelWatcher.HasWatchedFortune.Value))
    {
      switch (IconStyle)
      {
        case IconStyleDice:
          DrawClassicIcon();
          break;
        case IconStyleTvFortune:
          DrawTvIcon();
          break;
        default:
          DrawCloverIcon();
          break;
      }
    }
  }

  private void DrawCloverIcon()
  {
    IconHandler.Handler.EnqueueIcon(
      "Luck",
      (batch, pos) =>
      {
        ClickableTextureComponent icon = _icon.Value;
        icon.bounds.X = pos.X;
        icon.bounds.Y = pos.Y;
        icon.sourceRect = new Rectangle(_cloverFrame.Value * CloverFrameSize, 0, CloverFrameSize, CloverFrameSize);
        _icon.Value = icon;
        _icon.Value.draw(batch, Color.White * 0.9f, 1f);
      },
      batch =>
      {
        if (_icon.Value.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
        {
          IClickableMenu.drawHoverText(batch, _hoverText.Value, Game1.dialogueFont);
        }
      }
    );
  }

  private void DrawTvIcon()
  {
    int scaledW = (int)(TvFrameWidth * TvIconScale);
    int scaledH = (int)(TvFrameHeight * TvIconScale);

    IconHandler.Handler.EnqueueIcon(
      "Luck",
      (batch, pos) =>
      {
        var sourceRect = new Rectangle(_tvFrame.Value * TvFrameWidth, 0, TvFrameWidth, TvFrameHeight);
        var destRect = new Rectangle(pos.X, pos.Y, scaledW, scaledH);

        batch.Draw(_tvLuckTexture, destRect, sourceRect, Color.White * 0.9f);

        // Update icon bounds for hover detection
        ClickableTextureComponent icon = _icon.Value;
        icon.bounds = destRect;
        _icon.Value = icon;
      },
      batch =>
      {
        if (_icon.Value.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
        {
          IClickableMenu.drawHoverText(batch, _hoverText.Value, Game1.dialogueFont);
        }
      },
      scaledW
    );
  }

  private void DrawClassicIcon()
  {
    IconHandler.Handler.EnqueueIcon(
      "Luck",
      (batch, pos) =>
      {
        ClickableTextureComponent icon = _icon.Value;
        icon.bounds.X = pos.X;
        icon.bounds.Y = pos.Y;
        _icon.Value = icon;
        _icon.Value.draw(batch, _diceColor.Value, 1f);
      },
      batch =>
      {
        if (_icon.Value.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
        {
          IClickableMenu.drawHoverText(batch, _hoverText.Value, Game1.dialogueFont);
        }
      }
    );
  }
  #endregion

  #region Logic
  private void CalculateLuck(UpdateTickedEventArgs e)
  {
    if (e.IsMultipleOf(30)) // half second
    {
      double luck = Game1.player.DailyLuck;

      switch (luck)
      {
        // Min luck — includes shrine extremes
        case <= -0.075:
          _hoverText.Value = I18n.LuckStatus6();
          _cloverFrame.Value = 0;
          _diceColor.Value = Luck6Color;
          _tvFrame.Value = 0;
          break;
        // Very bad luck
        case < -0.07:
          _hoverText.Value = I18n.LuckStatus6();
          _cloverFrame.Value = 1;
          _diceColor.Value = Luck6Color;
          _tvFrame.Value = 1;
          break;
        // Bad luck
        case < -0.02:
          _hoverText.Value = I18n.LuckStatus5();
          _cloverFrame.Value = 2;
          _diceColor.Value = Luck5Color;
          _tvFrame.Value = 2;
          break;
        // Absolutely neutral
        case 0:
          _hoverText.Value = I18n.LuckStatus4();
          _cloverFrame.Value = 3;
          _diceColor.Value = Luck4Color;
          _tvFrame.Value = 3;
          break;
        // Near-neutral (non-zero, between -0.02 and +0.02)
        case <= 0.02:
          _hoverText.Value = I18n.LuckStatus3();
          _cloverFrame.Value = 4;
          _diceColor.Value = Luck3Color;
          _tvFrame.Value = 3;
          break;
        // Good luck
        case <= 0.07:
          _hoverText.Value = I18n.LuckStatus2();
          _cloverFrame.Value = 5;
          _diceColor.Value = Luck2Color;
          _tvFrame.Value = 4;
          break;
        // Very good luck
        case < 0.1:
          _hoverText.Value = I18n.LuckStatus1();
          _cloverFrame.Value = 6;
          _diceColor.Value = Luck1Color;
          _tvFrame.Value = 5;
          break;
        // Max luck — includes shrine extremes
        default:
          _hoverText.Value = I18n.LuckStatus1();
          _cloverFrame.Value = 7;
          _diceColor.Value = Luck1Color;
          _tvFrame.Value = 6;
          break;
      }

      // Rewrite the text, but keep the frame/color
      if (ShowExactValue)
      {
        _hoverText.Value = string.Format(I18n.DailyLuckValue(), Game1.player.DailyLuck.ToString("N3"));
      }
    }
  }

  private void OnWarped(object? sender, WarpedEventArgs e)
  {
    // adjust icon X to black border
    if (e.IsLocalPlayer)
    {
      AdjustIconXToBlackBorder();
    }
  }

  private void AdjustIconXToBlackBorder()
  {
    _icon.Value = CreateIcon();
  }

  private ClickableTextureComponent CreateIcon()
  {
    if (IconStyle == IconStyleDice)
    {
      return new ClickableTextureComponent(
        "",
        new Rectangle(Tools.GetWidthInPlayArea() - 134, 290, 10 * Game1.pixelZoom, 10 * Game1.pixelZoom),
        "",
        "",
        Game1.mouseCursors,
        new Rectangle(50, 428, 10, 14),
        Game1.pixelZoom
      );
    }

    if (IconStyle == IconStyleTvFortune)
    {
      int tvScaledW = (int)(TvFrameWidth * TvIconScale);
      int tvScaledH = (int)(TvFrameHeight * TvIconScale);
      return new ClickableTextureComponent(
        "",
        new Rectangle(Tools.GetWidthInPlayArea() - 134, 290, tvScaledW, tvScaledH),
        "",
        "",
        _tvLuckTexture,
        new Rectangle(_tvFrame.Value * TvFrameWidth, 0, TvFrameWidth, TvFrameHeight),
        TvIconScale
      );
    }

    int scaledSize = (int)(CloverFrameSize * CloverScale);
    return new ClickableTextureComponent(
      "",
      new Rectangle(Tools.GetWidthInPlayArea() - 134, 290, scaledSize, scaledSize),
      "",
      "",
      _cloverTexture,
      new Rectangle(_cloverFrame.Value * CloverFrameSize, 0, CloverFrameSize, CloverFrameSize),
      CloverScale
    );
  }
  #endregion
}
