using System.Text.RegularExpressions;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AssetRipper.Primitives;
using Il2CppInterop.Generator;
using Cpp2IL.Core;
using Cpp2IL.Core.Api;
using Cpp2IL.Core.Extensions;
using Cpp2IL.Core.Logging;
using Il2CppInterop.Common;
using Il2CppInterop.Generator.Runners;
using LibCpp2IL.Wasm;

namespace ModLoader;
public class Il2CppAssemblyGenerator
{
    static string MainPath => Core.MainPath + "/Il2CppAssemblies";
    private static string GameAssemblyPath;
    public static void Prepare()
    {
        if (Path.Exists(MainPath))
        {
            return;
        }
        string Stubs = Path.Combine(Path.GetTempPath(), "Il2CppAssemblies");
        GameAssemblyPath = Core.DataPath + "lib/arm64/libil2cpp.so";
        if (!GenerateStubs(Stubs))
        {
            Core.Logger.Error("Failed to prepare assemblies");
            return;
        }
        ExecuteInterop(Stubs);
    }
    internal static bool GenerateStubs(string OutputFolder)
    {
        try
        {
            byte[] mdData = AssetManager.ReadAssetBytes("bin/Data/Managed/Metadata/global-metadata.dat");
            string mdPath = Path.Combine(Path.GetTempPath(), "global-metadata.dat");
            File.WriteAllBytes(mdPath, mdData);

            Cpp2IlApi.Init();
            Cpp2IlApi.ConfigureLib(false);
            var result = new Cpp2IlRuntimeArgs()
            {
                PathToAssembly = GameAssemblyPath,
                PathToMetadata = mdPath,
                UnityVersion = Core.UnityVersion,
                Valid = true,
                OutputRootDirectory = OutputFolder,
                OutputFormat = OutputFormatRegistry.GetFormat("dummydll"),
                ProcessingLayersToRun = [ProcessingLayerRegistry.GetById("attributeinjector")],
            };

            return RunCpp2IL(result);
        }
        catch (Exception e)
        {
            Core.Logger.Error("Failed to generate il2cpp stubs due to: " + e.Message + " at " + e.StackTrace);
            return false;
        }
    }

    // mostly copied from https://github.com/SamboyCoding/Cpp2IL/blob/development/Cpp2IL/Program.cs
    private static bool RunCpp2IL(Cpp2IlRuntimeArgs runtimeArgs)
    {
        var executionStart = DateTime.Now;

        runtimeArgs.OutputFormat?.OnOutputFormatSelected();

        WasmFile.RemappedDynCallFunctions = null;

        Cpp2IlApi.InitializeLibCpp2Il(runtimeArgs.PathToAssembly, runtimeArgs.PathToMetadata, runtimeArgs.UnityVersion);

        foreach (var (key, value) in runtimeArgs.ProcessingLayerConfigurationOptions)
            Cpp2IlApi.CurrentAppContext.PutExtraData(key, value);

        //Pre-process processing layers, allowing them to stop others from running
        Core.Logger.Msg("Pre-processing processing layers...");
        var layers = runtimeArgs.ProcessingLayersToRun.Clone();
        RunProcessingLayers(runtimeArgs,
            processingLayer => processingLayer.PreProcess(Cpp2IlApi.CurrentAppContext, layers));
        runtimeArgs.ProcessingLayersToRun = layers;

        //Run processing layers
        Core.Logger.Msg("Invoking processing layers...");
        RunProcessingLayers(runtimeArgs, processingLayer => processingLayer.Process(Cpp2IlApi.CurrentAppContext));

        var outputStart = DateTime.Now;

        if (runtimeArgs.OutputFormat != null)
        {
            Core.Logger.Msg(
                $"Outputting as {runtimeArgs.OutputFormat.OutputFormatName} to {runtimeArgs.OutputRootDirectory}...");
            runtimeArgs.OutputFormat.DoOutput(Cpp2IlApi.CurrentAppContext, runtimeArgs.OutputRootDirectory);
            Core.Logger.Msg($"Finished outputting in {(DateTime.Now - outputStart).TotalMilliseconds}ms");
        }
        else
        {
            Core.Logger.Warning(
                "No output format requested, so not outputting anything. The il2cpp game loaded properly though! (Hint: You probably want to specify an output format, try --output-as)");
        }

        Cpp2IlPluginManager.CallOnFinish();

        File.Delete(runtimeArgs
            .PathToMetadata); // because we extracted it from the apk's assets folder; only purpose was this

        Core.Logger.Msg($"Done. Total execution time: {(DateTime.Now - executionStart).TotalMilliseconds}ms");
        return true;
    }

    private static void RunProcessingLayers(Cpp2IlRuntimeArgs runtimeArgs, Action<Cpp2IlProcessingLayer> run)
    {
        foreach (var processingLayer in runtimeArgs.ProcessingLayersToRun)
        {
            var processorStart = DateTime.Now;

            Core.Logger.Msg($"    {processingLayer.Name}...");
            
                try
                {
                    run(processingLayer);
                }
                catch (Exception e)
                {
                    Core.Logger.Error($"Processing layer {processingLayer.Id} threw an exception: {e}");
                }

            Core.Logger.Msg(
                $"    {processingLayer.Name} finished in {(DateTime.Now - processorStart).TotalMilliseconds}ms");
        }
    }

    public static void ExecuteInterop(string Path)
    {
        Core.Logger.Msg("Reading dumped assemblies for interop generation...");
        var resolver = new InteropResolver();
        var inputAssemblies = Directory.GetFiles(Path)
            .Where(f => f.EndsWith(".dll"))
            .Select(f => ModuleDefinition.FromFile(f, new ModuleReaderParameters() { ModuleResolver = resolver }))
            .Select(f => { resolver.Add(f); return f; })
            .Select(f => f.Assembly)
            .ToList();
        
        var opts = new GeneratorOptions
        {
            GameAssemblyPath = GameAssemblyPath, // Path to GameAssembly.dll
            Source = inputAssemblies, // List of Cpp2Il dummy assemblies loaded into Cecil
            OutputDir = MainPath, // Path to which generate the assemblies
            UnityBaseLibsDir = null // Path to managed Unity core libraries (UnityEngine.dll etc)
        };

        Il2CppInteropGenerator.Create(opts)
            .AddInteropAssemblyGenerator()
            .AddLogger(Core.Logger.Instance)
            .Run();
    }
}
internal class InteropResolver : INetModuleResolver
{
    private readonly Dictionary<string, ModuleDefinition> _cache = new();
        
    public void Dispose()
    {
        _cache.Clear();
    }
        
    internal void Add(ModuleDefinition module)
    {
        _cache[module.Name] = module;
    }

    public ModuleDefinition Resolve(string name)
    {
        return _cache.GetValueOrDefault(name);
    }
}