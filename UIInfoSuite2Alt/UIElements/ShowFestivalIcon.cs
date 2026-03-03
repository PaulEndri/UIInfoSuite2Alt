using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData;
using StardewValley.Menus;
using StardewValley.TokenizableStrings;
using UIInfoSuite2Alt.Infrastructure;

namespace UIInfoSuite2Alt.UIElements;

internal class ShowFestivalIcon : IDisposable
{
  #region Properties
  private enum FestivalType { None, Regular, Passive, FishingDerby }

  private static readonly HashSet<string> FishingDerbyIds = new() { "TroutDerby", "SquidFest" };

  private readonly PerScreen<bool> _isToday = new();
  private readonly PerScreen<bool> _isTomorrow = new();
  private readonly PerScreen<FestivalType> _festivalType = new();
  private readonly PerScreen<string> _hoverText = new();

  private readonly Texture2D _billboardTexture;

  // Flag icon for regular festivals (from Billboard spritesheet)
  private static readonly Rectangle FlagSourceRect = new(1, 399, 13, 11);

  // Purple star icon for passive festivals (from Cursors spritesheet)
  private static readonly Rectangle StarSourceRect = new(346, 392, 8, 8);

  // Fishing derby icon (from Cursors_1_6 spritesheet)
  private static readonly Rectangle FishingDerbySourceRect = new(103, 2, 10, 11);

  private readonly PerScreen<ClickableTextureComponent> _festivalIcon = new(
    () => new ClickableTextureComponent(
      new Rectangle(0, 0, 40, 40),
      null,
      FlagSourceRect,
      40 / 13f
    )
  );

  private readonly IModHelper _helper;
  #endregion


  #region Life cycle
  public ShowFestivalIcon(IModHelper helper)
  {
    _helper = helper;
    _billboardTexture = helper.GameContent.Load<Texture2D>("LooseSprites/Billboard");
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool enabled)
  {
    _helper.Events.Display.RenderingHud -= OnRenderingHud;
    _helper.Events.Display.RenderedHud -= OnRenderedHud;
    _helper.Events.GameLoop.DayStarted -= OnDayStarted;

    if (enabled)
    {
      CheckForFestival();
      _helper.Events.Display.RenderingHud += OnRenderingHud;
      _helper.Events.Display.RenderedHud += OnRenderedHud;
      _helper.Events.GameLoop.DayStarted += OnDayStarted;
    }
  }
  #endregion


  #region Logic
  private void CheckForFestival()
  {
    _isToday.Value = false;
    _isTomorrow.Value = false;
    _festivalType.Value = FestivalType.None;
    _hoverText.Value = "";

    // Collect all festivals for today
    List<string> todayNames = new();
    FestivalType todayIconType = FestivalType.None;

    if (Utility.isFestivalDay())
    {
      todayIconType = FestivalType.Regular;
      Dictionary<string, string> festivalDates = DataLoader.Festivals_FestivalDates(Game1.temporaryContent);
      string todayKey = $"{Utility.getSeasonKey(Game1.season)}{Game1.dayOfMonth}";
      todayNames.Add(festivalDates.TryGetValue(todayKey, out string? name) ? name : todayKey);
    }

    foreach ((string id, PassiveFestivalData data) in GetActivePassiveFestivals(Game1.dayOfMonth, Game1.season))
    {
      todayNames.Add(GetPassiveFestivalName(id, data));
      if (todayIconType == FestivalType.None)
      {
        todayIconType = FishingDerbyIds.Contains(id) ? FestivalType.FishingDerby : FestivalType.Passive;
      }
    }

    if (todayNames.Count > 0)
    {
      _isToday.Value = true;
      _festivalType.Value = todayIconType;
      _hoverText.Value = string.Join(Environment.NewLine, todayNames.ConvertAll(n => string.Format(I18n.FestivalToday(), n)));
      return;
    }

    // Calculate tomorrow
    int tomorrowDay = Game1.dayOfMonth + 1;
    Season tomorrowSeason = Game1.season;

    if (tomorrowDay > 28)
    {
      tomorrowDay = 1;
      tomorrowSeason = tomorrowSeason switch
      {
        Season.Spring => Season.Summer,
        Season.Summer => Season.Fall,
        Season.Fall => Season.Winter,
        Season.Winter => Season.Spring,
        _ => tomorrowSeason
      };
    }

    // Collect all festivals for tomorrow
    List<string> tomorrowNames = new();
    FestivalType tomorrowIconType = FestivalType.None;

    if (Utility.isFestivalDay(tomorrowDay, tomorrowSeason))
    {
      tomorrowIconType = FestivalType.Regular;
      Dictionary<string, string> festivalDates = DataLoader.Festivals_FestivalDates(Game1.temporaryContent);
      string festivalKey = $"{Utility.getSeasonKey(tomorrowSeason)}{tomorrowDay}";
      tomorrowNames.Add(festivalDates.TryGetValue(festivalKey, out string? name) ? name : festivalKey);
    }

    // Passive festivals — first day only for tomorrow
    foreach ((string id, PassiveFestivalData data) in GetPassiveFestivalsStartingOn(tomorrowDay, tomorrowSeason))
    {
      tomorrowNames.Add(GetPassiveFestivalName(id, data));
      if (tomorrowIconType == FestivalType.None)
      {
        tomorrowIconType = FishingDerbyIds.Contains(id) ? FestivalType.FishingDerby : FestivalType.Passive;
      }
    }

    if (tomorrowNames.Count > 0)
    {
      _isTomorrow.Value = true;
      _festivalType.Value = tomorrowIconType;
      _hoverText.Value = string.Join(Environment.NewLine, tomorrowNames.ConvertAll(n => string.Format(I18n.FestivalTomorrow(), n)));
    }
  }

