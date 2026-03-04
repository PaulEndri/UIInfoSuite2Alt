<p align="center">
  <img src=".github/assets/ui-info-suite-2-alternative-logo.png" alt="UI Info Suite 2 Alternative">
</p>

<p align="center">
  <img src=".github/assets/showcase-animation.webp" alt="Showcase">
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

![New features](.github/assets/new-features.png)
- **2.4.2+**
  - Better Game Menu compatibility ([Annosz/UIInfoSuite2#648](https://github.com/Annosz/UIInfoSuite2/pull/648)) [@KhloeLeclair](https://github.com/KhloeLeclair)
- **2.4.1+**
  - Cloudy Skies framework compatibility: weather icon now supports custom weather from mods like [Weather Wonders](https://www.nexusmods.com/stardewvalley/mods/23868)
- **2.4.0+**
  - Show icon if there's a Festival/Event tomorrow or today
  - Option added to require watching TV daily to make Luck/Weather icon visible
- **2.3.9+**
  - Add hotkey to open mod options menu directly (Default: F8)
- **2.3.8+**
  - Show bookseller icon when he is visiting town
  - Show mastery experience bar and XP gains when all skills are at level 10
  - Show bundle items indicator on Traveling Merchant icon when the merchant has items needed for bundles
  - Show quest count under journal icon
  - Show recipe item as Queen of Sauce icon with mini TV overlay

> Most new features can be toggled on/off in the mod's in-game options menu.

![Issues resolved from upstream](.github/assets/issues-resolved-from-upstream.png)
- **2.4.1+**
  - Add Cloudy Skies API compatibility for custom weather support ([Annosz/UIInfoSuite2#659](https://github.com/Annosz/UIInfoSuite2/issues/659)) [@toffi3](https://github.com/toffi3)
- **2.3.8+**
  - Fix fruit tree drop parsing for DAY_OF_WEEK and non-day conditions (e.g. LOCATION_IS_OUTDOORS)
  - Fix CC bundle tooltips showing after Joja route or CC completion ([Annosz/UIInfoSuite2#572](https://github.com/Annosz/UIInfoSuite2/issues/572)) [@littlerat07](https://github.com/littlerat07)
  - Fix Ginger and Spring Onion displaying as "Unknown crop" ([Annosz/UIInfoSuite2#660](https://github.com/Annosz/UIInfoSuite2/issues/660)) [@FiveMountain](https://github.com/FiveMountain)

![Pull requests from upstream](.github/assets/pull-requests-from-upstream.png)
- **2.3.8+**
  - Fix mushroom log and mossy seed effect ranges ([Annosz/UIInfoSuite2#641](https://github.com/Annosz/UIInfoSuite2/pull/641)) [@Disassembler0](https://github.com/Disassembler0)

![Added Mod Compatibility](.github/assets/added-mod-compatibility.png)
- [Better Game Menu](https://www.nexusmods.com/stardewvalley/mods/12667) by SiTheGreat1
- [Cloudy Skies](https://www.nexusmods.com/stardewvalley/mods/23868) by Kanadeyoru
- [Custom Bush](https://www.nexusmods.com/stardewvalley/mods/20619) by LeFauxMatt
- [Deluxe Journal](https://www.nexusmods.com/stardewvalley/mods/11436) by Molson
- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) by spacechase0
- [NPC Map Locations](https://www.nexusmods.com/stardewvalley/mods/239) by Bouhm and Pathoschild
- [Bigger Backpack](https://www.nexusmods.com/stardewvalley/mods/1845) by spacechase0
- [Level Extender](https://www.nexusmods.com/stardewvalley/mods/1471) by unidarkshin and f1r3w477
- [Better Farm Animal Variety](https://www.nexusmods.com/stardewvalley/mods/3273) by Paritee

---

> **Note:** All links above are for this fork, except issue/PR references and usernames in the "Issues resolved" and "Pull requests" sections which link to the upstream repo. Everything below is the original README and links from [UIInfoSuite2](https://github.com/Annosz/UIInfoSuite2).

---

# If you are here to install the mod

## We're on Nexus again!!
Big thank you to the Nexus support staff for clearing us to be back on the NexusMods site under the UIInfoSuite name!

**[Download Here](https://www.nexusmods.com/stardewvalley/mods/7098)**

### GitHub Download
**Go to the [Releases page](https://github.com/Annosz/UIInfoSuite2/releases) on the side, where you can always find the latest release. Download the UIInfoSuite2.zip file and copy it's content to the mod folder.**
![image](https://user-images.githubusercontent.com/10620868/145580465-5dc6cd97-e4da-4830-a639-8f3fb94a1001.png)
_Do **NOT** download the Source code (zip) or Source code (tar.gz). Also, do **NOT**  use the green Code > Download ZIP button on the main page. These methods will only give you the source code but you will not be able to run the mod and use it with Stardew Valley!_

**If you like the mod, you can help the development by [gifting me a coffee](https://www.buymeacoffee.com/Annosz). Actually, as I'm from a corrupt Eastern European country, this is worth more like a whole dinner for me - any donation is much appreciated.**

# UI Info Suite 2
_Ongoing maintenance for the original UI Info Suite mod for Stardew Valley._

UI Info Suite provides helpful information about things around you designed to help you be aware of what's going on without feeling like you're cheating.

This mod is a rewrite of cdaragorn's UIInfoSuite mod (which is based on Demiacle's UiModSuite), so 99% of the credit goes to those guys. My contribution started after the December of 2019, when the original project became abandoned. As this is a very useful mod, I wanted to continue providing support for newer game versions, fixing bugs and adding new requested features, so here we are!

The current features include:
- Display an icon that represents the current days luck
- Display experience point gains
- Display a dynamic experience bar that changes based on your current tool or location
- Display more accurate heart levels
- Display more information on item mouse overs, including items that are still needed for bundles
- View calendar and quest billboard anywhere
- Display icons over animals that need petting
- Display crop type and days until harvest
- Display icon when animal has item yield (milk, wool)
- Sprinkler, scarecrow, beehive and junimo hut ranges
- Display npc locations on map
- Skip the intro by pressing the Escape key
- Display an icon for Queen of Sauce when she is airing a recipe you don't already know
- Display an icon when Clint is upgrading one of your tools. Icon will tell you how long until the tool is finished and shows you which tool you are upgrading.
- Display an icon for items that can be donated to the museum for the Complete Collection achievement
- Display an icon for items that can be shipped for the Full Shipment achievement
- Display an icon for the next day's weather conditions
- ... and also a new tab added to the options menu that allows turning each individual mod on or off whenever you want.

Known issues:
- In multiplayer only the host can see the correct location of NPC's on the map. If you face this issue, use the mod NPC Map Locations by Bouhm, which solves this problem. As it is a mod specialized for extending the map, it gets priority before UI Info Suite 2 and disables this mod's map features!

Compatibility is assured with:
- NPC Map Locations by Bouhm (gets priority before UI Info Suite 2 and disables this mod's map features)
- Bigger Backpack by spacechase0
- Level Extender by DevinLematty (probably?)
- Better Farm Animal Variety by Paritee

# Current collaborators
<table>
<tr>
    <td align="center">
        <a href="https://github.com/Annosz">
            <img src="https://avatars.githubusercontent.com/u/10620868?v=4" width="100;" alt="Annosz"/>
        </a>
        <br />
        <sub><b>Ádám Tóth</b></sub>
        <br />
        <a href="https://github.com/Annosz/UIInfoSuite2/commits?author=Annosz" title="Code">💻</a> <a href="https://github.com/Annosz/UIInfoSuite2/pulls?q=is%3Apr+reviewed-by%3AAnnosz" title="Reviewed Pull Requests">👀</a></td>
    </td>
    <td align="center">
        <a href="https://github.com/drewhoener">
            <img src="https://avatars.githubusercontent.com/u/6218989?v=4" width="100;" alt="drewhoener"/>
        </a>
        <br />
        <sub><b>Drew Hoener</b></sub>
        <br />
        <a href="https://github.com/Annosz/UIInfoSuite2/commits?author=drewhoener" title="Code">💻</a> <a href="https://github.com/Annosz/UIInfoSuite2/pulls?q=is%3Apr+reviewed-by%3Adrewhoener" title="Reviewed Pull Requests">👀</a></td>
    </td>
    <td align="center">
        <a href="https://github.com/tqdv">
            <img src="https://avatars.githubusercontent.com/u/11901480?v=4" width="100;" alt="tqdv"/>
        </a>
        <br />
        <sub><b>Tilwa Qendov</b></sub>
        <br />
        <a href="https://github.com/Annosz/UIInfoSuite2/commits?author=tqdv" title="Code">💻</a> <a href="https://github.com/Annosz/UIInfoSuite2/pulls?q=is%3Apr+reviewed-by%3Atqdv" title="Reviewed Pull Requests">👀</a></td>
    </td></tr>
</table>

## All contributors

<a href="https://github.com/Annosz/UIInfoSuite2/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=Annosz/UIInfoSuite2" />
</a>

# Translating UI Info Suite 2
The mod can be translated into any language supported by the game, and SMAPI will automatically
use the right translations.

Contributions are welcome! See [Modding:Translations](https://stardewvalleywiki.com/Modding:Translations)
on the wiki for help contributing translations.

(❑ = untranslated, ↻ = partly translated, ✓ = fully translated)

locale      | status
----------- | :----------------
default     | [✓](UIInfoSuite2/i18n/default.json)
Chinese     | [✓](UIInfoSuite2/i18n/zh.json)
French      | [↻](UIInfoSuite2/i18n/fr.json)
German      | [✓](UIInfoSuite2/i18n/de.json)
Hungarian   | [↻](UIInfoSuite2/i18n/hu.json)
Italian     | [✓](UIInfoSuite2/i18n/it.json)
Japanese    | [✓](UIInfoSuite2/i18n/ja.json)
Korean      | [↻](UIInfoSuite2/i18n/ko.json)
[Polish]    | [✓](UIInfoSuite2/i18n/pl.json)
Portuguese  | [✓](UIInfoSuite2/i18n/pt.json)
Russian     | [✓](UIInfoSuite2/i18n/ru.json)
Spanish     | [↻](UIInfoSuite2/i18n/es.json)
[Thai]      | [✓](UIInfoSuite2/i18n/th.json)
Turkish     | [✓](UIInfoSuite2/i18n/tr.json)
[Ukrainian] | [✓](UIInfoSuite2/i18n/uk.json)

[Polish]: https://www.nexusmods.com/stardewvalley/mods/3616
[Thai]: https://www.nexusmods.com/stardewvalley/mods/7052
[Ukrainian]: https://www.nexusmods.com/stardewvalley/mods/8427
