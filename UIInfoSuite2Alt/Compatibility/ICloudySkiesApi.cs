using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace UIInfoSuite2Alt.Compatibility;

public interface ICloudySkiesApi
{
  IEnumerable<IWeatherData> GetAllCustomWeather();
}

public interface IWeatherData
{
  string Id { get; }
  string DisplayName { get; }
  string? IconTexture { get; }
  Point IconSource { get; }
}
