using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace XLua
{
    static class ex
    {
        public static string arraytostring(this string[] args)
        {
            var s = "------------------------\n";

            if (args == null || args.Length == 0)
            {
                return s;
            }

            for (var i = 0; i < args.Length; i++)
            {
                s += args[i] + "\n";
            }
            s += "------------------------\n";
            return s;
        }
    }

    public class XLuaHotfixInject
    {
        public static void Useage()
        {
            Console.WriteLine("XLuaHotfixInject assmbly_path xlua_assembly_path gencode_assembly_path id_map_file_path [cfg_assmbly2_path] [search_path1, search_path2 ...]");
        }

        static void Info(string info)
        {
#if XLUA_GENERAL
            System.Console.WriteLine(info);
#else
            UnityEngine.Debug.Log(info);
#endif
        }

        public static void Main(string[] args)
        {
            // Info($"XLuaHotfixInject Main args -> {args.arraytostring()}");

            if (args.Length < 4)
            {
                // Info($"XLuaHotfixInject Main args.Length < 4");
                Useage();
                return;
            }

            try
            {
                var injectAssmblyPath = Path.GetFullPath(args[0]);
                var xluaAssmblyPath = Path.GetFullPath(args[1]);
                var genAssemblyPath = Path.GetFullPath(args[2]);
                string cfg_append = null;
                if (args.Length > 4)
                {
                    cfg_append = Path.GetFullPath(args[4]);
                    if (!cfg_append.EndsWith(".data"))
                    {
                        cfg_append = null;
                    }
                }
                AppDomain currentDomain = AppDomain.CurrentDomain;
                List<string> search_paths = args.Skip(cfg_append == null ? 4 : 5).ToList();
                currentDomain.AssemblyResolve += new ResolveEventHandler((object sender, ResolveEventArgs rea) =>
                {
                    foreach (var search_path in search_paths)
                    {
                        string assemblyPath = Path.Combine(search_path, new AssemblyName(rea.Name).Name + ".dll");
                        if (File.Exists(assemblyPath))
                        {
                            return Assembly.Load(File.ReadAllBytes(assemblyPath));
                        }
                    }
                    return null;
                });
                var assembly = Assembly.Load(File.ReadAllBytes(injectAssmblyPath));
                var hotfixCfg = new Dictionary<string, int>();
                HotfixConfig.GetConfig(hotfixCfg, assembly.GetTypes());
                if (cfg_append != null)
                {
                    using (BinaryReader reader = new BinaryReader(File.Open(cfg_append, FileMode.Open)))
                    {
                        int count = reader.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            string k = reader.ReadString();
                            int v = reader.ReadInt32();
                            if (!hotfixCfg.ContainsKey(k))
                            {
                                hotfixCfg.Add(k, v);
                            }
                        }
                    }
                }
                // Info($"XLuaHotfixInject Main HotfixInject");
                Hotfix.HotfixInject(injectAssmblyPath, xluaAssmblyPath, genAssemblyPath, args.Skip(cfg_append == null ? 4 : 5), args[3], hotfixCfg);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in hotfix inject: " + e);
            }
        }
    }
}