  // Get all active passive festivals for a given day (including mid-festival)
  private static IEnumerable<(string id, PassiveFestivalData data)> GetActivePassiveFestivals(
    int day, Season season)
  {
    Dictionary<string, PassiveFestivalData> allPassive = DataLoader.PassiveFestivals(Game1.content);

    foreach (KeyValuePair<string, PassiveFestivalData> entry in allPassive)
    {
      if (entry.Value.Season == season
          && day >= entry.Value.StartDay && day <= entry.Value.EndDay
          && GameStateQuery.CheckConditions(entry.Value.Condition))
      {
        yield return (entry.Key, entry.Value);
      }
    }
  }

  // Get all passive festivals starting on a specific day (not mid-festival)
  private static IEnumerable<(string id, PassiveFestivalData data)> GetPassiveFestivalsStartingOn(
    int day, Season season)
  {
    Dictionary<string, PassiveFestivalData> allPassive = DataLoader.PassiveFestivals(Game1.content);

    foreach (KeyValuePair<string, PassiveFestivalData> entry in allPassive)
    {
      if (entry.Value.Season == season && entry.Value.StartDay == day
          && GameStateQuery.CheckConditions(entry.Value.Condition))
      {
        yield return (entry.Key, entry.Value);
      }
    }
  }

  // Get display name, falling back to humanized ID ("TroutDerby" → "Trout Derby")
  private static string GetPassiveFestivalName(string festivalId, PassiveFestivalData data)
  {
    string parsed = TokenParser.ParseText(data.DisplayName);
    if (!string.IsNullOrWhiteSpace(parsed))
    {
      return parsed;
    }

    // Insert spaces before uppercase letters: "TroutDerby" → "Trout Derby"
    return System.Text.RegularExpressions.Regex.Replace(festivalId, "(?<=.)([A-Z])", " $1");
  }

  private bool ShouldDrawIcon => _isToday.Value || _isTomorrow.Value;
  #endregion


  #region Event subscriptions
  private void OnDayStarted(object? sender, DayStartedEventArgs e)
  {
    CheckForFestival();
  }

  private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
  {
    if (!UIElementUtils.IsRenderingNormally() || !ShouldDrawIcon)
    {
      return;
    }

    ClickableTextureComponent icon = _festivalIcon.Value;

    switch (_festivalType.Value)
    {
      case FestivalType.Passive:
        icon.texture = Game1.mouseCursors;
        icon.sourceRect = StarSourceRect;
        icon.scale = 40 / 8f;
        break;
      case FestivalType.FishingDerby:
        icon.texture = Game1.mouseCursors_1_6;
        icon.sourceRect = FishingDerbySourceRect;
        icon.scale = 40 / 11f;
        break;
      default:
        icon.texture = _billboardTexture;
        icon.sourceRect = FlagSourceRect;
        icon.scale = 40 / 13f;
        break;
    }

    Point iconPosition = IconHandler.Handler.GetNewIconPosition();
    icon.bounds.X = iconPosition.X;
    icon.bounds.Y = iconPosition.Y;

    // Offset icons to center them in the icon slot
    if (_festivalType.Value == FestivalType.Passive)
    {
      icon.bounds.X += 8;
      icon.bounds.Y += 8;
    }
    else if (_festivalType.Value == FestivalType.FishingDerby)
    {
      icon.bounds.X += 3;
      icon.bounds.Y += 3;
    }

    icon.draw(e.SpriteBatch);

    // Draw static exclamation mark overlay for "today"
    if (_isToday.Value)
    {
      float scale = 1.6f;
      e.SpriteBatch.Draw(
        Game1.mouseCursors,
        new Vector2(iconPosition.X + 30 + 2.5f * scale, iconPosition.Y + 16 + 7f * scale),
        new Rectangle(403, 496, 5, 14),
        Color.White,
        0f,
        new Vector2(2.5f, 7f),
        scale,
        SpriteEffects.None,
        1f
      );
    }
  }

  private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
  {
    if (ShouldDrawIcon && _festivalIcon.Value.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
    {
      IClickableMenu.drawHoverText(Game1.spriteBatch, _hoverText.Value, Game1.dialogueFont);
    }
  }
  #endregion
}
