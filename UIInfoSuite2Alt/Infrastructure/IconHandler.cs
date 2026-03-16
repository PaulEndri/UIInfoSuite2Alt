using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2Alt.UIElements;

namespace UIInfoSuite2Alt.Infrastructure;

public sealed class IconHandler
{
  public static readonly string[] IconKeys =
  {
    "Luck",
    "Weather",
    "Birthday",
    "Festival",
    "QueenOfSauce",
    "ToolUpgrade",
    "RobinBuilding",
    "SeasonalBerry",
    "TravelingMerchant",
    "Bookseller"
  };

  private const int DefaultIconWidth = 40;
  private const int IconGap = 8;

  private readonly PerScreen<List<QueuedIcon>> _queuedIcons = new(() => new());

  private IconHandler() { }

  public static IconHandler Handler { get; } = new();

  public bool IsQuestLogPermanent { get; set; } = false;

  /// <summary>When true, a quest count number is drawn below the journal icon, requiring more vertical clearance.</summary>
  public bool ShowQuestCount { get; set; } = true;

  /// <summary>The configured icon order, keyed by icon key. Lower = more right.</summary>
  public Dictionary<string, int> IconOrder { get; set; } = new();

  /// <summary>When true, icons stack vertically downward instead of horizontally to the left.</summary>
  public bool UseVerticalLayout { get; set; }

  /// <summary>Enqueue an icon to draw this frame, sorted by configured order.</summary>
  public void EnqueueIcon(string iconKey, Action<SpriteBatch, Point> draw, Action<SpriteBatch>? drawHover = null, int iconWidth = 0)
  {
    int order = IconOrder.TryGetValue(iconKey, out int o) ? o : 99;
    _queuedIcons.Value.Add(new QueuedIcon
    {
      Draw = draw,
      DrawHover = drawHover,
      SortOrder = order,
      RegistrationOrder = _queuedIcons.Value.Count,
      IconWidth = iconWidth
    });
  }

  /// <summary>Sort, position, draw all queued icons + hover text. Call once per frame.</summary>
  public void DrawQueuedIcons(SpriteBatch batch)
  {
    List<QueuedIcon> icons = _queuedIcons.Value;
    if (icons.Count == 0)
    {
      return;
    }

    // Skip when HUD is hidden (cutscenes, events)
    if (!UIElementUtils.IsRenderingNormally())
    {
      icons.Clear();
      return;
    }

    // Stable sort: config order, then registration order
    var sorted = icons.OrderBy(i => i.SortOrder).ThenBy(i => i.RegistrationOrder).ToList();

    int yPos = Game1.options.zoomButtons ? 290 : 260;
    int xBase = Tools.GetWidthInPlayArea() - 70;

    if (IsQuestLogPermanent || Game1.player.questLog.Any() || Game1.player.team.specialOrders.Any())
    {
      if (UseVerticalLayout)
      {
        xBase -= 16;
        yPos += ShowQuestCount ? 55 : 20;
      }
      else
      {
        xBase -= 65;
      }
    }
    else if (UseVerticalLayout)
    {
      yPos -= 30;
    }

    // Offset for wide first icon
    if (!UseVerticalLayout)
    {
      int firstWidth = sorted[0].IconWidth > 0 ? sorted[0].IconWidth : DefaultIconWidth;
      xBase -= Math.Max(0, firstWidth - DefaultIconWidth);
    }

    // Draw icons with variable-width spacing
    int xOffset = 0;
    for (int i = 0; i < sorted.Count; i++)
    {
      Point pos = UseVerticalLayout
        ? new Point(xBase, yPos + 48 * i)
        : new Point(xBase - xOffset, yPos);
      sorted[i].Draw(batch, pos);

      if (!UseVerticalLayout && i + 1 < sorted.Count)
      {
        int nextWidth = sorted[i + 1].IconWidth > 0 ? sorted[i + 1].IconWidth : DefaultIconWidth;
        xOffset += IconGap + nextWidth;
      }
    }

    // Draw hover text on top
    foreach (QueuedIcon icon in sorted)
    {
      icon.DrawHover?.Invoke(batch);
    }

    icons.Clear();
  }

  public void Reset(object? sender, EventArgs e)
  {
    _queuedIcons.Value.Clear();
  }

  private class QueuedIcon
  {
    public Action<SpriteBatch, Point> Draw { get; set; } = null!;
    public Action<SpriteBatch>? DrawHover { get; set; }
    public int SortOrder { get; set; }
    public int RegistrationOrder { get; set; }
    public int IconWidth { get; set; }
  }
}
