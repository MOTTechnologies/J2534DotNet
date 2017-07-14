using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Plugins;

namespace Plugins
{
    /// <summary>
    /// This loads all Plugin.FileFormat.*.dll files that implement Plugins.FileFormat
    /// </summary>
    public class FileFormatPluginLoader
    {
        public List<Plugins.FileFormat> Plugins;

        public FileFormatPluginLoader(string path)
        {
            string[] dllFileNames = null;
            Plugins = new List<Plugins.FileFormat>();

            if (Directory.Exists(path))
            {
                dllFileNames = Directory.GetFiles(path, "Plugin.FileFormat*.dll");

                ICollection<Assembly> assemblies = new List<Assembly>(dllFileNames.Length);
                foreach (string dllFile in dllFileNames)
                {
                    AssemblyName an = AssemblyName.GetAssemblyName(dllFile);
                    Assembly assembly = Assembly.Load(an);
                    assemblies.Add(assembly);
                }

                Type pluginType = typeof(Plugins.FileFormat);
                ICollection<Type> pluginTypes = new List<Type>();
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly != null)
                    {
                        Type[] types = assembly.GetTypes();
                        foreach (Type type in types)
                        {
                            if (type.IsInterface || type.IsAbstract) continue;
                            else if (type.GetInterface(pluginType.FullName) != null) pluginTypes.Add(type);
                        }
                    }
                }

                foreach (Type type in pluginTypes)
                {
                    Plugins.FileFormat plugin = (Plugins.FileFormat)Activator.CreateInstance(type);
                    Plugins.Add(plugin);
                }
                Plugins.Add(new Plugins.BIN());
            }
        }

        public string GetAllFormatsFilter()
        {
            string filter = "All Valid Files|";
            for (int i = 0; i < Plugins.Count; i++)
            {
                filter += "*." + Plugins[i].FileExtension;
                if (i < Plugins.Count - 1) filter += ";";
            }
            filter += "|All Files|*.*";

            return filter;
        }

        public string GetIndividualFormatsFilter()
        {
            string filter = "";
            for (int i = 0; i < Plugins.Count; i++)
            {
                filter += Plugins[i].FileDescription + "|*." + Plugins[i].FileExtension;
                if (i < Plugins.Count - 1) filter += "|";
            }

            return filter;
        }

        public bool TryGetFileFormat(string extension, out Plugins.FileFormat FileFormatter)
        {
            foreach (var plugin in Plugins)
            {
                if (plugin.FileExtension.Equals(extension))
                {
                    FileFormatter = plugin;
                    return true;
                }
            }
            FileFormatter = null;
            return false;
        }
    }
}
