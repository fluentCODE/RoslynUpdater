using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynUpdater
{
    public class SolutionProcessor
    {
        public SolutionProcessor(string solutionDirectory)
        {
            SolutionDirectory = solutionDirectory;
        }

        public SolutionProcessor WithInternalsVisibleTo(string assemblyName)
        {
            if (!internalsVisibleToAssembliesHash.Contains(assemblyName))
            {
                InternalsVisibleToAssemblies.Add(assemblyName);
                internalsVisibleToAssembliesHash.Add(assemblyName);
            }

            return this;
        }

        public SolutionProcessor WithLinkedFile(string filePath)
        {
            LinkedFiles.Add(SolutionDirectory);
            return this;
        }

        public SolutionProcessor WithFindReplace(string filePath, string find, string replace)
        {
            var realPath = Path.Combine(SolutionDirectory, filePath);
            if (File.Exists(realPath))
            {
                string fileContents = null;
                if (!ModifiedFiles.ContainsKey(realPath))
                    ModifiedFiles.Add(realPath, File.ReadAllText(realPath));

                fileContents = ModifiedFiles[realPath];
                ModifiedFiles[realPath] = fileContents.Replace(find, replace);                
            }

            return this;
        }

        public SolutionProcessor ForEachProject(Action<ProjectProcessor> action)
        {
            foreach (var project in FindProjects())
            {
                var processor = new ProjectProcessor(project);
                projectProcessors.Add(processor);

                foreach (var linkedFile in LinkedFiles)
                    processor.WithLinkedFile(linkedFile.MakeRelative(Path.GetDirectoryName(processor.FilePath)));

                action(processor);
            }

            return this;
        }

        public IEnumerable<string> FindProjects()
        {
            return Directory.EnumerateFiles(SolutionDirectory, "*.csproj", SearchOption.AllDirectories)
                .Union(Directory.EnumerateFiles(SolutionDirectory, "*.vbproj", SearchOption.AllDirectories));
        }

        public void Save()
        {
            if (ModifiedFiles.Count > 0)
            {
                foreach (var file in ModifiedFiles)
                    File.WriteAllText(file.Key, file.Value);
            }

            if (InternalsVisibleToAssemblies.Count > 0)
            {
                var visibleToPath = Path.Combine(SolutionDirectory, "InjectedInternalsVisibleTo.cs");
                var internalsLines = InternalsVisibleToAssemblies.Select(x => $"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"{x}\")]");
                File.WriteAllLines(Path.Combine(SolutionDirectory, "InjectedInternalsVisibleTo.cs"), internalsLines.ToArray());
                foreach (var project in projectProcessors)
                    project.WithLinkedFile(visibleToPath.MakeRelative(Path.GetDirectoryName(project.FilePath))).Save();
            }
            else
            {
                foreach (var project in projectProcessors)
                    project.Save();
            }
        }

        public string SolutionDirectory { get; }

        public List<string> LinkedFiles { get; } = new List<string>();

        public List<string> InternalsVisibleToAssemblies { get; } = new List<string>();

        public Dictionary<string, string> ModifiedFiles { get; } = new Dictionary<string, string>();

        List<ProjectProcessor> projectProcessors = new List<ProjectProcessor>();
        HashSet<string> internalsVisibleToAssembliesHash = new HashSet<string>();
    }
}
