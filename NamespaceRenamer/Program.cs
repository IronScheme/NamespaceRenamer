using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mono.Cecil;
using System.Linq;
using System.IO;

namespace NamespaceRenamer
{
    class Program
    {
        class Transform
        {
            public string Source, Target;
        }

        static Regex tx = new Regex("^(?<src>.+)=(?<tgt>.+)$", RegexOptions.Compiled);

        static void Main(string[] args)
        {
            bool refs = false;
            if (args.Contains("-r"))
            {
                refs = true;
                args = Array.FindAll(args, x => x != "-r");
            }


            var assname = args[0];
            string snk = Directory.GetFiles(Path.GetDirectoryName(Path.GetFullPath(assname)), "*.snk").FirstOrDefault();
            var renames = new List<Transform>();
            var except = new List<string>();

            for (int i = 1; i < args.Length; i++)
            {
                var m = tx.Match(args[i]);
                if (m.Success)
                {
                    renames.Add(new Transform
                    {
                        Source = m.Groups["src"].Value,
                        Target = m.Groups["tgt"].Value,
                    });
                }
                else
                {
                    except.Add(args[i]);
                }
            }

            var ass = AssemblyDefinition.ReadAssembly(assname, new ReaderParameters { ReadSymbols = true, });


            if (!refs)
            {
                Console.WriteLine("Renaming namespaces in {0}", ass.FullName);

                foreach (var t in ass.MainModule.Types)
                {
                    if (!except.Contains(t.Namespace))
                    {
                        foreach (var r in renames)
                        {
                            if (t.Namespace.StartsWith(r.Source))
                            {
                                var oldtn = t.FullName;
                                t.Namespace = t.Namespace.Replace(r.Source, r.Target);
                                Console.WriteLine("{0} -> {1}", oldtn, t.FullName);
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Renaming imported namespaces in {0}", ass.FullName);

                foreach (var t in ass.MainModule.GetTypeReferences())
                {
                    if (!except.Contains(t.Namespace))
                    {
                        foreach (var r in renames)
                        {
                            if (t.Namespace.StartsWith(r.Source))
                            {
                                var oldtn = t.FullName;
                                t.Namespace = t.Namespace.Replace(r.Source, r.Target);
                                Console.WriteLine("{0} -> {1}", oldtn, t.FullName);
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Saving assembly: {0}", assname);

            if (!refs && snk != null)
            {
                using (var sn = File.OpenRead(snk))
                {
                    ass.Write(assname, new WriterParameters { StrongNameKeyPair = new System.Reflection.StrongNameKeyPair(sn), WriteSymbols = true });
                }
            }
            else
            {
                ass.Write(assname, new WriterParameters { WriteSymbols = true });
            }
        }
    }
}
