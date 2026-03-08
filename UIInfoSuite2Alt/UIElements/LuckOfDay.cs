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

  private readonly Texture2D _cloverTexture;

  private readonly PerScreen<ClickableTextureComponent> _icon;

  private readonly IModHelper _helper;

  private bool Enabled { get; set; }
  private bool ShowExactValue { get; set; }
  private bool RequireTv { get; set; }
  #endregion

  #region Lifecycle
  public LuckOfDay(IModHelper helper)
  {
    _helper = helper;
    _cloverTexture = Texture2D.FromFile(
      Game1.graphics.GraphicsDevice,
      Path.Combine(helper.DirectoryPath, "assets", "clover_group.png")
    );
    _icon = new PerScreen<ClickableTextureComponent>(() => CreateIcon());
  }

  public void Dispose()
  {
    ToggleOption(false);
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
          break;
        // Very bad luck
        case < -0.07:
          _hoverText.Value = I18n.LuckStatus6();
          _cloverFrame.Value = 1;
          break;
        // Bad luck
        case < -0.02:
          _hoverText.Value = I18n.LuckStatus5();
          _cloverFrame.Value = 2;
          break;
        // Absolutely neutral
        case 0:
          _hoverText.Value = I18n.LuckStatus4();
          _cloverFrame.Value = 3;
          break;
        // Near-neutral (non-zero, between -0.02 and +0.02)
        case <= 0.02:
          _hoverText.Value = I18n.LuckStatus3();
          _cloverFrame.Value = 4;
          break;
        // Good luck
        case <= 0.07:
          _hoverText.Value = I18n.LuckStatus2();
          _cloverFrame.Value = 5;
          break;
        // Very good luck
        case < 0.1:
          _hoverText.Value = I18n.LuckStatus1();
          _cloverFrame.Value = 6;
          break;
        // Max luck — includes shrine extremes
        default:
          _hoverText.Value = I18n.LuckStatus1();
          _cloverFrame.Value = 7;
          break;
      }

      // Rewrite the text, but keep the frame
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
