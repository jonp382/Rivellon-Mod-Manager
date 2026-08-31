# Rivellon Mod Manager
NOTE! This app is a work in progress.

This Mod Manager supports Divinity: Original Sin 2, by Larian Studios.

This tool is cross-platform and supports any desktop operating system. It is developed and tested primarily on Linux (Ubuntu).

It was developed as a cross-platform alternative to LaughingLeader's Divinity Mod Manager for those who do not use Windows. Their mod manager is a fantastic tool and I highly recommend it for Windows users, but unfortunately it is infamously tricky to get running on non-Windows systems.

# Features
Currently, it supports:
* Profile selection
* Reading in "modsettings.lsx" files to obtain the current load order
* Reading and parsing the PAK files in the mods directory to obtain all available mods
* Re-ordering, enabling and disabling mods
* Dependency checking, and highlighting invalid mods
  * Example: missing dependency (disabled, or not installed), or dependency is loaded after the mod that needs it.
* Saving and exporting the current load order to the target profile
* Basic user settings, including setting the GameData path, the current profile, and a light/dark theme toggle checkbox.

# Planned Features
Features that are planned, but not yet implemented, include:
* Importing mods from ZIP, PAK formats via the mod manager
* Importing and exporting mod order to share with friends
  * This would also enable storing mod order separate of profiles, for example to restore an old load order.
  * This must also be compatible with LaughingLeader's format, so a Windows user running his tool and a Linux user running this tool can seamlessly share load orders.
* Script extender detection and management

## Long-term Plans
* General feature-parity with Laughing Leader's Divinity Mod Manager in most regards
* Baldurs Gate 3 support (and any future Larian games)

# Credits
* [LaughingLeader](https://github.com/LaughingLeader) for their [Divinity Mod Manager](https://github.com/LaughingLeader-DOS2-Mods/DivinityModManager) and [BG3 Mod Manager](https://github.com/laughingleader/bg3modmanager) apps. These tools are the gold standard for Larian games modding. I used them as feature and design references while creating this app.
* [Norbyte](https://github.com/Norbyte) for [LSLib](https://github.com/Norbyte/lslib). This is used extensively to manage Larian's file formats.
* Larian Studios, for their excellent Divinity: Original Sin 2 game, for which this app was made.
