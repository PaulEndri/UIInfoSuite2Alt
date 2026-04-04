using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace UIInfoSuite2Alt.Infrastructure;

/// <summary>Draws a 9-slice (9-patch) sprite that tiles to fill any size while keeping pixel-perfect corners and edges.</summary>
public static class NineSlice
{
  /// <summary>Default brown box from Cursors with 6px corners and built-in 2px padding.</summary>
  public static readonly SliceSources DefaultSlices = new(
    TopLeft: new(293, 360, 6, 6),
    Top: new(299, 360, 1, 6),
    TopRight: new(311, 360, 6, 6),
    Left: new(293, 377, 6, 1),
    Center: new(299, 366, 1, 1),
    Right: new(311, 377, 6, 1),
    BottomLeft: new(293, 378, 6, 6),
    Bottom: new(299, 378, 1, 6),
    BottomRight: new(311, 378, 6, 6)
  );
  /// <summary>
  /// Draw a 9-slice panel from individual non-contiguous source rectangles.
  /// Corners are fixed, edges stretch to fill, center fills the remaining area.
  /// </summary>
  /// <param name="batch">The sprite batch to draw with.</param>
  /// <param name="texture">Source texture containing the 9-slice pieces.</param>
  /// <param name="slices">The 9 source rectangles (corners, edges, center).</param>
  /// <param name="destination">Screen-space rectangle to fill.</param>
  /// <param name="scale">Pixel scale factor applied to all pieces.</param>
  /// <param name="layerDepth">Draw layer depth.</param>
  /// <param name="color">Tint color (default White).</param>
  public static void Draw(
    SpriteBatch batch,
    Texture2D texture,
    SliceSources slices,
    Rectangle destination,
    float scale,
    float layerDepth,
    Color? color = null
  )
  {
    Color tint = color ?? Color.White;

    // Scaled corner dimensions (derived from corner source rects)
    int csL = (int)(slices.TopLeft.Width * scale);
    int csR = (int)(slices.TopRight.Width * scale);
    int csT = (int)(slices.TopLeft.Height * scale);
    int csB = (int)(slices.BottomLeft.Height * scale);

    // Scaled edge tile sizes
    int scaledEdgeW = (int)(slices.Top.Width * scale);
    int scaledEdgeH = (int)(slices.Left.Height * scale);

    // Inner area (between corners)
    int innerX = destination.X + csL;
    int innerY = destination.Y + csT;
    int innerW = destination.Width - csL - csR;
    int innerH = destination.Height - csT - csB;

    // --- Corners (fixed size) ---
    batch.Draw(
      texture,
      new Rectangle(destination.X, destination.Y, csL, csT),
      slices.TopLeft,
      tint,
      0f,
      Vector2.Zero,
      SpriteEffects.None,
      layerDepth
    );
    batch.Draw(
      texture,
      new Rectangle(destination.Right - csR, destination.Y, csR, csT),
      slices.TopRight,
      tint,
      0f,
      Vector2.Zero,
      SpriteEffects.None,
      layerDepth
    );
    batch.Draw(
      texture,
      new Rectangle(destination.X, destination.Bottom - csB, csL, csB),
      slices.BottomLeft,
      tint,
      0f,
      Vector2.Zero,
      SpriteEffects.None,
      layerDepth
    );
    batch.Draw(
      texture,
      new Rectangle(destination.Right - csR, destination.Bottom - csB, csR, csB),
      slices.BottomRight,
      tint,
      0f,
      Vector2.Zero,
      SpriteEffects.None,
      layerDepth
    );

    // --- Edges (stretch to fill) ---
    batch.Draw(
      texture,
      new Rectangle(innerX, destination.Y, innerW, csT),
      slices.Top,
      tint,
      0f,
      Vector2.Zero,
      SpriteEffects.None,
      layerDepth
    );
    batch.Draw(
      texture,
      new Rectangle(innerX, destination.Bottom - csB, innerW, csB),
      slices.Bottom,
      tint,
      0f,
      Vector2.Zero,
      SpriteEffects.None,
      layerDepth
    );
    batch.Draw(
      texture,
      new Rectangle(destination.X, innerY, csL, innerH),
      slices.Left,
      tint,
      0f,
      Vector2.Zero,
      SpriteEffects.None,
      layerDepth
    );
    batch.Draw(
      texture,
      new Rectangle(destination.Right - csR, innerY, csR, innerH),
      slices.Right,
      tint,
      0f,
      Vector2.Zero,
      SpriteEffects.None,
      layerDepth
    );

    // --- Center (stretch to fill) ---
    batch.Draw(
      texture,
      new Rectangle(innerX, innerY, innerW, innerH),
      slices.Center,
      tint,
      0f,
      Vector2.Zero,
      SpriteEffects.None,
      layerDepth
    );
  }

  /// <summary>
  /// Draw the default brown box panel from Cursors.
  /// </summary>
  public static void Draw(
    SpriteBatch batch,
    Rectangle destination,
    float scale,
    float layerDepth,
    Color? color = null
  )
  {
    Draw(batch, Game1.mouseCursors, DefaultSlices, destination, scale, layerDepth, color);
  }

  /// <summary>
  /// Draw a 9-slice panel from a contiguous square source sprite.
  /// Corners are fixed, edges and center tile to fill the destination.
  /// </summary>
  public static void Draw(
    SpriteBatch batch,
    Texture2D texture,
    Rectangle source,
    Rectangle destination,
    int cornerSize,
    float scale,
    float layerDepth,
    Color? color = null
  )
  {
    Draw(
      batch,
      texture,
      SliceSources.FromContiguous(source, cornerSize),
      destination,
      scale,
      layerDepth,
      color
    );
  }

  /// <summary>Source rectangles for each of the 9 slices.</summary>
  public readonly record struct SliceSources(
    Rectangle TopLeft,
    Rectangle Top,
    Rectangle TopRight,
    Rectangle Left,
    Rectangle Center,
    Rectangle Right,
    Rectangle BottomLeft,
    Rectangle Bottom,
    Rectangle BottomRight
  )
  {
    /// <summary>Create slices from a contiguous source rectangle with uniform corner size.</summary>
    public static SliceSources FromContiguous(Rectangle source, int cornerSize)
    {
      int edgeW = source.Width - cornerSize * 2;
      int edgeH = source.Height - cornerSize * 2;

      return new SliceSources(
        TopLeft: new(source.X, source.Y, cornerSize, cornerSize),
        Top: new(source.X + cornerSize, source.Y, edgeW, cornerSize),
        TopRight: new(source.Right - cornerSize, source.Y, cornerSize, cornerSize),
        Left: new(source.X, source.Y + cornerSize, cornerSize, edgeH),
        Center: new(source.X + cornerSize, source.Y + cornerSize, edgeW, edgeH),
        Right: new(source.Right - cornerSize, source.Y + cornerSize, cornerSize, edgeH),
        BottomLeft: new(source.X, source.Bottom - cornerSize, cornerSize, cornerSize),
        Bottom: new(source.X + cornerSize, source.Bottom - cornerSize, edgeW, cornerSize),
        BottomRight: new(
          source.Right - cornerSize,
          source.Bottom - cornerSize,
          cornerSize,
          cornerSize
        )
      );
    }
  }
}
