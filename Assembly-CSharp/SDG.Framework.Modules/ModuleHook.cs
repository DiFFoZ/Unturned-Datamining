using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SDG.Framework.IO;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using Unturned.SystemEx;

namespace SDG.Framework.Modules;

/// <summary>
/// Runs before everything else to find and load modules.
/// </summary>
public class ModuleHook : MonoBehaviour
{
    protected class AssemblyFileSettings
    {
        public string absolutePath;

        public bool loadAsByteArray;
    }

    private static List<IModuleNexus> coreNexii;

    protected static Dictionary<string, AssemblyFileSettings> nameToPath;

    /// <summary>
    /// These are *.dll files discovered in the modules folder.
    /// </summary>
    protected static Dictionary<AssemblyName, string> discoveredNameToPath;

    protected static Dictionary<string, Assembly> nameToAssembly;

    /// <summary>
    /// Should missing DLLs be logged?
    /// Opt-in because RocketMod has its own handler.
    /// </summary>
    private static CommandLineFlag shouldLogAssemblyResolve = new CommandLineFlag(defaultValue: false, "-LogAssemblyResolve");

    /// <summary>
    /// Should vanilla search for *.dll files?
    /// Can be turned off in case it conflicts with third-party search mechanism.
    /// </summary>
    private static CommandLineFlag shouldSearchModulesForDLLs = new CommandLineFlag(defaultValue: true, "-NoVanillaAssemblySearch");

    public static List<Module> modules { get; protected set; }

    /// <summary>
    /// Temporarily contains Unturned's code untils it's moved into modules.
    /// </summary>
    public static Assembly coreAssembly { get; protected set; }

    /// <summary>
    /// Temporarily contains <see cref="P:SDG.Framework.Modules.ModuleHook.coreAssembly" /> types.
    /// </summary>
    public static Type[] coreTypes { get; protected set; }

    /// <summary>
    /// Should module assemblies be loaded?
    /// </summary>
    private static bool shouldLoadModules
    {
        get
        {
            if (Dedicator.IsDedicatedServer)
            {
                return true;
            }
            return !Dedicator.hasBattlEye;
        }
    }

    /// <summary>
    /// Called once after all startup enabled modules are loaded. Not called when modules are initialized due to enabling/disabling.
    /// </summary>
    public static event ModulesInitializedHandler onModulesInitialized;

    /// <summary>
    /// Called once after all modules are shutdown. Not called when modules are shutdown due to enabling/disabling.
    /// </summary>
    public static event ModulesShutdownHandler onModulesShutdown;

    /// <summary>
    /// Event for plugin frameworks (e.g., Rocket) to override AssemblyResolve handling.
    /// </summary>
    public static event ResolveEventHandler PreVanillaAssemblyResolve;

    public static event ResolveEventHandler PreVanillaAssemblyResolvePostRedirects;

    public static event ResolveEventHandler PostVanillaAssemblyResolve;

