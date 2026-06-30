using System.Runtime.InteropServices;
using System.Text;
using ggrev2.core.redirector.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using Reloaded.Universal.Redirector.Interfaces;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace ggrev2.core.redirector;

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

    private string? _engineIniPath;

    private static List<string> _contentPaths = [];

    private WeakReference<IRedirectorController>? _controller;

    [Function(CallingConventions.MicrosoftThiscall)]
    private delegate void CachePath(nint @this, nint inPath);

    private static CachePath? _cachePath;

    [Function(CallingConventions.MicrosoftThiscall)]
    private delegate void CachePaths(nint @this);

    private static IHook<CachePaths>? _cachePathsHook;

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

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

        _modLoader.ModLoaded += ModLoaded;

        var baseAddress = GetModuleHandle("GuiltyGearXrd.exe");

        _cachePath =
            Marshal.GetDelegateForFunctionPointer<CachePath>(_hooks!.CreateWrapper<CachePath>(baseAddress + 0x84480));
        _cachePathsHook = _hooks.CreateHook<CachePaths>(CachePathsImpl, baseAddress + 0x862A0).Activate();

        _controller = _modLoader.GetController<IRedirectorController>();
        if (_controller != null && _controller.TryGetTarget(out var controller))
        {
            controller.AddRedirectFolder(_modLoader.GetDirectoryForModId("ggrev2.core.redirector") + @"\Config",
                @"\..\..\REDGame\Config");
        }

        if (!Directory.Exists(_modLoader.GetDirectoryForModId("ggrev2.core.redirector") + @"\Config"))
        {
            Directory.CreateDirectory(_modLoader.GetDirectoryForModId("ggrev2.core.redirector") + @"\Config");
        }
        
        _engineIniPath = _modLoader.GetDirectoryForModId("ggrev2.core.redirector") + @"\Config\DefaultEngine.ini";

        File.Delete(_engineIniPath);
        File.Copy(Directory.GetParent(
                Directory.GetParent(Directory.GetParent(_modLoader.GetAppConfig().AppLocation)!.ToString())!
                    .ToString()) + @"\REDGame\Config\DefaultEngine.ini",
            _engineIniPath, true);
    }

    private static void CachePathsImpl(nint @this)
    {
        foreach (var path in _contentPaths)
        {
            unsafe
            {
                fixed (byte* pathPtr = Encoding.Unicode.GetBytes(path))
                {
                    _cachePath!(@this, (nint)pathPtr);
                }
            }
        }

        unsafe
        {
            fixed (byte* pathPtr = Encoding.Unicode.GetBytes(@"..\..\REDGame\CookedPC"))
            {
                _cachePath!(@this, (nint)pathPtr);
            }
        }
    }

    private void ModLoaded(IModV1 mod, IModConfigV1 config)
    {
        if (Directory.Exists(_modLoader.GetDirectoryForModId(config.ModId) + @"\CookedPC"))
            _contentPaths.Add(_modLoader.GetDirectoryForModId(config.ModId) + @"\CookedPC");

        if (!File.Exists(_engineIniPath)) return;

        var scriptPackages = _modLoader.GetDirectoryForModId(config.ModId) + @"\ScriptPackages.txt";
        if (!File.Exists(scriptPackages)) return;

        var lineCounter = 0;

        List<string> allLines;

        using (var sr = new StreamReader(_engineIniPath))
        {
            do
            {
                lineCounter++;
            } while (sr.ReadLine() != "+NonNativePackages=REDGameContent");

            var packages = File.ReadAllLines(scriptPackages).ToList();
            allLines = File.ReadAllLines(_engineIniPath).ToList();
            foreach (var package in packages)
            {
                allLines.Insert(lineCounter, "+NativePackages=" + package);
                lineCounter++;
            }
        }

        File.WriteAllLines(_modLoader.GetDirectoryForModId("ggrev2.core.redirector") + @"\Config\DefaultEngine.ini",
            allLines.ToArray());
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion
}