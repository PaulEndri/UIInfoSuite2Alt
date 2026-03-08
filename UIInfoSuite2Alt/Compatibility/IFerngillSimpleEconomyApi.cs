using Object = StardewValley.Object;

namespace UIInfoSuite2Alt.Compatibility;

public interface IFerngillSimpleEconomyApi
{
  bool IsLoaded();
  bool ItemIsInEconomy(Object obj);
}
