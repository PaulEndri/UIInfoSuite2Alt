namespace UIInfoSuite2Alt.Compatibility;

public interface IVanillaPlusProfessions
{
  /// <summary>
  /// XP limits for VPP's extended levels. Index 0 is total experience required for level 11.
  /// </summary>
  int[] LevelExperiences { get; }

  /// <summary>
  /// The max level config value (10, 15, or 20) controlling when mastery unlocks.
  /// </summary>
  int MasteryCaveChanges { get; }
}
