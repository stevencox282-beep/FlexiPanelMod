# FlexiPanelMod
This Mod is for Pantheon: Rise of the Fallen. It provides buff and debuff information about players and the enemy you currently have targetted.
1) Display Buff/Debuff/Both information in a panel
2) Allowss you to track this information for yourslef, your party or specific player names 
4) Access to additional enemy target information accessible via the /commands listed below
5) You can create any number of panels you wish, change their height and width (with restrictions) and change the Panels opacity
6) Allows for change of configuration without restarting the game

The following new commands have been added:
/fpshow - Shows all configured panels
/fphide - Hides all configured panels
/fpreload - Reloads the configuration
/fptarget prepend-text - Takes one optional argument prepend-text which will appear before the Target information. 
  E.G  /fptarget Pulling - Will display "Pulling Goblin Rockthrower(Lv.10), Rogue, Humanoid, Iron-Willed"

## Maximum Number Of Panels Vs Performance
The mod sets no upper limit on the number of panels.
If your machine is fast it may handle many panels, if it is very slow it may only handle a few.
All panels MUST finish all of their work in less than one second to ensure the times displayed update properly.  

## Panel Titles
You can provide a Name for a Panel by setting TargetOrTitle to "title" and then setting Title appropriatly.
Alternatly you can set TargetOrTitle to "target" and it will display the improved target information next to that panel instead.
You can not have both on a single panel.

## Config File And Location
The configuration file needed for this mod is FlexiPanelConfig.xml.  It MUST be placed in the <GamePath>/UserData/ directory.
The filename AND its contents are CASE-SENSITIVE and there is almost no error handling code written in the mod, be extra careful when changing things

### Resizing Panels And Restrictions
You can resize each panels' number of rows to display by setting the property RowsToDisplay in valid combinations of: 5,10,15,20,25,30,35.
All other numbers provided will be rounded down to the nearest valid value or defaulted to ten.
You can change the width of the panels (in pixels) by setting the property PanelWidthPx.

### How Buffs/Debuffs are displayed
The order of rows in the config file for a panel is the order they are displayed in that panel on the screen.
You can specify if a row is displayed using the Include property:
"[Me]" = Only shows the row if this buff/debuff is on you specifically.
"[Party]" = Only shows the row if this buff/debuff is on you specifically or your Party.
"Name1,Name2,Name3" = Only shows the row if this buff/debuff is on any player name defined in the comma seperated list.

Configured buffs/debuffs names (and only names) are not case sensitive.
Configured buffs/debuffs are selected if a buff CONTAINS the configured buff/debuff name.
  If you create a row with the Name "Mantle" it will include all tiers of Mantle (assuming all Tiers actually have Mantle in the name)
  If you create a row with the Name "Rip" (Bleed debuff) it will also include "Grip Of Stone" (Shaman Buff) as Grip contains Rip and Name is not case-sensitive.

### ExcludeAllBuffs / ExcludeAllDeBuffs Overrides
These two Panel properties exist to help keep panels small and specific.  Use these to set the general parameters for your Panel.
ExcludeAllBuffs/ExcludeAllDebuffs properties are dominant over IncludeAllBuffs/IncludeAllDebuffs.

### IncludeAllBuffs / IncludeAllDebuffs Overrides
These parameters are used to facilitate the create of generic catch all Buff or Debuff Panel.
When either of these properties are set you MUST have one row which provides the Include information.
When either of these properties are set to true the color of the bars will be based on the Spell Type not custom colors.
When IncludeAllBuffs is set to true the blacklist IncludeAllBuffsBlackList is used to prevent filling up the panel with common buffs.
When IncludeAllDebuffs is set to true the blacklist IncludeAllDebuffsBlackList is used to prevent filling up the panel with common debuffs.
ExcludeAllBuffs/ExcludeAllDebuffs properties are dominant over IncludeAllBuffs/IncludeAllDebuffs.

### Row Color And Color Availability
You can define the color a row will for any row that is NOT included in a Panel that have IncludeAllBuffs or IncludeAllDebuffs set to true.
You can set the color by specifying the color you want in the Color property.  E.G. Color="green"
There is no support for RGBA color configuration.
All available colors are defined by Unity. You can find the full Unity colour list for Unity (correct at time of writing) available at [Unity Color List](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Color.html).
If the user provides a color that Unity does not support it will default to dark orange.

### Panel Opacity
You can change how opaque each panel is by setting the panels PanelOpacity attribute in the configuration.  100 = fully opaque, 0 = fully transparent

### Confiuguring the improved target information
The <TargetInfo> node in the config file can be used to configure what is displayed in the improved target string

## Known Limitations
Panels are not dynamically resizable
Buff/Debuff data is removed on change of zone
The screen updates only once per second so there can be a small delay between target information / buffs changing and panels being updated
For debuffs that can only be applied once to an entity E.G. "Corrosive Brew" all casts of this debuff will still show up in the panel even if it has been over-written
Some buffs can not be tracked such as "Exhausted" and ALL Stances such as Rogues "Shadow Walk" or DireLords "Nightmarish"
Custom row colors are not supported for Panels that have IncludeAll<Buff/Debuff> set to true

## Installation
Install MelonLoader, following along with their [installation instructions](https://melonwiki.xyz/#/?id=requirements).
When selecting a version to install, tick `Enable Nightly builds` and install the latest nightly build (0.7.1-ci.2207 at time of writing).
Once you're finished, run the game once as normal to allow MelonLoader to generate the required libraries. Once this is done, close the game.

This project relies on libraries generated by MelonLoader. By default, the `GamePath` is set to the default Steam installation path. You can modify where your game is installed by editing the GamePath in the [Directory.Build.props](https://github.com/ModsOfPantheon/PantheonAddons/blob/master/Directory.Build.props) file. Please don't commit changes to this file. You can instruct git to ignore these changes locally without changes to `.gitignore` by running the following command in the root of the project:
```
git update-index --skip-worktree Directory.Build.props
```

## Disclaimer regarding cheating/anticheat
We believe that these addons do not violate the terms and conditions of Pantheon: Rise of the Fallen. From the EULA (as of 7th Jan 2025), we may not:

(b) use cheats, exploits, automation software, bots, hacks, mods or any unauthorized third-party software,
code or other device designed to modify or interfere with the Game or Service, or without Visionary Realms’
express written consent, modify or cause to be modified any files that are a part of the Game or Service;

However this Mod is to be used at your own risk. We do not accept any liability or responsibility for any actions taken against your account.