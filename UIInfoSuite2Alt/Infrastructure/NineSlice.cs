using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace UIInfoSuite2Alt.Infrastructure;

/// <summary>Draws a 9-slice (9-patch) sprite that tiles to fill any size while keeping pixel-perfect corners and edges.</summary>
public static class NineSlice
{
  /// <summary>
  /// Draw a 9-slice panel from a square source sprite.
  /// Corners are fixed, edges and center tile to fill the destination.
  /// All pieces are drawn at the given <paramref name="scale"/>.
  /// </summary>
  /// <param name="batch">The sprite batch to draw with.</param>
  /// <param name="texture">Source texture containing the 9-slice sprite.</param>
  /// <param name="source">Full source rectangle of the sprite (e.g. 10x10).</param>
  /// <param name="destination">Screen-space rectangle to fill.</param>
  /// <param name="cornerSize">Size of each corner in source pixels (e.g. 2 for 2x2 corners).</param>
  /// <param name="scale">Pixel scale factor applied to all pieces.</param>
  /// <param name="layerDepth">Draw layer depth.</param>
  /// <param name="color">Tint color (default White).</param>
  public static void Draw(
    SpriteBatch batch, Texture2D texture, Rectangle source, Rectangle destination,
    int cornerSize, float scale, float layerDepth, Color? color = null)
  {
    Color tint = color ?? Color.White;
    int cs = (int)(cornerSize * scale); // scaled corner size

    // Source edge/center sizes in source pixels
    int edgeW = source.Width - cornerSize * 2;
    int edgeH = source.Height - cornerSize * 2;

    // Source rectangles for all 9 pieces
    // Corners
    Rectangle srcTL = new(source.X, source.Y, cornerSize, cornerSize);
    Rectangle srcTR = new(source.Right - cornerSize, source.Y, cornerSize, cornerSize);
    Rectangle srcBL = new(source.X, source.Bottom - cornerSize, cornerSize, cornerSize);
    Rectangle srcBR = new(source.Right - cornerSize, source.Bottom - cornerSize, cornerSize, cornerSize);

    // Edges
    Rectangle srcTop = new(source.X + cornerSize, source.Y, edgeW, cornerSize);
    Rectangle srcBot = new(source.X + cornerSize, source.Bottom - cornerSize, edgeW, cornerSize);
    Rectangle srcLeft = new(source.X, source.Y + cornerSize, cornerSize, edgeH);
    Rectangle srcRight = new(source.Right - cornerSize, source.Y + cornerSize, cornerSize, edgeH);

    // Center
    Rectangle srcCenter = new(source.X + cornerSize, source.Y + cornerSize, edgeW, edgeH);

    int scaledEdgeW = (int)(edgeW * scale);
    int scaledEdgeH = (int)(edgeH * scale);

    // Inner area (between corners)
    int innerX = destination.X + cs;
    int innerY = destination.Y + cs;
    int innerW = destination.Width - cs * 2;
    int innerH = destination.Height - cs * 2;

    // --- Corners (fixed size) ---
    batch.Draw(texture, new Rectangle(destination.X, destination.Y, cs, cs), srcTL, tint, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
    batch.Draw(texture, new Rectangle(destination.Right - cs, destination.Y, cs, cs), srcTR, tint, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
    batch.Draw(texture, new Rectangle(destination.X, destination.Bottom - cs, cs, cs), srcBL, tint, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
    batch.Draw(texture, new Rectangle(destination.Right - cs, destination.Bottom - cs, cs, cs), srcBR, tint, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);

    // --- Edges (tile to fill) ---
    TileHorizontal(batch, texture, srcTop, innerX, destination.Y, innerW, cs, scaledEdgeW, tint, layerDepth);
    TileHorizontal(batch, texture, srcBot, innerX, destination.Bottom - cs, innerW, cs, scaledEdgeW, tint, layerDepth);
    TileVertical(batch, texture, srcLeft, destination.X, innerY, cs, innerH, scaledEdgeH, tint, layerDepth);
    TileVertical(batch, texture, srcRight, destination.Right - cs, innerY, cs, innerH, scaledEdgeH, tint, layerDepth);

    // --- Center (tile to fill) ---
    TileArea(batch, texture, srcCenter, innerX, innerY, innerW, innerH, scaledEdgeW, scaledEdgeH, tint, layerDepth);
  }

  private static void TileHorizontal(
    SpriteBatch batch, Texture2D texture, Rectangle src,
    int x, int y, int totalWidth, int height, int tileWidth,
    Color tint, float layerDepth)
  {
    int drawn = 0;
    while (drawn < totalWidth)
    {
      int remaining = totalWidth - drawn;
      int drawWidth = Math.Min(tileWidth, remaining);

      // Clip source if partial tile
      Rectangle clippedSrc = drawWidth < tileWidth
        ? new Rectangle(src.X, src.Y, (int)Math.Ceiling((double)drawWidth / tileWidth * src.Width), src.Height)
        : src;

      batch.Draw(texture, new Rectangle(x + drawn, y, drawWidth, height), clippedSrc, tint, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
      drawn += tileWidth;
    }
  }

  private static void TileVertical(
    SpriteBatch batch, Texture2D texture, Rectangle src,
    int x, int y, int width, int totalHeight, int tileHeight,
    Color tint, float layerDepth)
  {
    int drawn = 0;
    while (drawn < totalHeight)
    {
      int remaining = totalHeight - drawn;
      int drawHeight = Math.Min(tileHeight, remaining);

      Rectangle clippedSrc = drawHeight < tileHeight
        ? new Rectangle(src.X, src.Y, src.Width, (int)Math.Ceiling((double)drawHeight / tileHeight * src.Height))
        : src;

      batch.Draw(texture, new Rectangle(x, y + drawn, width, drawHeight), clippedSrc, tint, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
      drawn += tileHeight;
    }
  }

  private static void TileArea(
    SpriteBatch batch, Texture2D texture, Rectangle src,
    int x, int y, int totalWidth, int totalHeight, int tileWidth, int tileHeight,
    Color tint, float layerDepth)
  {
    int drawnY = 0;
    while (drawnY < totalHeight)
    {
      int remainingH = totalHeight - drawnY;
      int drawHeight = Math.Min(tileHeight, remainingH);

      int drawnX = 0;
      while (drawnX < totalWidth)
      {
        int remainingW = totalWidth - drawnX;
        int drawWidth = Math.Min(tileWidth, remainingW);

        // Clip source for partial tiles
        Rectangle clippedSrc = new(
          src.X,
          src.Y,
          drawWidth < tileWidth ? (int)Math.Ceiling((double)drawWidth / tileWidth * src.Width) : src.Width,
          drawHeight < tileHeight ? (int)Math.Ceiling((double)drawHeight / tileHeight * src.Height) : src.Height
        );

        batch.Draw(texture, new Rectangle(x + drawnX, y + drawnY, drawWidth, drawHeight), clippedSrc, tint, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
        drawnX += tileWidth;
      }

      drawnY += tileHeight;
    }
  }
}
