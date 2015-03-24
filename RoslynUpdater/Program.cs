using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynUpdater
{
    class Program
    {
        static void Main(string[] args)
        {
            new SolutionProcessor(@"W:\Projects\roslyn\Src")
                .WithInternalsVisibleTo("Scrawl.CodeEngine.Roslyn")
                .WithInternalsVisibleTo("Scrawl.Plugins.Roslyn")
                .WithInternalsVisibleTo("OmniSharp")
                .WithFindReplace(
                    @"Workspaces\Core\Portable\Workspace\Host\Mef\MefHostServices.cs",
                    @"var assemblyName = new AssemblyName(string.Format(""{0}, Version={1}, Culture=neutral, PublicKeyToken={2}"", assemblySimpleName, assemblyVersion, publicKeyToken));",
                    @"var assemblyName = new AssemblyName(assemblySimpleName);"
                )
                .ForEachProject(x => x.WithSigningDisabled())
                .Save();
        }
    }
}
