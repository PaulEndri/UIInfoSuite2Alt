using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace UIInfoSuite2Alt.UIElements.ExperienceElements;

internal class DisplayedExperienceValue
{
  private readonly float _experiencePoints;
  private readonly Color _color;
  private readonly float _scale = 1f;
  private float _alpha = 1f;
  private Vector2 _position;
  private int _delayTicks;

  public DisplayedExperienceValue(
    float experiencePoints,
    Vector2 position,
    Color? color = null,
    int delayTicks = 0
  )
  {
    _experiencePoints = experiencePoints;
    _position = position;
    _color = color ?? new Color(240, 240, 240, 255);
    _delayTicks = delayTicks;
  }

  public bool IsInvisible => _alpha <= 0f;

  public void Draw()
  {
    if (_delayTicks > 0)
    {
      _delayTicks--;
      return;
    }

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
      _color * _alpha,
      0f,
      Vector2.Zero,
      _scale,
      SpriteEffects.None,
      1f
    );
  }
}
