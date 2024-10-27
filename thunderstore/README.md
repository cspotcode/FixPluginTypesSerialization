# FixPluginTypesSerialization

FOR PLUGIN MAKERS

Hook into the native Unity engine for adding BepInEx plugin assemblies into the assembly list that is normally used for the assemblies sitting in the game Managed/ folder.

This solve a bug where custom Serializable structs and such stored in plugin assemblies are not properly getting deserialized by the engine.

Note: you must *explicitly* enable this mod for your plugins.  Adding a FixPluginTypesSerialization.txt to your mod, containing the filename of one DLL per line.

<!--
For example, for the TombRush mod, we use a `FixPluginTypesSerialization.txt` that looks like this:

```
TombRush.Common.dll
TombRush.Plugin.dll
```
-->