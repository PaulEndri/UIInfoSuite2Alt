using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace UIInfoSuite2Alt.Options;

public class ModConfig
{
  public bool ShowOptionsTabInMenu { get; set; } = true;
  public string ApplyDefaultSettingsFromThisSave { get; set; } = "JohnDoe_123456789";
  public KeybindList OpenCalendarKeybind { get; set; } = KeybindList.ForSingle(SButton.B);
  public KeybindList OpenQuestBoardKeybind { get; set; } = KeybindList.ForSingle(SButton.H);
  public KeybindList ShowOneRange { get; set; } = KeybindList.ForSingle(SButton.LeftControl);
  public KeybindList ShowAllRange { get; set; } = KeybindList.Parse("LeftControl + LeftAlt");
  public KeybindList OpenModOptionsKeybind { get; set; } = KeybindList.ForSingle(SButton.F8);
}
