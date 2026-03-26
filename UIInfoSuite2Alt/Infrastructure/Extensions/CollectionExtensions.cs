using System.Collections.Generic;

namespace UIInfoSuite2Alt.Infrastructure.Extensions;

public static class CollectionExtensions
{
  /// <summary>
  ///   Get a value from a dictionary, or return a default if it isn't present in the collection
  /// </summary>
  /// <param name="dictionary">This Dictionary</param>
  /// <param name="key">The key to look up</param>
  /// <param name="defaultValue">The value to return if the key is not present</param>
  /// <typeparam name="TKey">Dictionary Key Type</typeparam>
  /// <typeparam name="TValue">Dictionary Value Type</typeparam>
  /// <returns></returns>
  public static TValue GetOrDefault<TKey, TValue>(
    this IDictionary<TKey, TValue> dictionary,
    TKey key,
    TValue defaultValue
  )
  {
    return dictionary.TryGetValue(key, out TValue? foundDictValue) ? foundDictValue : defaultValue;
  }
}
