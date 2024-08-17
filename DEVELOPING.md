# DEVELOPING
# Mod directory structure
```
mod
├── kr5
│   ├── balance
│   │   └── balance.lua
│   ├── data
│   │   └── animations
│   │       └── vile_spawner.lua
│   └── vscode-mobdebug.lua
└── mod.json
```
In this example, every file here will be patched into the game executable,
except for `mod.json`. `mod.json` contains the metadata for the mod that
will be used by NecroManager to initialize it.  
The root folder for the game will be the root folder of the mod, so to
patch the game file `Kingdom Rush Alliance/lib/middleclass.lua`,  
just put the file in `mod/lib/middleclass.lua`.  
An important thing to note is that you do not need to recompile your
mod scripts for them to work in the game.

# `mod.json` structure
```json
{
  "Name": "MyMod",
  "Description": "Example Mod",
  "Version": "1.0",
  "Author": "meow",
  "Game": "Kr5"
}
```
The `mod.json` file should be present in the root folder of your mod,
for example `mymod/mod.json`. Your mod's `mod.json` file should include
all of these attributes, or it might not be properly loaded by NecroManager.
The `"Game"` attribute is not *needed* for your mod to be loaded properly,
However your mod will not be able to be zip imported by NecroManger if it
is not present, so you should always include it.  
The `"Game"` attribute can have 4 possible options, and is **case-sensitive**.  
You can use this chart to find what you should set the `"Game"` attribute
to for your mod:
```
  Kr1        : Kingdom Rush
  Kr2        : Kingdom Rush: Frontiers
  Kr3        : Kingdom Rush: Origins
  Kr5        : Kingdom Rush: Alliance
```
###### If you were making a mod for Kingdom Rush: Frontiers, you would put `"Game": "Kr2"`
###### While for Kingdom Rush: Origins you would put `"Game": "Kr3"`

# Mod making best practices
 * Name the mod parent folder something unique, and try not to use special 
   characters in its name
   * Avoid characters like spaces and symbols (other than `-` and `_`)
 * Try patching as little files as you can, as this will help your mod's
   compatibility with other mods.
 * Only include scripts that are required for the mod, any scripts that
   are not different from the original game scripts should not be included.
 * When packing your mod as a zip file, take note of the zip file name.
   The name of the zip file will become the name of the mod's root folder

# Distributing your mod
Distributing your mod with NecroManager is very simple, just compress your
mod into a zip file. Anyone can use the "Import mod zip" button in the 
settings menu of the game to import your mod easily.  
Alternatively, people could manually install your 