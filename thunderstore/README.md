# FixPluginTypesSerialization

FOR PLUGIN MAKERS

Hook into the native Unity engine for adding BepInEx plugin assemblies into the assembly list that is normally used for the assemblies sitting in the game Managed/ folder.

This solve a bug where custom Serializable structs and such stored in plugin assemblies are not properly getting deserialized by the engine.

Note: you must *explicitly* enable this mod for your plugins. In your mod zip, include a `FixPluginTypesSerialization.txt` containing the filename of one DLL per line.

<!--
For example, for the TombRush mod, we use a `FixPluginTypesSerialization.txt` that looks like this:

```
TombRush.Common.dll
TombRush.Plugin.dll
```
-->

## Attribution

This is a customized version of FixPluginTypesSerialization by xiaoxiao921.

https://github.com/xiaoxiao921/FixPluginTypesSerialization
