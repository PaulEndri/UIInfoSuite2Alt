using StardewValley;

namespace UIInfoSuite2Alt.Compatibility;

/// <summary>
/// Public API for Walk of Life - Rebirth (DaLion.Professions).
/// Mirrors methods from DaLion.Professions.IProfessionsApi.
/// </summary>
public interface IWalkOfLifeApi
{
  /// <summary>Gets the value of an Ecologist's forage quality.</summary>
  /// <param name="farmer">The player.</param>
  /// <returns>An SObject quality level.</returns>
  int GetEcologistForageQuality(Farmer? farmer = null);

  /// <summary>Gets the value of a Gemologist's mineral quality.</summary>
  /// <param name="farmer">The player.</param>
  /// <returns>An SObject quality level.</returns>
  int GetGemologistMineralQuality(Farmer? farmer = null);

  /// <summary>Gets the price bonus applied to animal produce sold by Producer.</summary>
  /// <param name="farmer">The player.</param>
  /// <returns>A bonus applied to Producer animal product prices.</returns>
  float GetProducerSaleBonus(Farmer? farmer = null);

  /// <summary>Gets the price bonus applied to fish sold by Angler.</summary>
  /// <param name="farmer">The player.</param>
  /// <returns>A bonus applied to Angler fish prices.</returns>
  float GetAnglerSaleBonus(Farmer? farmer = null);

  /// <summary>Gets the Conservationist's effective tax deduction based on last season's trash collection.</summary>
  /// <param name="farmer">The player.</param>
  /// <returns>The percentage of tax deductions currently in effect (0 to 1).</returns>
  float GetConservationistTaxDeduction(Farmer? farmer = null);
}
