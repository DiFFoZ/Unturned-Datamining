using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SDG.Framework.IO;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace SDG.Framework.Modules;

public class ModuleHook : MonoBehaviour
{
    protected class AssemblyFileSettings
    {
        public string absolutePath;

        public bool loadAsByteArray;
    }

    private static List<IModuleNexus> coreNexii;

    protected static Dictionary<string, AssemblyFileSettings> nameToPath;

    protected static Dictionary<string, Assembly> nameToAssembly;

    private static CommandLineFlag shouldLogAssemblyResolve = new CommandLineFlag(defaultValue: false, "-LogAssemblyResolve");

    public static List<Module> modules { get; protected set; }

    public static Assembly coreAssembly { get; protected set; }

    public static Type[] coreTypes { get; protected set; }

    private static bool shouldLoadModules => true;

    public static event ModulesInitializedHandler onModulesInitialized;

    public static event ModulesShutdownHandler onModulesShutdown;

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

    public static Assembly resolveAssemblyPath(string path)
    {
        return resolveAssemblyName(AssemblyName.GetAssemblyName(path).FullName);
    }

    protected Assembly handleAssemblyResolve(object sender, ResolveEventArgs args)
    {
        if (string.Equals(args.Name, "Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
        {
            UnturnedLog.info("Redirecting Assembly-CSharp-firstpass to com.rlabrecque.steamworks.net for {0}", args.RequestingAssembly);
            return typeof(SteamAPI).Assembly;
        }
        if (string.Equals(args.Name, "Steamworks.NET, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
        {
            UnturnedLog.info("Redirecting Steamworks.NET to com.rlabrecque.steamworks.net for {0}", args.RequestingAssembly);
            return typeof(SteamAPI).Assembly;
        }
        Assembly assembly = resolveAssemblyName(args.Name);
        if (assembly == null && (bool)shouldLogAssemblyResolve)
        {
            UnturnedLog.error("Unable to resolve dependency \"" + args.Name + "\"! Include it in one of your module assembly lists.");
        }
        return assembly;
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
        string[] directories = Directory.GetDirectories(path);
        foreach (string path2 in directories)
        {
            findModules(path2, configs);
        }
    }

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
                if (moduleAssembly.Role == EModuleRole.Client)
                {
                    moduleConfig.Assemblies.RemoveAt(num);
                }
                else
                {
                    _ = moduleAssembly.Role;
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
        nameToAssembly = new Dictionary<string, Assembly>();
        if (shouldLoadModules)
        {
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
        if (ModuleHook.onModulesInitialized != null)
        {
            ModuleHook.onModulesInitialized();
        }
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
        if (ModuleHook.onModulesShutdown != null)
        {
            ModuleHook.onModulesShutdown();
        }
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
            if (!type.IsAbstract && typeFromHandle.IsAssignableFrom(type))
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
