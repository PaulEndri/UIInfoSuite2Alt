using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

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

  private readonly PerScreen<List<QueuedIcon>> _queuedIcons = new(() => new());

  private IconHandler() { }

  public static IconHandler Handler { get; } = new();

  public bool IsQuestLogPermanent { get; set; } = false;

  /// <summary>The configured icon order, keyed by icon key. Lower = more right.</summary>
  public Dictionary<string, int> IconOrder { get; set; } = new();

  /// <summary>
  ///   Enqueue an icon to be drawn this frame. Icons are sorted by configured order
  ///   and drawn together during <see cref="DrawQueuedIcons" />.
  /// </summary>
  /// <param name="iconKey">The icon key used to look up the configured sort order</param>
  /// <param name="draw">Draws the icon at the given position</param>
  /// <param name="drawHover">Draws hover text if the icon is hovered (called after all icons are drawn)</param>
  public void EnqueueIcon(string iconKey, Action<SpriteBatch, Point> draw, Action<SpriteBatch>? drawHover = null)
  {
    int order = IconOrder.TryGetValue(iconKey, out int o) ? o : 99;
    _queuedIcons.Value.Add(new QueuedIcon
    {
      Draw = draw,
      DrawHover = drawHover,
      SortOrder = order,
      RegistrationOrder = _queuedIcons.Value.Count
    });
  }

  /// <summary>
  ///   Sort all queued icons by configured order, compute positions, draw them,
  ///   then draw hover text. Call this once per frame during RenderedHud.
  /// </summary>
  public void DrawQueuedIcons(SpriteBatch batch)
  {
    List<QueuedIcon> icons = _queuedIcons.Value;
    if (icons.Count == 0)
    {
      return;
    }

    // Stable sort: by configured order, then by registration order
    var sorted = icons.OrderBy(i => i.SortOrder).ThenBy(i => i.RegistrationOrder).ToList();

    int yPos = Game1.options.zoomButtons ? 290 : 260;
    int xBase = Tools.GetWidthInPlayArea() - 70;

    if (IsQuestLogPermanent || Game1.player.questLog.Any() || Game1.player.team.specialOrders.Any())
    {
      xBase -= 65;
    }

    // Draw all icons
    for (int i = 0; i < sorted.Count; i++)
    {
      var pos = new Point(xBase - 48 * i, yPos);
      sorted[i].Draw(batch, pos);
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
  }
}
