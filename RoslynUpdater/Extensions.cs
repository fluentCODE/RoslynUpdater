using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynUpdater
{
    public static class Extensions
    {
        public static string MakeRelative(this string filePath, string referencePath)
        {
            if (Directory.Exists(referencePath) && !referencePath.EndsWith("/"))
                referencePath += "/";

            var fileUri = new Uri(filePath);
            var referenceUri = new Uri(referencePath);
            return referenceUri.MakeRelativeUri(fileUri).ToString().Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