    /// <summary>
    /// Find modules containing an assembly with the Both_Required role.
    /// </summary>
    /// <param name="result">Modules to append to.</param>
    public static void getRequiredModules(List<Module> result)
    {
        if (modules == null || result == null)
        {
            return;
        }
        for (int i = 0; i < modules.Count; i++)
        {
            Module module = modules[i];
            if (module == null)
            {
                continue;
            }
            ModuleConfig config = module.config;
            if (config == null)
            {
                continue;
            }
            for (int j = 0; j < config.Assemblies.Count; j++)
            {
                ModuleAssembly moduleAssembly = config.Assemblies[j];
                if (moduleAssembly != null && moduleAssembly.Role == EModuleRole.Both_Required)
                {
                    result.Add(module);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Find module using dependency name.
    /// </summary>
    /// <returns></returns>
    public static Module getModuleByName(string name)
    {
        if (modules == null)
        {
            return null;
        }
        for (int i = 0; i < modules.Count; i++)
        {
            Module module = modules[i];
            if (module != null && module.config != null && module.config.Name == name)
            {
                return module;
            }
        }
        return null;
    }

    public static void toggleModuleEnabled(int index)
    {
        if (index >= 0 && index < modules.Count)
        {
            Module module = modules[index];
            ModuleConfig config = module.config;
            config.IsEnabled = !config.IsEnabled;
            IOUtility.jsonSerializer.serialize(module.config, config.FilePath, isFormatted: true);
            updateModuleEnabled(index, config.IsEnabled);
        }
    }

    public static void registerAssemblyPath(string path)
    {
        registerAssemblyPath(path, loadAsByteArray: false);
    }

    public static void registerAssemblyPath(string path, bool loadAsByteArray)
    {
        AssemblyName assemblyName = AssemblyName.GetAssemblyName(path);
        if (!nameToPath.ContainsKey(assemblyName.FullName))
        {
            AssemblyFileSettings assemblyFileSettings = new AssemblyFileSettings();
            assemblyFileSettings.absolutePath = path;
            assemblyFileSettings.loadAsByteArray = loadAsByteArray;
            nameToPath.Add(assemblyName.FullName, assemblyFileSettings);
        }
    }

    public static Assembly resolveAssemblyName(string name)
    {
        if (nameToAssembly.TryGetValue(name, out var value))
        {
            return value;
        }
        if (nameToPath.TryGetValue(name, out var value2))
        {
            value = ((!value2.loadAsByteArray) ? Assembly.LoadFile(value2.absolutePath) : Assembly.Load(File.ReadAllBytes(value2.absolutePath)));
            nameToAssembly.Add(name, value);
            return value;
        }
        return null;
    }

    private static Assembly LoadAssemblyFromDiscoveredPaths(AssemblyName loadAssemblyName)
    {
        Assembly value = null;
        try
        {
            foreach (KeyValuePair<AssemblyName, string> item in discoveredNameToPath)
            {
                AssemblyName key = item.Key;
                string value2 = item.Value;
                if (!string.Equals(key.Name, loadAssemblyName.Name) || (!(loadAssemblyName.Version == null) && !(key.Version >= loadAssemblyName.Version)))
                {
                    continue;
                }
                if ((bool)shouldLogAssemblyResolve)
                {
                    UnturnedLog.info($"Using discovered assembly for \"{loadAssemblyName}\" at \"{value2}\"");
                }
                if (!nameToAssembly.TryGetValue(key.Name, out value))
                {
                    value = Assembly.Load(File.ReadAllBytes(value2));
                    if (value != null)
                    {
                        nameToAssembly.Add(key.Name, value);
                    }
                }
                break;
            }
        }
        catch (Exception e)
        {
            UnturnedLog.exception(e, $"Caught exception loading assembly for \"{loadAssemblyName}\" from discovered paths:");
        }
        return value;
    }

    public static Assembly resolveAssemblyPath(string path)
    {
        return resolveAssemblyName(AssemblyName.GetAssemblyName(path).FullName);
    }

    protected Assembly handleAssemblyResolve(object sender, ResolveEventArgs args)
    {
        if (ModuleHook.PreVanillaAssemblyResolve != null)
        {
            Assembly assembly = ModuleHook.PreVanillaAssemblyResolve(sender, args);
            if ((bool)shouldLogAssemblyResolve)
            {
                if (assembly != null)
                {
                    UnturnedLog.info($"PreVanillaAssemblyResolve found \"{assembly.FullName}\" for \"{args.RequestingAssembly}\"");
                }
                else
                {
                    UnturnedLog.info($"PreVanillaAssemblyResolve is bound but unable to find \"{args.Name}\" for \"{args.RequestingAssembly}\"");
                }
            }
            if (assembly != null)
            {
                return assembly;
            }
        }
        if (string.Equals(args.Name, "Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
        {
            if ((bool)shouldLogAssemblyResolve)
            {
                UnturnedLog.info("Redirecting Assembly-CSharp-firstpass to com.rlabrecque.steamworks.net for {0}", args.RequestingAssembly);
            }
            return typeof(SteamAPI).Assembly;
        }
        if (string.Equals(args.Name, "Steamworks.NET, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
        {
            if ((bool)shouldLogAssemblyResolve)
            {
                UnturnedLog.info("Redirecting Steamworks.NET to com.rlabrecque.steamworks.net for {0}", args.RequestingAssembly);
            }
            return typeof(SteamAPI).Assembly;
        }
        if (ModuleHook.PreVanillaAssemblyResolvePostRedirects != null)
        {
            Assembly assembly2 = ModuleHook.PreVanillaAssemblyResolvePostRedirects(sender, args);
            if ((bool)shouldLogAssemblyResolve)
            {
                if (assembly2 != null)
                {
                    UnturnedLog.info($"PreVanillaAssemblyResolvePostRedirects found \"{assembly2.FullName}\" for \"{args.RequestingAssembly}\"");
                }
                else
                {
                    UnturnedLog.info($"PreVanillaAssemblyResolvePostRedirects is bound but unable to find \"{args.Name}\" for \"{args.RequestingAssembly}\"");
                }
            }
            if (assembly2 != null)
            {
                return assembly2;
            }
        }
        Assembly assembly3 = resolveAssemblyName(args.Name);
        if (assembly3 != null)
        {
            return assembly3;
        }
        if ((bool)shouldSearchModulesForDLLs)
        {
            assembly3 = LoadAssemblyFromDiscoveredPaths(new AssemblyName(args.Name));
            if (assembly3 != null)
            {
                return assembly3;
            }
        }
        if ((bool)shouldLogAssemblyResolve)
        {
            UnturnedLog.error("Vanilla unable to resolve dependency \"" + args.Name + "\"! Please include it in one of your module assembly lists.");
        }
        if (ModuleHook.PostVanillaAssemblyResolve != null)
        {
            Assembly assembly4 = ModuleHook.PostVanillaAssemblyResolve(sender, args);
            if ((bool)shouldLogAssemblyResolve)
            {
                if (assembly4 != null)
                {
                    UnturnedLog.info($"PostVanillaAssemblyResolve found \"{assembly4.FullName}\" for \"{args.RequestingAssembly}\"");
                }
                else
                {
                    UnturnedLog.info($"PostVanillaAssemblyResolve is bound but unable to find \"{args.Name}\" for \"{args.RequestingAssembly}\"");
                }
            }
            if (assembly4 != null)
            {
                return assembly4;
            }
        }
        return null;
    }

    protected Assembly OnTypeResolve(object sender, ResolveEventArgs args)
    {
        if (args.Name.StartsWith("SDG.NetTransport."))
        {
            UnturnedLog.info("Redirecting type \"{0}\" assembly for {1}", args.Name, args.RequestingAssembly);
            return typeof(ITransportConnection).Assembly;
        }
        UnturnedLog.info("Unable to resolve type \"{0}\" for {1}", args.Name, args.RequestingAssembly);
        return null;
    }

    private static bool areModuleDependenciesEnabled(int moduleIndex)
    {
        ModuleConfig config = modules[moduleIndex].config;
        for (int i = 0; i < config.Dependencies.Count; i++)
        {
            ModuleDependency moduleDependency = config.Dependencies[i];
            int index = moduleIndex - 1;
            while (moduleIndex >= 0)
            {
                if (modules[index].config.Name == moduleDependency.Name && !modules[index].isEnabled)
                {
                    return false;
                }
                moduleIndex--;
            }
        }
        return true;
    }

    private static void updateModuleEnabled(int index, bool enable)
    {
        if (enable)
        {
            if (!modules[index].config.IsEnabled || !areModuleDependenciesEnabled(index) || isModuleDisabledByCommandLine(modules[index].config.Name))
            {
                return;
            }
            modules[index].isEnabled = true;
            for (int i = index + 1; i < modules.Count; i++)
            {
                for (int j = 0; j < modules[i].config.Dependencies.Count; j++)
                {
                    if (modules[i].config.Dependencies[j].Name == modules[index].config.Name)
                    {
                        updateModuleEnabled(i, enable: true);
                        break;
                    }
                }
            }
            return;
        }
        for (int num = modules.Count - 1; num > index; num--)
        {
            for (int k = 0; k < modules[num].config.Dependencies.Count; k++)
            {
                if (modules[num].config.Dependencies[k].Name == modules[index].config.Name)
                {
                    updateModuleEnabled(num, enable: false);
                    break;
                }
            }
        }
        modules[index].isEnabled = false;
    }

    /// <summary>
    /// Depending on the platform, assemblies are found in different directories.
    /// </summary>
    /// <returns>Root folder for modules.</returns>
    private string getModulesRootPath()
    {
        string pATH = ReadWrite.PATH;
        pATH += "/Modules";
        if (!Directory.Exists(pATH))
        {
            Directory.CreateDirectory(pATH);
        }
        return pATH;
    }

    /// <summary>
    /// Search Modules directory for .dll files and save their AssemblyName to discoveredNameToPath.
    /// </summary>
    private void DiscoverAssemblies()
    {
        try
        {
            string[] files = Directory.GetFiles(getModulesRootPath(), "*.dll", SearchOption.AllDirectories);
            foreach (string text in files)
            {
                try
                {
                    AssemblyName assemblyName = AssemblyName.GetAssemblyName(text);
                    UnturnedLog.info($"Discovered assembly \"{assemblyName}\" at \"{text}\"");
                    if (!discoveredNameToPath.TryGetValue(assemblyName, out var value))
                    {
                        discoveredNameToPath.Add(assemblyName, text);
                        UnturnedLog.info($"Discovered assembly \"{assemblyName}\" at \"{text}\"");
                    }
                    else
                    {
                        UnturnedLog.info($"Discovered duplicate of assembly \"{assemblyName}\" at \"{text}\" (first found at \"{value}\")");
                    }
                }
                catch (Exception ex)
                {
                    UnturnedLog.info("Caught exception trying to determine AssemblyName for dll \"" + text + "\": \"" + ex.Message + "\"");
                }
            }
        }
        catch (Exception e)
        {
            UnturnedLog.exception(e, "Caught exception discovering assemblies in Modules folder:");
        }
    }

    /// <summary>
    /// Search Modules directory for .module files and load them.
    /// </summary>
    private List<ModuleConfig> findModules()
    {
        List<ModuleConfig> list = new List<ModuleConfig>();
        string modulesRootPath = getModulesRootPath();
        findModules(modulesRootPath, list);
        return list;
    }

    private void findModules(string path, List<ModuleConfig> configs)
    {
        string[] files = Directory.GetFiles(path, "*.module");
        foreach (string text in files)
        {
            try
            {
                ModuleConfig moduleConfig = IOUtility.jsonDeserializer.deserialize<ModuleConfig>(text);
                if (moduleConfig == null)
                {
                    UnturnedLog.warn("Unable to parse module config file: " + text);
                    continue;
                }
                moduleConfig.DirectoryPath = path;
                moduleConfig.FilePath = text;
                moduleConfig.Version_Internal = Parser.getUInt32FromIP(moduleConfig.Version);
                for (int num = moduleConfig.Dependencies.Count - 1; num >= 0; num--)
                {
                    ModuleDependency moduleDependency = moduleConfig.Dependencies[num];
                    if (moduleDependency.Name == "Framework" || moduleDependency.Name == "Unturned")
                    {
                        moduleConfig.Dependencies.RemoveAtFast(num);
                    }
                    else
                    {
                        moduleDependency.Version_Internal = Parser.getUInt32FromIP(moduleDependency.Version);
                    }
                }
                configs.Add(moduleConfig);
            }
            catch (Exception e)
            {
                UnturnedLog.exception(e, "Caught exception parsing .module file: " + text);
            }
        }
        string[] directories = Directory.GetDirectories(path);
        foreach (string path2 in directories)
        {
            findModules(path2, configs);
        }
    }

    /// <summary>
    /// Orders configs by dependency and removes those that are missing files.
    /// </summary>
    private void sortModules(List<ModuleConfig> configs)
    {
        ModuleComparer comparer = new ModuleComparer();
        configs.Sort(comparer);
        for (int i = 0; i < configs.Count; i++)
        {
            ModuleConfig moduleConfig = configs[i];
            bool flag = true;
            for (int num = moduleConfig.Assemblies.Count - 1; num >= 0; num--)
            {
                ModuleAssembly moduleAssembly = moduleConfig.Assemblies[num];
                if (moduleAssembly.Role == EModuleRole.Client && Dedicator.IsDedicatedServer)
                {
                    moduleConfig.Assemblies.RemoveAt(num);
                }
                else if (moduleAssembly.Role == EModuleRole.Server && !Dedicator.IsDedicatedServer)
                {
                    moduleConfig.Assemblies.RemoveAt(num);
                }
                else
                {
                    bool flag2 = false;
                    for (int j = 1; j < moduleAssembly.Path.Length; j++)
                    {
                        if (moduleAssembly.Path[j] == '.' && moduleAssembly.Path[j - 1] == '.')
                        {
                            flag2 = true;
                            break;
                        }
                    }
                    if (flag2)
                    {
                        flag = false;
                        break;
                    }
                    string text = moduleConfig.DirectoryPath + moduleAssembly.Path;
                    if (!File.Exists(text))
                    {
                        flag = false;
                        UnturnedLog.warn("Module \"" + moduleConfig.Name + "\" missing assembly: " + text);
                        break;
                    }
                }
            }
            if (!flag || moduleConfig.Assemblies.Count == 0)
            {
                configs.RemoveAt(i);
                i--;
                UnturnedLog.info("Discard module \"" + moduleConfig.Name + "\" because it has no assemblies");
                continue;
            }
            for (int k = 0; k < moduleConfig.Dependencies.Count; k++)
            {
                ModuleDependency moduleDependency = moduleConfig.Dependencies[k];
                bool flag3 = false;
                for (int num2 = i - 1; num2 >= 0; num2--)
                {
                    if (configs[num2].Name == moduleDependency.Name)
                    {
                        if (configs[num2].Version_Internal >= moduleDependency.Version_Internal)
                        {
                            flag3 = true;
                        }
                        break;
                    }
                }
                if (!flag3)
                {
                    configs.RemoveAtFast(i);
                    i--;
                    UnturnedLog.warn("Discard module \"" + moduleConfig.Name + "\" because dependency \"" + moduleDependency.Name + "\" wasn't met");
                    break;
                }
            }
        }
    }

    private void loadModules()
    {
        modules = new List<Module>();
        nameToPath = new Dictionary<string, AssemblyFileSettings>();
        discoveredNameToPath = new Dictionary<AssemblyName, string>();
        nameToAssembly = new Dictionary<string, Assembly>();
        if (shouldLoadModules)
        {
            if ((bool)shouldSearchModulesForDLLs)
            {
                DiscoverAssemblies();
            }
            List<ModuleConfig> list = findModules();
            sortModules(list);
            if (list.Count > 0)
            {
                UnturnedLog.info($"Found {list.Count} module(s):");
            }
            for (int i = 0; i < list.Count; i++)
            {
                ModuleConfig moduleConfig = list[i];
                if (moduleConfig != null)
                {
                    UnturnedLog.info($"{i}: \"{moduleConfig.Name}\"");
                    Module item = new Module(moduleConfig);
                    modules.Add(item);
                }
            }
        }
        else
        {
            UnturnedLog.info("Disabling module loading because BattlEye is enabled");
        }
    }

    private static bool isModuleDisabledByCommandLine(string moduleName)
    {
        string commandLine = Environment.CommandLine;
        int num = commandLine.IndexOf(moduleName, StringComparison.OrdinalIgnoreCase);
        if (num == -1)
        {
            return false;
        }
        string text = "-disableModule/";
        int num2 = num - text.Length;
        if (num2 < 0)
        {
            return false;
        }
        if (commandLine.Substring(num2, text.Length) == text)
        {
            return true;
        }
        return false;
    }

    private void initializeModules()
    {
        if (modules == null)
        {
            return;
        }
        for (int i = 0; i < modules.Count; i++)
        {
            Module module = modules[i];
            ModuleConfig config = module.config;
            bool isEnabled;
            if (!config.IsEnabled)
            {
                isEnabled = false;
                UnturnedLog.info("Disabling module \"" + config.Name + "\" as requested by config");
            }
            else if (!areModuleDependenciesEnabled(i))
            {
                isEnabled = false;
                UnturnedLog.info("Disabling module \"" + config.Name + "\" because dependencies are disabled");
            }
            else if (isModuleDisabledByCommandLine(config.Name))
            {
                isEnabled = false;
                UnturnedLog.info("Disabling module \"" + config.Name + "\" as requested by command-line");
            }
            else
            {
                isEnabled = true;
            }
            module.isEnabled = isEnabled;
        }
        ModuleHook.onModulesInitialized?.Invoke();
    }

    private void shutdownModules()
    {
        if (modules == null)
        {
            return;
        }
        for (int num = modules.Count - 1; num >= 0; num--)
        {
            Module module = modules[num];
            if (module != null)
            {
                module.isEnabled = false;
            }
        }
        ModuleHook.onModulesShutdown?.Invoke();
    }

    public void awake()
    {
        AppDomain.CurrentDomain.AssemblyResolve += handleAssemblyResolve;
        AppDomain.CurrentDomain.TypeResolve += OnTypeResolve;
        coreAssembly = Assembly.GetExecutingAssembly();
        try
        {
            coreTypes = coreAssembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            coreTypes = ex.Types;
        }
        loadModules();
    }

    public void start()
    {
        coreNexii = new List<IModuleNexus>();
        coreNexii.Clear();
        Type typeFromHandle = typeof(IModuleNexus);
        for (int i = 0; i < coreTypes.Length; i++)
        {
            Type type = coreTypes[i];
            if (!type.IsAbstract && typeFromHandle.TryIsAssignableFrom(type))
            {
                IModuleNexus moduleNexus = Activator.CreateInstance(type) as IModuleNexus;
                try
                {
                    moduleNexus.initialize();
                }
                catch (Exception e)
                {
                    UnturnedLog.error("Failed to initialize nexus!");
                    UnturnedLog.exception(e);
                }
                coreNexii.Add(moduleNexus);
            }
        }
        initializeModules();
    }

    private void OnDestroy()
    {
        shutdownModules();
        for (int i = 0; i < coreNexii.Count; i++)
        {
            try
            {
                coreNexii[i].shutdown();
            }
            catch (Exception e)
            {
                UnturnedLog.error("Failed to shutdown nexus!");
                UnturnedLog.exception(e);
            }
        }
        coreNexii.Clear();
        AppDomain.CurrentDomain.AssemblyResolve -= handleAssemblyResolve;
        AppDomain.CurrentDomain.TypeResolve -= OnTypeResolve;
    }
}
