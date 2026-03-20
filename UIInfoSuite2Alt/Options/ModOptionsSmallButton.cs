using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace UIInfoSuite2Alt.Options;

internal class ModOptionsSmallButton : ModOptionsElement
{
  private readonly Action _onClick;
  private bool _boundsInitialized;

  public ModOptionsSmallButton(
    string label,
    int whichOption,
    Action onClick
  ) : base(label, whichOption)
  {
    _onClick = onClick;
  }

  private void EnsureBounds()
  {
    if (_boundsInitialized)
    {
      return;
    }

    _boundsInitialized = true;
    var textSize = Game1.smallFont.MeasureString(_label);
    Bounds = new Rectangle(
      Bounds.X,
      Bounds.Y - Game1.pixelZoom * 4,
      (int)textSize.X + 64,
      (int)textSize.Y + 20
    );
  }

  public override void ReceiveLeftClick(int x, int y)
  {
    if (Bounds.Contains(x, y))
    {
      Game1.playSound("drumkit6");
      _onClick();
    }
  }

  public override void Draw(SpriteBatch batch, int slotX, int slotY)
  {
    EnsureBounds();

    float layerDepth = 0.8f - (slotY + Bounds.Y) * 1E-06f;

    IClickableMenu.drawTextureBox(
      batch,
      Game1.mouseCursors,
      new Rectangle(432, 439, 9, 9),
      slotX + Bounds.X,
      slotY + Bounds.Y,
      Bounds.Width,
      Bounds.Height,
      Color.White,
      4f,
      false,
      layerDepth
    );

    var textSize = Game1.smallFont.MeasureString(_label) / 2f;
    textSize.X = (int)(textSize.X / 4f) * 4;
    textSize.Y = (int)(textSize.Y / 4f) * 4;

    Utility.drawTextWithShadow(
      batch,
      _label,
      Game1.smallFont,
      new Vector2(slotX + Bounds.Center.X, slotY + Bounds.Center.Y) - textSize,
      Game1.textColor,
      1f,
      layerDepth + 1E-06f,
      -1,
      -1,
      0f
    );
  }

  public override Point? GetRelativeSnapPoint(Rectangle slotBounds)
  {
    EnsureBounds();
    return new Point(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2);
  }
}
