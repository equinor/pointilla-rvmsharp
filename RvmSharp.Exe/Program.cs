﻿namespace RvmSharp.Exe
{
    using Ben.Collections.Specialized;
    using CommandLine;
    using Tessellation;
    using ShellProgressBar;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Containers;
    using Exporters;
    using Operations;
    using Primitives;
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    static class Program
    {
        private static void Main(string[] args)
        {
            // use full Profile Guided Optimization
            Environment.SetEnvironmentVariable("DOTNET_ReadyToRun", "0");
            Environment.SetEnvironmentVariable("DOTNET_TC_QuickJitForLoops", "1");
            Environment.SetEnvironmentVariable("DOTNET_TieredPGO", "1");

            var result = Parser.Default.ParseArguments<Options>(args).MapResult(RunOptionsAndReturnExitCode, HandleParseError);
            Environment.Exit(result);
        }

        private static int HandleParseError(IEnumerable<Error> e)
        {
            return -1;
        }

        private static int RunOptionsAndReturnExitCode(Options options)
        {
            var workload = CollectWorkload(options);

            var rvmStore = ReadRvmData(workload);

            var progressBar = new ProgressBar(2, "Connecting geometry");
            RvmConnect.Connect(rvmStore);
            progressBar.Tick();
            progressBar.Message = "Aligning geometry";
            RvmAlign.Align(rvmStore);
            progressBar.Tick();
            progressBar.Dispose();

            var leafs = rvmStore.RvmFiles.SelectMany(rvm => rvm.Model.Children.SelectMany(CollectGeometryNodes)).ToArray();
            progressBar = new ProgressBar(leafs.Length, "Tessellating");
            var meshes = leafs.AsParallel().Select(leaf =>
            {
                progressBar.Message = leaf.Name;
                var tesselatedMeshes = TessellatorBridge.Tessellate(leaf, options.Tolerance);
                progressBar.Tick();
                return (leaf.Name, tesselatedMeshes);
            }).ToArray();
            progressBar.Dispose();
            progressBar = new ProgressBar(meshes.Length, "Exporting");

            using var objExporter = new ObjExporter(options.Output);
            foreach ((string objectName, IEnumerable<Mesh> primitives) in meshes)
            {
                objExporter.StartObject(objectName);
                foreach (var primitive in primitives)
                    objExporter.WriteMesh(primitive);
                progressBar.Tick();
            }

            progressBar.Dispose();

            Console.WriteLine("Done!");
            return 0;
        }

        private static (string rvmFilename, string? txtFilename)[] CollectWorkload(Options options)
        {
            var regexFilter = options.Filter != null ? new Regex(options.Filter) : null;
            var directories = options.Inputs.Where(Directory.Exists).ToArray();
            var files = options.Inputs.Where(File.Exists).ToArray();
            var missingInputs = options.Inputs.Where(i => !files.Contains(i) && !directories.Contains(i)).ToArray();

            if (missingInputs.Any())
            {
                throw new FileNotFoundException(
                    $"Missing file or folder: {Environment.NewLine}{string.Join(Environment.NewLine, missingInputs)}");
            }

            var inputFiles =
                directories.SelectMany(directory => Directory.GetFiles(directory, "*.rvm")) // Collect RVMs
                    .Concat(directories.SelectMany(directory => Directory.GetFiles(directory, "*.txt"))) // Collect TXTs
                    .Concat(files) // Append single files
                    .Where(f => regexFilter == null || regexFilter.IsMatch(Path.GetFileName(f))) // Filter by regex
                    .GroupBy(Path.GetFileNameWithoutExtension).ToArray(); // Group by filename (rvm, txt)

            var workload = (from filePair in inputFiles
                select filePair.ToArray()
                into filePairStatic
                let rvmFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".rvm"))
                let txtFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".txt"))
                select (rvmFilename, txtFilename)).ToArray();

            var result = new List<(string, string?)>();
            foreach ((string? rvmFilename, string? txtFilename) in workload)
            {
                if (rvmFilename == null)
                    Console.WriteLine(
                        $"No corresponding RVM file found for attributes: '{txtFilename}', the file will be skipped.");
                else
                    result.Add((rvmFilename, txtFilename));
            }

            return result.ToArray();
        }

        private static RvmStore ReadRvmData(IReadOnlyCollection<(string rvmFilename, string? txtFilename)> workload)
        {
            using var progressBar = new ProgressBar(workload.Count, "Parsing input");

            var stringInternPool = new InternPool();
            var rvmFiles = workload.Select(filePair =>
            {
                (string rvmFilename, string? txtFilename) = filePair;
                progressBar.Message = Path.GetFileNameWithoutExtension(rvmFilename);
                using var stream = File.OpenRead(rvmFilename);
                var rvmFile = RvmParser.ReadRvm(stream);
                if (!string.IsNullOrEmpty(txtFilename))
                {
                    rvmFile.AttachAttributes(txtFilename, ImmutableList<string>.Empty, stringInternPool);
                }

                progressBar.Tick();
                return rvmFile;
            }).ToArray();
            var rvmStore = new RvmStore();
            rvmStore.RvmFiles.AddRange(rvmFiles);
            return rvmStore;
        }

        private static IEnumerable<RvmNode> CollectGeometryNodes(RvmNode root)
        {
            if (root.Children.OfType<RvmPrimitive>().Any())
                yield return root;
            foreach (var geometryNode in root.Children.OfType<RvmNode>().SelectMany(CollectGeometryNodes))
                yield return geometryNode;
        }
    }
}