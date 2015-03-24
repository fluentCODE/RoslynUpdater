using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RoslynUpdater
{
    public class ProjectProcessor
    {
        public ProjectProcessor(string projectPath)
        {
            FilePath = projectPath;
            Document = XDocument.Load(projectPath);
            RootNamespace = Document.Root.GetDefaultNamespace();
        }

        public ProjectProcessor WithSigningDisabled()
        {
            var propertyGroups = Document.Root.Elements().Where(x => x.Name.LocalName == "PropertyGroup");
            var signAssemblyGroup = propertyGroups.Where(x => x.Elements().Any(y => y.Name.LocalName == "SignAssembly"));
            if (signAssemblyGroup.Count() == 0)
            {
                Document.Root.Add(new XElement(RootNamespace + "PropertyGroup", new XElement(RootNamespace + "SignAssembly", "false")));
            }
            else
            {
                var signAssemblyValue = signAssemblyGroup.Elements().First(x => x.Name.LocalName == "SignAssembly");
                signAssemblyValue.SetValue("false");
            }

            var delaySignGroup = propertyGroups.Where(x => x.Elements().Any(y => y.Name.LocalName == "DelaySign"));
            if (delaySignGroup.Count() == 0)
            {
                Document.Root.Add(new XElement(RootNamespace + "PropertyGroup", new XElement(RootNamespace + "DelaySign", "false")));
            }
            else
            {
                var delaySignValue = delaySignGroup.Elements().First(x => x.Name.LocalName == "DelaySign");
                delaySignValue.SetValue("false");
            }

            var internalsVisibleTo = Document.Root.Elements().Where(x => x.Name.LocalName == "ItemGroup" && x.Elements().Any(y => y.Name.LocalName == "InternalsVisibleTo"));
            if (internalsVisibleTo.Count() > 0)
            {
                var internalsVisibleElements = internalsVisibleTo.Elements().Where(x => x.Name.LocalName == "InternalsVisibleTo");
                foreach (var element in internalsVisibleElements)
                {
                    WithInternalsVisibleTo(element.Attribute("Include").Value);
                }

                internalsVisibleElements.Remove();
            }

            return this;
        }

        public ProjectProcessor WithInternalsVisibleTo(string assemblyName)
        {
            if (!internalsVisibleToAssembliesHash.Contains(assemblyName))
            {
                InternalsVisibleToAssemblies.Add(assemblyName);
                internalsVisibleToAssembliesHash.Add(assemblyName);
            }

            return this;
        }

        public ProjectProcessor WithLinkedFile(string filePath)
        {
            Document.Root.Add(new XElement(RootNamespace + "ItemGroup",
                new XElement(RootNamespace + "Compile",
                    new XAttribute("Include", filePath),
                    new XElement(RootNamespace + "Link", Path.GetFileName(filePath))
                )
            ));

            return this;
        }

        public void Save()
        {
            if (InternalsVisibleToAssemblies.Count > 0)
            {
                var projectPath = Path.GetDirectoryName(FilePath);
				var extension = Path.GetExtension(FilePath);

				string visibleToPath = null;
				if (extension == ".csproj")
				{
					visibleToPath = Path.Combine(Path.GetDirectoryName(FilePath), "InjectedProjectInternalsVisibleTo.cs");
					var internalsLines = InternalsVisibleToAssemblies.Select(x => $"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"{x}\")]");
					File.WriteAllLines(visibleToPath, internalsLines.ToArray());
				}
				else if (extension == ".vbproj")
				{
					visibleToPath = Path.Combine(Path.GetDirectoryName(FilePath), "InjectedProjectInternalsVisibleTo.vb");
					var internalsLines = InternalsVisibleToAssemblies.Select(x => $"<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"{x}\")>");
					File.WriteAllLines(visibleToPath, internalsLines.ToArray());
				}

				if (!string.IsNullOrEmpty(visibleToPath))
					WithLinkedFile(visibleToPath.MakeRelative(projectPath));
            }

            Document.Save(FilePath);
        }

        public string FilePath { get; }

        public XNamespace RootNamespace { get; }
        
        public XDocument Document { get; }

        public List<string> InternalsVisibleToAssemblies { get; } = new List<string>();
        HashSet<string> internalsVisibleToAssembliesHash = new HashSet<string>();
    }
}
