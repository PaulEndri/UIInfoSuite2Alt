using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace UIInfoSuite2Alt.Compatibility;

public interface ISpaceCoreApi
{
  string[] GetCustomSkills();
  int GetLevelForCustomSkill(Farmer farmer, string skill);
  int GetExperienceForCustomSkill(Farmer farmer, string skill);
  Texture2D GetSkillPageIconForCustomSkill(string skill);
  string GetDisplayNameOfCustomSkill(string skill);
  int GetProfessionId(string skill, string profession);
}
