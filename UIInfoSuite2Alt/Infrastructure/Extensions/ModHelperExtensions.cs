using StardewModdingAPI;

namespace UIInfoSuite2Alt.Infrastructure.Extensions;

public static class ModHelperExtensions
{
  public static string SafeGetString(this IModHelper helper, string key)
  {
    var result = string.Empty;

    if (!string.IsNullOrEmpty(key) && helper != null)
    {
      result = helper.Translation.Get(key);
    }

    return result;
  }
}
