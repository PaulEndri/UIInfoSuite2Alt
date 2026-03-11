using System.Text;
using StardewValley;
using StardewValley.Menus;
using StardewValley.GameData;
using StardewValley.TokenizableStrings;

namespace UIInfoSuite2Alt.UIElements;

public static class MonsterQuestHelper
{
  public static void ShowMonsterKillList()
  {
    StringBuilder stringBuilder = new();

    //string header = Game1.content.LoadString("Strings\\Locations:AdventureGuild_KillList_Header");
    //stringBuilder.Append(header.Replace('\n', '^') + "^");

    foreach (MonsterSlayerQuestData value in DataLoader.MonsterSlayerQuests(Game1.content).Values)
    {
      int num = 0;
      if (value.Targets != null)
      {
        foreach (string target in value.Targets)
        {
          num += Game1.stats.getMonstersKilled(target);
        }
      }

      string displayName = TokenParser.ParseText(value.DisplayName);
      string line = FormatKillListLine(displayName, num, value.Count);
      stringBuilder.Append(line);
    }

    //string footer = Game1.content.LoadString("Strings\\Locations:AdventureGuild_KillList_Footer");
    //stringBuilder.Append(footer.Replace('\n', '^'));

    string finalMessage = stringBuilder.ToString();

    Game1.activeClickableMenu = new LetterViewerMenu(finalMessage);
  }

  private static string FormatKillListLine(string name, int count, int goal)
  {
    bool isCompleted = count >= goal;
    string statusSuffix = " *";
    string line = "";

    if (isCompleted)
      line += $"{count} {name}{statusSuffix}";
    else
      line += $"{count}/{goal} {name}";

    return line + "^";
  }
}
