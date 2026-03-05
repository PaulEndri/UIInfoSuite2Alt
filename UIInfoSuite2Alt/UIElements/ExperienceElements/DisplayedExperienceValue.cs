using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StardewValley;

namespace UIInfoSuite2Alt.UIElements.ExperienceElements;

internal class DisplayedExperienceValue
{
  private readonly float _experiencePoints;
  private readonly float _scale = 1f;
  private float _alpha = 1f;
  private Vector2 _position;

  public DisplayedExperienceValue(float experiencePoints, Vector2 position)
  {
    _experiencePoints = experiencePoints;
    _position = position;
  }

  public bool IsInvisible => _alpha <= 0f;

  public void Draw()
  {
    _position.Y -= 0.5f;
    _alpha -= 0.02f;

    Vector2 pos = Utility.ModifyCoordinatesForUIScale(
      new Vector2(_position.X - 28, _position.Y - 130)
    );

    string text = "Exp " + _experiencePoints;

    // Shadow
    Game1.spriteBatch.DrawString(
        Game1.smallFont,
        text,
        pos + new Vector2(2f, 2f),
        Color.Black * _alpha,
        0f,
        Vector2.Zero,
        _scale,
        SpriteEffects.None,
        1f
    );

    // Text
    Game1.spriteBatch.DrawString(
        Game1.smallFont,
        text,
        pos,
        new Color(240, 240, 240, 255) * _alpha,
        0f,
        Vector2.Zero,
        _scale,
        SpriteEffects.None,
        1f
    );
  }
}
