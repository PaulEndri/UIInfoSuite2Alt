using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace UIInfoSuite2Alt.Options;

internal class ModOptionsImage : ModOptionsElement
{
  private readonly Func<Texture2D> _texture;
  private readonly Rectangle? _sourceRect;
  private readonly int _scale;

  public ModOptionsImage(
    Func<Texture2D> texture,
    Rectangle? sourceRect = null,
    int scale = Game1.pixelZoom
  )
    : base("", -1)
  {
    _texture = texture;
    _sourceRect = sourceRect;
    _scale = scale;
  }

  public override void Draw(SpriteBatch batch, int slotX, int slotY)
  {
    Texture2D tex = _texture();
    Rectangle source = _sourceRect ?? new Rectangle(0, 0, tex.Width, tex.Height);

    // Center horizontally in the slot
    int drawWidth = source.Width * _scale;
    int drawHeight = source.Height * _scale;
    int slotWidth = Game1.activeClickableMenu?.width ?? Game1.uiViewport.Width;
    int drawX = slotX + (slotWidth - Game1.tileSize / 2 - drawWidth) / 2;

    // Center vertically in the slot
    int slotHeight =
      Game1.activeClickableMenu != null
        ? (Game1.activeClickableMenu.height - Game1.tileSize * 2) / 7 + Game1.pixelZoom
        : drawHeight;
    int drawY = slotY + (slotHeight - drawHeight) / 2;

    batch.Draw(
      tex,
      new Vector2(drawX, drawY),
      source,
      Color.White,
      0f,
      Vector2.Zero,
      _scale,
      SpriteEffects.None,
      0.4f
    );
  }

  public override Point? GetRelativeSnapPoint(Rectangle slotBounds)
  {
    // Non-interactive, skip snap
    return null;
  }
}
