using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using XrdFileRedirector.Template;

namespace XrdFileRedirector;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    private string _contentPath;
    private string _configPath;
    private string _modPath;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _modConfig = context.ModConfig;


        // For more information about this template, please see
        // https://reloaded-project.github.io/Reloaded-II/ModTemplate/

        // If you want to implement e.g. unload support in your mod,
        // and some other neat features, override the methods in ModBase.
        
        _contentPath = Directory.GetParent(Directory.GetParent(
            Directory.GetParent(_modLoader.GetAppConfig().AppLocation)!.ToString())!.ToString()) + @"\REDGame\CookedPCConsole\Mods\";
        _configPath = Directory.GetParent(Directory.GetParent(
            Directory.GetParent(_modLoader.GetAppConfig().AppLocation)!.ToString())!.ToString()) + @"\REDGame\Config\DefaultEngine.ini";
        _modPath = _modLoader.GetDirectoryForModId(context.ModConfig.ModId);
        
        if (Directory.Exists(_contentPath))
            Directory.Delete(_contentPath, true);
        Directory.CreateDirectory(_contentPath);

        _modLoader.ModLoading += ModLoading;
    }

    private void ModLoading(IModV1 mod, IModConfigV1 config)
    {
        var allowedExtensions = new [] {".u", ".upk", ".umap"}; 

        foreach (string newPath in Directory.GetFiles(_modLoader.GetDirectoryForModId(config.ModId), "*.*",
                         SearchOption.AllDirectories).Where(file => allowedExtensions.Any(file.ToLower().EndsWith)).ToList())
        {
            var finalPath = newPath.Replace(_modLoader.GetDirectoryForModId(config.ModId), _contentPath);
            Directory.CreateDirectory(Path.GetDirectoryName(finalPath) ?? string.Empty);
            File.Copy(newPath, finalPath);
        }

        var scriptPackages = _modLoader.GetDirectoryForModId(config.ModId) + @"\ScriptPackages.txt";
        
        if (!File.Exists(_modPath + @"\DefaultEngine.ini")) return;
        if (!File.Exists(scriptPackages)) return;

        int lineCounter = 0;

        var sr = new StreamReader(_modPath + @"\DefaultEngine.ini");
        do
        {
            lineCounter++;
        } while (sr.ReadLine() != "+NonNativePackages=REDGameContent");

        var packages = File.ReadAllLines(scriptPackages).ToList();
        var allLines = File.ReadAllLines(_modPath + @"\DefaultEngine.ini").ToList();
        foreach (var package in packages)
        {
            allLines.Insert(lineCounter, "+NativePackages=" + package);
            lineCounter++;
        }
        File.WriteAllLines(_configPath , allLines.ToArray() );
    }
    
    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}