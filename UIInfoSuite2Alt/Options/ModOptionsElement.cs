using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace UIInfoSuite2Alt.Options;

public class ModOptionsElement
{
  protected const int DefaultX = 8;
  protected const int DefaultY = 4;
  protected const int DefaultPixelSize = 9;

  protected readonly string _label;

  protected readonly ModOptionsElement? _parent;
  private readonly int _whichOption;
  private readonly bool _isSubtitle;
  private readonly bool _isSmallText;
  private readonly Color? _textColor;

  public ModOptionsElement(string label, int whichOption = -1, ModOptionsElement? parent = null, bool isSubtitle = false, bool isSmallText = false, Color? textColor = null)
  {
    int x = DefaultX * Game1.pixelZoom;
    int y = DefaultY * Game1.pixelZoom;
    int width = DefaultPixelSize * Game1.pixelZoom;
    int height = DefaultPixelSize * Game1.pixelZoom;

    if (parent != null)
    {
      x += DefaultX * 2 * Game1.pixelZoom;
    }

    Bounds = new Rectangle(x, y, width, height);
    _label = label;
    _whichOption = whichOption;

    _parent = parent;
    _isSubtitle = isSubtitle;
    _isSmallText = isSmallText;
    _textColor = textColor;
  }

  public Rectangle Bounds { get; protected set; }

  public virtual void ReceiveLeftClick(int x, int y) { }

  public virtual void LeftClickHeld(int x, int y) { }

  public virtual void LeftClickReleased(int x, int y) { }

  public virtual void ReceiveKeyPress(Keys key) { }

  public virtual void Draw(SpriteBatch batch, int slotX, int slotY)
  {
    if (_isSmallText)
    {
      Utility.drawTextWithShadow(
        batch,
        _label,
        Game1.smallFont,
        new Vector2(slotX + Bounds.X, slotY + Bounds.Y),
        _textColor ?? Game1.textColor,
        1f,
        0.1f
      );
    }
    else if (_isSubtitle)
    {
      Utility.drawTextWithShadow(
        batch,
        _label,
        Game1.dialogueFont,
        new Vector2(slotX + Bounds.X, slotY + Bounds.Y),
        Game1.textColor,
        1f,
        0.1f
      );
    }
    else if (_whichOption < 0)
    {
      SpriteText.drawString(
        batch,
        _label,
        slotX + Bounds.X,
        slotY + Bounds.Y,
        999,
        -1,
        999,
        1,
        0.1f
      );
    }
    else
    {
      Utility.drawTextWithShadow(
        batch,
        _label,
        Game1.dialogueFont,
        new Vector2(slotX + Bounds.X + Bounds.Width + Game1.pixelZoom * 2, slotY + Bounds.Y),
        Game1.textColor,
        1f,
        0.1f
      );
    }
  }

  public virtual Point? GetRelativeSnapPoint(Rectangle slotBounds)
  {
    // Positioning taken from OptionsPage.snapCursorToCurrentSnappedComponent
    return new Point(48, slotBounds.Height / 2 - 12);
  }
}
