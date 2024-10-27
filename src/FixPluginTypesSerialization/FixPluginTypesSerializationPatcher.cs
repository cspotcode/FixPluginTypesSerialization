using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using FixPluginTypesSerialization.Patchers;
using FixPluginTypesSerialization.Util;
using Mono.Cecil;

namespace FixPluginTypesSerialization
{
    internal static class FixPluginTypesSerializationPatcher
    {
        public static IEnumerable<string> TargetDLLs { get; } = new string[0];

        static FixPluginTypesSerializationPatcher() {
            var rootDir = BepInEx.Paths.PluginPath + "\\";
            var includeListFilename = "FixPluginTypesSerialization.txt";
            Log.Info($"Scanning for include-lists named {includeListFilename} in {rootDir}...");
            string relative(string path) {
                return path.Replace(rootDir, "");
            }
            // Find any FixPluginTypesSerialization.txt files in entire plugins directory
            var includePatterns = Directory.GetFiles(rootDir, includeListFilename, SearchOption.AllDirectories)
                .SelectMany(absPath => {
                    var path = relative(absPath);
                    Log.Info($"Reading include-list {path}");
                    return File.ReadAllLines(absPath)
                        .Select(line => new { Rule = line, IncludeListPath = path })
                        .ToList();
                });
            PluginPaths = Directory.GetFiles(BepInEx.Paths.PluginPath, "*.dll", SearchOption.AllDirectories)
                .Where(absPath => {
                    if(IsNetAssembly(absPath)) {
                        var included = includePatterns.FirstOrDefault(pattern => pattern.Rule == Path.GetFileName(absPath));
                        var path = relative(absPath);
                        if(included != null) {
                            Log.Info($"Assembly included: {path}");
                            Log.Info($"  Include-list file: {included.IncludeListPath}");
                            return true;
                        } else {
                            Log.Info($"Assembly excluded because no include-list mentioned it: {path}");
                        }
                    }
                    return false;
                })
                .ToList();
            PluginNames = PluginPaths.Select(Path.GetFileName).ToList();
        }

        public static List<string> PluginPaths;

        public static List<string> PluginNames;

        public static bool IsNetAssembly(string fileName)
        {
            try
            {
                AssemblyName.GetAssemblyName(fileName);
            }
            catch (BadImageFormatException)
            {
                return false;
            }

            return true;
        }

        public static void Patch(AssemblyDefinition ass)
        {
        }

        public static void Initialize()
        {
            Log.Init();

            foreach(var p in PluginPaths) {
                Log.Message("PluginPath:" + p);
            }

            try
            {
                InitializeInternal();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to initialize plugin types serialization fix: ({e.GetType()}) {e.Message}. Some plugins may not work properly.");
                Log.Error(e);
            }
        }

        private static void InitializeInternal()
        {
            DetourUnityPlayer();
        }

        private static unsafe void DetourUnityPlayer()
        {
            var unityDllPath = Path.Combine(BepInEx.Paths.GameRootPath, "UnityPlayer.dll");
            //Older Unity builds had all functionality in .exe instead of UnityPlayer.dll
            if (!File.Exists(unityDllPath))
            {
                unityDllPath = BepInEx.Paths.ExecutablePath;
            }
            
            Log.Message("unityDllPath: " + unityDllPath);

            static bool IsUnityPlayer(ProcessModule p) {
                Log.Message("IUP: " + p.ModuleName.ToLowerInvariant());
                return p.ModuleName.ToLowerInvariant().Contains("unityplayer");
            }

            var temp = Process.GetCurrentProcess().Modules
                .Cast<ProcessModule>();
            var proc = temp.FirstOrDefault(IsUnityPlayer) ?? Process.GetCurrentProcess().MainModule;

            var patternDiscoverer = new PatternDiscoverer(proc.BaseAddress, unityDllPath);
            CommonUnityFunctions.Init(patternDiscoverer);

            var awakeFromLoadPatcher = new AwakeFromLoad();
            var isAssemblyCreatedPatcher = new IsAssemblyCreated();
            var isFileCreatedPatcher = new IsFileCreated();
            var scriptingManagerDeconstructorPatcher = new ScriptingManagerDeconstructor();
            var convertSeparatorsToPlatformPatcher = new ConvertSeparatorsToPlatform();
            
            awakeFromLoadPatcher.Patch(patternDiscoverer, Config.MonoManagerAwakeFromLoadOffset);
            isAssemblyCreatedPatcher.Patch(patternDiscoverer, Config.MonoManagerIsAssemblyCreatedOffset);
            // if (!IsAssemblyCreated.IsApplied)
            // {
            //     isFileCreatedPatcher.Patch(patternDiscoverer, Config.IsFileCreatedOffset);
            // }
            convertSeparatorsToPlatformPatcher.Patch(patternDiscoverer, Config.ConvertSeparatorsToPlatformOffset);
            scriptingManagerDeconstructorPatcher.Patch(patternDiscoverer, Config.ScriptingManagerDeconstructorOffset);

            foreach(var m in Process.GetCurrentProcess().Modules
                .Cast<ProcessModule>()) {
                Log.Message("F: " + m.ModuleName.ToLowerInvariant());
            }
        }
    }
}
