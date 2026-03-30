using System;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace UIInfoSuite2Alt.Compatibility;

public interface IInformantApi
{
  void AddItemDecorator(
    string id,
    Func<string> displayName,
    Func<string> description,
    Func<Item, Texture2D?> decorator
  );
}
