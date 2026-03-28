using Microsoft.Xna.Framework;

namespace UIInfoSuite2Alt.Compatibility;

/// <summary>
/// Data model for custom HUD icons added via the <c>UIInfoSuite2Alt/CustomIcons</c> content asset.
/// Content Patcher mods can edit this asset to display icons in the top-right HUD icon bar.
/// </summary>
public class CustomIconData
{
  /// <summary>Asset path to the texture containing the icon sprite (e.g. "Mods/YourMod/Icons").</summary>
  public string Texture { get; set; } = "";

  /// <summary>Source rectangle within the texture to draw. Defaults to a 20x20 region at origin.</summary>
  public Rectangle SourceRect { get; set; } = new(0, 0, 20, 20);

  /// <summary>Optional tooltip text shown when the player hovers over the icon.</summary>
  public string? HoverText { get; set; }
}
