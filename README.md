<p align="center">
  <img src=".github/assets/ui-info-suite-2-alternative-logo.png" alt="UI Info Suite 2 Alternative">
</p>

<p align="center">
  <img src=".github/assets/showcase-animation-2-compressed.webp" alt="Showcase">
</p>

<p align="center">
  This is an alternative fork of <a href="https://github.com/Annosz/UIInfoSuite2">UIInfoSuite2</a> with additional features and bug fixes.
</p>

<p>
  <h3><strong>Why Alternative?</strong></h3>
  Since upstream development has slowed, this fork aims to keep the mod alive by fixing outstanding issues and adding features that felt missing. All credit for the mod goes to them.
  <br><br>
  Thanks to <a href="https://github.com/Annosz">Annosz</a>, <a href="https://github.com/tqdv">tqdv</a>, <a href="https://github.com/drewhoener">drewhoener</a>, <a href="https://github.com/cdaragorn">cdaragorn</a>, and all the contributors who built and maintained UIInfoSuite over the years. Their work made this mod what it is today.
</p>

<h2 align="center">
  <a href="https://github.com/dazuki/UIInfoSuite2Alt/releases/latest"><strong>Download Latest Release</strong></a>
</h2>

![New features](.github/assets/patch-notes.png)

> Most new features can be toggled on/off in the mod's in-game options menu.

- **v2.6.7**
  - Machine icons now have a mode selector (Off, Toggle, Hold) with hold-to-show keybind support
  - Fix tooltip text for bushes in green houses to ignore season checks
  - Fix experience-specific settings not being applied after game launch
- **v2.6.6**
  - Hotfix for custom weather icons using Cloudy Skies framework
- **v2.6.5**
  - Show a small item icon on machines that are currently processing, so you can see at a glance what each machine is working on without hovering (toggleable with F10 keybind)
  - Show the fish species icon on fish ponds so you can easily see what fish is in each pond
  - Show fish pond tooltip on hover with population, spawn timing, quest countdown, and golden cracker status
- **v2.6.4**
  - Add TV Fortune icon style for the luck HUD icon
  - Luck icon style is now a dropdown selector (Clover, Dice, TV Fortune) instead of a checkbox toggle
  - Improved vertical icon stacking: icons now start below the quest journal counter
