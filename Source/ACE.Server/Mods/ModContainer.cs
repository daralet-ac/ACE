using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using ACE.Common;
using McMaster.NETCore.Plugins;
using Serilog;

namespace ACE.Server.Mods
{
    public class ModContainer
    {
        private readonly ILogger _log = Log.ForContext<ModContainer>();
        private readonly TimeSpan RELOAD_TIMEOUT = TimeSpan.FromSeconds(3);

        public ModMetadata Meta { get; set; }
        public ModStatus Status = ModStatus.Unloaded;

        public Assembly ModAssembly { get; set; }
        public Type ModType { get; set; }
        public IHarmonyMod Instance { get; set; }

        // C:\ACE\Mods\SomeMod
        public string FolderPath { get; set; }
        // SomeMod
        public string FolderName //{ get; private set; }      
                => new DirectoryInfo(FolderPath).Name;
        // C:\ACE\Mods\SomeMod\SomeMod.dll
        public string DllPath =>
                Path.Combine(FolderPath, FolderName + ".dll");
        // C:\ACE\Mods\SomeMod\Meta.json
        public string MetadataPath =>
                Path.Combine(FolderPath, "Meta.json");
        // MyModNamespace.Mod
        public string TypeName =>
            ModAssembly.ManifestModule.ScopeName.Replace(".dll", "." + ModMetadata.TYPENAME);

        public PluginLoader Loader { get; private set; }
        private DateTime _lastChange = DateTime.Now;

        /// <summary>
        /// Sets up mod watchers for a valid mod Meta.json
        /// </summary>
        public void Initialize()
        {
            if (Meta is null)
            {
                _log.Warning("Unable to initialize. Check Meta.json...");
                return;
            }

            Loader = PluginLoader.CreateFromAssemblyFile(
                assemblyFile: DllPath,
                isUnloadable: true,
                sharedTypes: new Type[] { },
                configure: config =>
                {
                    config.EnableHotReload = Meta.HotReload;
                    config.IsLazyLoaded = false;
                    config.PreferSharedTypes = true;
                }
            );
            Loader.Reloaded += Reload;

            _log.Information("Set up {ModFolderName}", FolderName);
        }

        /// <summary>
        /// Loads assembly and activates an instance of the mod
        /// </summary>
        public void Enable()
        {
            if (Status == ModStatus.Active)
            {
                _log.Information("Mod is already enabled: {Mod}", Meta.Name);
                return;
            }
            if (Status == ModStatus.LoadFailure)
            {
                _log.Information("Unable to activate mod that failed to load: {Mod}", Meta.Name);
                return;
            }

            //Load assembly and create an instance if needed  (always?)
            if (!TryLoadModAssembly())
            {
                return;
            }

            if (!TryCreateModInstance())
            {
                return;
            }

            //Only mods with loaded assemblies that aren't active can be enabled
            if (Status != ModStatus.Inactive)
            {
                _log.Information("{Mod} is not inactive.", Meta.Name);
                return;
            }

            //Start mod and set status
            try
            {
                Instance?.Initialize();
                Status = ModStatus.Active;

                if (Meta.RegisterCommands)
                    this.RegisterUncategorizedCommands();

                _log.Information("Enabled mod `{Mod} (v{ModVersion})`.", Meta.Name, Meta.Version);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error enabling {Mod}", Meta.Name);
                Status = ModStatus.Inactive;    //Todo: what status?  Something to prevent reload attempts?
            }
        }

        public void RegisterCommands()
        {
            if (Meta.RegisterCommands)
                this.RegisterUncategorizedCommands();
        }

        //Todo: decide about removing the assembly?
        /// <summary>
        /// Disposes a mod and removes its chat commands
        /// </summary>
        public void Disable()
        {
            if (Status != ModStatus.Active)
                return;

            _log.Information("{ModFolderName} shutting down @ {ModShutdownTime}", FolderName, DateTime.Now);

            this.UnregisterAllCommands();

            try
            {
                Instance?.Dispose();
                Instance = null;
            }
            catch (TypeInitializationException ex)
            {
                _log.Error(ex, "Failed to dispose {ModFolderName}", FolderName);
            }
            Status = ModStatus.Inactive;
        }

