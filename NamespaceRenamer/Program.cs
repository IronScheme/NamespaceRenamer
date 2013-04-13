using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mono.Cecil;

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
      var assname = args[0];
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

      var ass = AssemblyDefinition.ReadAssembly(assname);

      foreach (var t in ass.MainModule.Types)
      {
        if (!except.Contains(t.Namespace))
        {
          foreach (var r in renames)
          {
            if (t.Namespace.StartsWith(r.Source))
            {
              t.Namespace = t.Namespace.Replace(r.Source, r.Target);
            }
          }
        }
      }

			foreach (var t in ass.MainModule.GetTypeReferences())
			{
				if (!except.Contains(t.Namespace))
				{
					foreach (var r in renames)
					{
						if (t.Namespace.StartsWith(r.Source))
						{
							t.Namespace = t.Namespace.Replace(r.Source, r.Target);
						}
					}
				}
			}

      ass.Write(assname);
    }
  }
}