- **v2.6.3**
  - Show tool upgrade icon for modded tools (e.g. [The Love of Cooking](https://www.nexusmods.com/stardewvalley/mods/6830))
  - Show festival/event start and end times on the festival reminder icon tooltip
  - Fix error/crash when modded animals have missing or unregistered produce data
  - Fix festival reminder not showing tomorrow's festival when a passive event is active today
- **v2.6.2**
  - Add Quest Board selection menu for [Ridgeside Village](https://www.nexusmods.com/stardewvalley/mods/7286)
  - Add Special Orders selection menu for [Visit Mount Vapius](https://www.nexusmods.com/stardewvalley/mods/9600)
  - Fix fruit tree tooltip showing "(no translation:...)" for Content Patcher mods with mismatched i18n keys (e.g. [Perfect Fruit](https://www.nexusmods.com/stardewvalley/mods/38413))
  - Fix cask tooltip calculation
- **v2.6.1**
  - Show [Stardew Aquarium](https://www.nexusmods.com/stardewvalley/mods/6372) donation indicator on fish item tooltips (Curator headshot, like Gunther for museum)
  - Add option to stack HUD icons vertically (downward from journal icon) instead of horizontally
  - Fix HUD icons and overlays showing during overnight farm events and minigames
  - Fix Traveling Merchant icon being invisible or look strange with certain Content Patcher mods
- **v2.6.0**
  - Show icon when there is extra forage to gather on The Beach during Summer
  - Show icon when there is a Pot of Gold at the End of the Rainbow 🌈
  - Show icon for Traveling merchant for [Ridgeside Village](https://www.nexusmods.com/stardewvalley/mods/7286) on Wednesdays
  - Add Special Orders board selection menu when detecting more available boards from mods (Currently: RSV, SBV & VMV)
  - Add gamepad navigation support for Calendar, Billboard, Special Orders, and Qi Orders icons in inventory
  - Add missing tree types to i18n for [Visit Mount Vapius](https://www.nexusmods.com/stardewvalley/mods/9600) and [Sunberry Village](https://www.nexusmods.com/stardewvalley/mods/11111)([Rose and the Alchemist](https://www.nexusmods.com/stardewvalley/mods/32385))
  - Improved fruit tree name resolution and tooltips for modded fruit trees
- **v2.5.5**
  - Add keybind to open Monster eradication goals (Default F9)
  - Fix tooltip showing up during special events/festivals/cutscenes
- **v2.5.4**
  - Fixed fertilizer string overlapping tooltip when using mods such as [Ultimate Fertilizer](https://www.nexusmods.com/stardewvalley/mods/21318)
- **v2.5.3**
  - (Hotfix)Reverted `LevelUp.ogg` to `LevelUp.wav` to fix certain audio/config issues in-game
- **v2.5.2**
  - Add [Cornucopia](https://www.nexusmods.com/stardewvalley/mods/19508) Corpse Flower & Date Palm tree to known treetypes
  - Add warning log for unknown/missing tree types
  - Adjusted icon margin for Calendar/Billboard icons
- **v2.5.1**
  - Add colored tooltips to crops for quick and better readability
  - Add [Cornucopia](https://www.nexusmods.com/stardewvalley/mods/19508) Sapodilla tree to known treetypes
  - Converted `LevelUp.wav` to `LevelUp.ogg` for smaller filesize
- **v2.5.0**
  - **Settings are now global**
    - All settings moved from per-save files to a single global `config.json`, applying across all save files
    - Old per-save settings files in `data/` are automatically renamed to `.json.old` on first load for reference
    - Full [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) (GMCM) integration for all settings
- **v2.4.9**
  - Show exclamation mark on billboard icon in inventory when a daily quest is available
  - Add Special Orders & Mr. Qi's Special Orders board icons in inventory with animated exclamation marks when new orders are available and be able to accept new SO's through the clickable icons
  - Add option to switch between the new clover luck icon and the classic dice icon
- **v2.4.8**
  - Replace dice luck icon with custom 4-leaf clover spritesheet (8 tiers of luck, including Special Charm and shrine extremes)
  - Add [Ferngill Simple Economy](https://www.nexusmods.com/stardewvalley/mods/21414) compatibility: item sell price tooltip no longer overlaps with Ferngill's supply/demand bar
- **v2.4.7**
  - Split "Show crop and barrel times" option into separate "Show crop and tree times" and "Show machine and barrel times" options
  - "Show bomb range" is now a standalone option, no longer nested under "Show scarecrow and sprinkler range"
  - Fix crash when opening the social tab if the social page hasn't fully loaded yet
  - Add warning log when social page is not ready to help identify mod conflicts
  - Fix fishing festival names (Trout Derby, SquidFest) not showing translated names in non-English locales
- **v2.4.6**
  - Add alpha pulsation and color to buff duration timers
  - Lower quest counter opacity to blend with background colors better
  - Fix tooltip showing wrong or no name for Custom Bush Mods
- **v2.4.5**
  - Add icon sorting in options
  - Show buff duration timers below buff icons
  - Changed font visuals for experience bar and experience gain for better look and feel
  - Fix calendar and billboard icons turned invisible when hovered/picking up items in inventory
  - Fix incorrect width on social page when using Better Game Menu
  - Fix hover-text no being shown for garden pots when they are placed on flooring ([Annosz/UIInfoSuite2#638](https://github.com/Annosz/UIInfoSuite2/pull/638)) [@NermNermNerm](https://github.com/NermNermNerm)
- **v2.4.4**
  - Split calendar and billboard into two separate icons using game item sprites (more Content Patcher friendly)
  - Fix duplicate birthday icons appearing in multiplayer
  - Fix bundle item detection not including the Missing Bundle (Abandoned JojaMart)
- **v2.4.3**
  - Always show fish identity when reeling in (Sonar Bobber effect without the item)
  - Optional quality star overlay on the fish icon with real-time perfect catch bonus
- **v2.4.2**
  - Better Game Menu compatibility ([Annosz/UIInfoSuite2#648](https://github.com/Annosz/UIInfoSuite2/pull/648)) [@KhloeLeclair](https://github.com/KhloeLeclair)
- **v2.4.1**
  - Cloudy Skies framework compatibility: weather icon now supports custom weather from mods like [Weather Wonders](https://www.nexusmods.com/stardewvalley/mods/23868) ([Annosz/UIInfoSuite2#659](https://github.com/Annosz/UIInfoSuite2/issues/659)) [@toffi3](https://github.com/toffi3)
- **v2.4.0**
  - Show icon if there's a Festival/Event tomorrow or today
  - Option added to require watching TV daily to make Luck/Weather icon visible
- **v2.3.9**
  - Add hotkey to open mod options menu directly (Default: F8)
- **v2.3.8**
  - Show bookseller icon when he is visiting town
  - Show mastery experience bar and XP gains when all skills are at level 10
  - Show bundle items indicator on Traveling Merchant icon when the merchant has items needed for bundles
  - Show quest count under journal icon
  - Show recipe item as Queen of Sauce icon with mini TV overlay
  - Fix fruit tree drop parsing for DAY_OF_WEEK and non-day conditions (e.g. LOCATION_IS_OUTDOORS)
  - Fix CC bundle tooltips showing after Joja route or CC completion ([Annosz/UIInfoSuite2#572](https://github.com/Annosz/UIInfoSuite2/issues/572)) [@littlerat07](https://github.com/littlerat07)
  - Fix Ginger and Spring Onion displaying as "Unknown crop" ([Annosz/UIInfoSuite2#660](https://github.com/Annosz/UIInfoSuite2/issues/660)) [@FiveMountain](https://github.com/FiveMountain)
  - Fix mushroom log and mossy seed effect ranges ([Annosz/UIInfoSuite2#641](https://github.com/Annosz/UIInfoSuite2/pull/641)) [@Disassembler0](https://github.com/Disassembler0)

![Added Mod Compatibility](.github/assets/added-mod-compatibility.png)

All mods listed here are **optional** and not required for UI Info Suite 2 Alternative.

- [Ridgeside Village](https://www.nexusmods.com/stardewvalley/mods/7286)
- [Sunberry Village](https://www.nexusmods.com/stardewvalley/mods/11111)
- [Visit Mount Vapius](https://www.nexusmods.com/stardewvalley/mods/9600)
- [Better Game Menu](https://www.nexusmods.com/stardewvalley/mods/12667)
- [Cloudy Skies](https://www.nexusmods.com/stardewvalley/mods/23868)
- [Custom Bush](https://www.nexusmods.com/stardewvalley/mods/20619)
- [Deluxe Journal](https://www.nexusmods.com/stardewvalley/mods/11436)
- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098)
- [NPC Map Locations](https://www.nexusmods.com/stardewvalley/mods/239)
- [Bigger Backpack](https://www.nexusmods.com/stardewvalley/mods/1845)
- [Level Extender](https://www.nexusmods.com/stardewvalley/mods/1471)
- [Better Farm Animal Variety](https://www.nexusmods.com/stardewvalley/mods/3273)
- [Ferngill Simple Economy](https://www.nexusmods.com/stardewvalley/mods/21414)
- [Stardew Aquarium](https://www.nexusmods.com/stardewvalley/mods/6372)
- [The Love of Cooking](https://www.nexusmods.com/stardewvalley/mods/6830)

<p align="center">
  <a href="https://ko-fi.com/dazuki89" target="_BLANK">
    <img src=".github/assets/kofi-icon.png" alt="Donate a Ko-fi!">
  </a>
  <br />
  If you like what i do, consider a donation for a cup of coffe ☕☺️
</p>