        public void Restart()
        {
            Disable();
            Enable();
        }

        private bool TryLoadModAssembly()
        {
            if (!File.Exists(DllPath))
            {
                _log.Warning("Missing mod: {ModDll}", DllPath);
                return false;
            }

            try
            {
                //Todo: check if an assembly is loaded?
                //ModAssembly = Loader.LoadAssemblyFromPath(DllPath);
                ModAssembly = Loader.LoadDefaultAssembly();

                //Safer to use the dll to get the type than using convention
                //ModType = ModAssembly.GetTypes().Where(x => x.IsAssignableFrom(typeof(IHarmonyMod))).FirstOrDefault();
                ModType = ModAssembly.GetType(TypeName);

                if (ModType is null)
                {
                    Status = ModStatus.LoadFailure;
                    _log.Warning("Missing IHarmonyMod Type {ModTypeName} from {ModAssembly}", TypeName, ModAssembly);
                    return false;
                }
            }
            catch (Exception e)
            {
                Status = ModStatus.LoadFailure;
                _log.Error(e, "Failed to load mod file `{ModDll}`", DllPath);
                return false;
            }

            Status = ModStatus.Inactive;
            return true;
        }

        private bool TryCreateModInstance()
        {
            try
            {
                Instance = Activator.CreateInstance(ModType) as IHarmonyMod;
                _log.Information($"Created instance of {Meta.Name}");
            }
            catch (Exception ex)
            {
                Status = ModStatus.LoadFailure;
                _log.Error(ex, "Failed to create Mod instance: {Mod}", Meta.Name);
                return false;
            }

            return true;
        }

        public void SaveMetadata()
        {
            var json = JsonSerializer.Serialize(Meta, ConfigManager.SerializerOptions);
            var info = new FileInfo(MetadataPath);

            if (!info.RetryWrite(json))
                _log.Error("Saving metadata failed: {ModMetadataPath}", MetadataPath);
        }

        #region Events
        //If Loader has hot reload enabled this triggers after the assembly is loaded again (after GC)
        private void Reload(object sender, PluginReloadedEventArgs eventArgs)
        {
            var lapsed = DateTime.Now - _lastChange;
            if (lapsed < RELOAD_TIMEOUT)
            {
                _log.Information($"Not reloading {FolderName}: {lapsed.TotalSeconds}/{RELOAD_TIMEOUT.TotalSeconds}");
                return;
                //Shutdown();
            }

            Restart();
            _log.Information($"Reloaded {FolderName} @ {DateTime.Now} after {lapsed.TotalSeconds}/{RELOAD_TIMEOUT.TotalSeconds} seconds");
        }


        private void ModDll_Changed(object sender, FileSystemEventArgs e)
        {
            //Todo: Rethink reload in progress?
            var lapsed = DateTime.Now - _lastChange;
            if (lapsed < RELOAD_TIMEOUT)
            {
                //log.Info($"Not reloading {FolderName}: {lapsed.TotalSeconds}/{RELOAD_TIMEOUT.TotalSeconds}");
                return;
            }

            _log.Information($"{FolderName} changed @ {DateTime.Now} after {lapsed.TotalMilliseconds}ms");
            _lastChange = DateTime.Now;

            Disable();
        }
        #endregion
    }

    public enum ModStatus
    {
        /// <summary>
        /// Assembly not loaded
        /// </summary>
        Unloaded,
        /// <summary>
        /// Assembly loaded but an instance is not active
        /// </summary>
        Inactive,
        /// <summary>
        /// Assembly is loaded and an instance is active
        /// </summary>
        Active,
        /// <summary>
        /// Assembly failed to load
        /// </summary>
        LoadFailure,

        //Todo: Decide on how to represent future conflicts/errors
        //NameConflict,       //Mod loaded but a higher priority mod has the same name
        //MissingDependency,  //Keeping it simple for now
        //Conflict,           //Loaded and conflict detected
    }
}
