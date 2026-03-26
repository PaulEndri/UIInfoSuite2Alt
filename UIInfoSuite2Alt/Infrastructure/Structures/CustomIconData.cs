using Microsoft.Xna.Framework;

namespace UIInfoSuite2Alt.Infrastructure.Structures;

public class CustomIconData
{
  public string Texture { get; set; } = "";
  public Rectangle SourceRect { get; set; } = new(0, 0, 20, 20);
  public string? HoverText { get; set; }
}
