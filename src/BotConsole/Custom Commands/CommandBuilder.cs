using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using Microsoft.CodeAnalysis.Emit;
using System.Runtime.Loader;
using System.Text;
using Discord;
using Discord.Commands;
using System.Text.RegularExpressions;

namespace BotConsole.Custom_Commands
{
    public class CustomCommandBuilder
    {
        private bool _compiled = false;
        //private Assembly _assembly;
        private StringBuilder codeBuilder;
        public List<PortableExecutableReference> References;
        public List<string> Usings;
        public CustomCommandBuilder()
        {
            Init();

        }

        /// <summary>
        /// Creates Commands from its given templates
        /// </summary>
        /// <param name="templates">templates to create commands from</param>
        public CustomCommandBuilder(List<CommandTemplate> templates)
        {
            if (templates == null || !templates.Any()) throw new ArgumentException("templates null or empty!");
            Init();
            codeBuilder = codeBuilder.WithCommands(templates);
        }


        /// <summary>
        /// Add commands to the code
        /// </summary>
        /// <param name="templates"></param>
        public void AddCommands(List<CommandTemplate> templates)
        {
            if (_compiled) codeBuilder = codeBuilder.WithCommands(templates, codeBuilder.Length - 2);
            else codeBuilder = codeBuilder.WithCommands(templates).Close();
            
        }

        /// <summary>
        /// Remove commands from the code. 
        /// </summary>
        /// <param name="templates"></param>
        public void RemoveCommands(params string[] names)
        {
            foreach (var name in names)
            {
                codeBuilder.RemoveCommand(name);
            }
        }
        /// <summary>
        /// Compiles the current code into an assmebly
        /// </summary>
        /// <returns>the compiled code</returns>
        public Assembly Compile()
        {
            codeBuilder = codeBuilder.WithUsings(Usings); //may be spammy, but it should work
            var content = codeBuilder.ToString();
            _compiled = true;
            SyntaxTree tree = CSharpSyntaxTree.ParseText(content);

            var references = References.Where(x => x != null); //just to prevent crashes
            string assemblyName = Path.GetRandomFileName();

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { tree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using (MemoryStream ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);
                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
            diagnostic.IsWarningAsError ||
            diagnostic.Severity == DiagnosticSeverity.Error);
                    Console.WriteLine("Failed Creating commands; following errors occurred:\n");
                    foreach (Diagnostic diagnostic in failures)
                    {
                        Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                    throw new IOException("Could not create Assembly!");
                }
                else
                {
                    Console.WriteLine("success");
                    ms.Seek(0, SeekOrigin.Begin);
                    var assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                    return assembly;
                }
            }
        }

        [Obsolete]
        private List<PortableExecutableReference> getReferences()
        {
            var assemblies = new[]
               {
                typeof(object).GetTypeInfo().Assembly,
                typeof(IMessage).GetTypeInfo().Assembly,
                typeof(CommandAttribute).GetTypeInfo().Assembly
            };
            //normal assemblies
            var refs = from a in assemblies select MetadataReference.CreateFromFile(a.Location);
            var returnList = refs.ToList();
            //.net assemblies
            var dotnetAssemblies = new[]
            {
                "mscorlib.dll",
                "System.Runtime.dll",
                "System.Threading.Tasks.dll"
            };
            var assemblyPath = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);
            foreach (var aName in dotnetAssemblies)
            {
                try
                {
                    returnList.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, aName)));
                }
                catch
                {
                    Console.WriteLine($"Could not find {aName}");
                }

            }
            return returnList;
        }

        /// <summary>
        /// Add a reference to this instance of the codebuilder 
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="usingEntries"></param>
        public void AddReference(Assembly assembly = null, params string[] usingEntries)
        {
            PortableExecutableReference entry = null;
            if (assembly != null)
            {
                entry = MetadataReference.CreateFromFile(assembly.Location);
                References.Add(entry);
            }
            Usings.AddRange(usingEntries);
        }

        public void AddReferenceLocations(IEnumerable<string> assemblyLocations, IEnumerable<string> usingEntries)
        {
            References.AddRange(assemblyLocations.Select(x => MetadataReference.CreateFromFile(x)));
            Usings.AddRange(usingEntries);
        }

        private void Init()
        {
            codeBuilder = new StringBuilder(@"
namespace CustomCommands {
[Module]
public class GeneratedCommands {");

            var temp = new Dictionary<Assembly, string[]>() {
                {typeof(object).GetTypeInfo().Assembly,new [] { "System" } },
                {typeof(IMessage).GetTypeInfo().Assembly, new [] {  "Discord" } },
                {typeof(CommandAttribute).GetTypeInfo().Assembly, new [] { "Discord.Commands" } }
            };
            References = temp.Select(x => MetadataReference.CreateFromFile(x.Key.Location)).ToList();
            Usings = new List<string>()
            {
                "System",
                "Discord",
                "Discord.Commands",
                "System.Threading.Tasks"
            };
            var dotnetAssemblies = new List<string>()
            {
                { "mscorlib.dll"},
                { "System.Runtime.dll"},
                { "System.Threading.Tasks.dll"}
            };
            var locationPath = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);
            foreach (var a in dotnetAssemblies)
            {
                var p = Path.Combine(locationPath, a);
                References.Add(MetadataReference.CreateFromFile(p));

            }

        }

    }

    internal static class BuilderExtensions
    {
        /// <summary>
        /// Adds the given commandTemplates to the StringBuilder
        /// </summary>
        /// <param name="builder">builder to add commands to</param>
        /// <param name="templates">templates of commands to add</param>
        /// <param name="startIndex">Location to insert commands at in builder, default is end</param>
        /// <returns></returns>
        internal static StringBuilder WithCommands(this StringBuilder builder, List<CommandTemplate> templates, int startIndex = -1)
        {
            StringBuilder sb;
            if (startIndex == -1)
            {
                sb = builder;
            }
            else
            {
                sb = new StringBuilder();
            }
            foreach (var template in templates)
            {
                sb.AppendLine();
                sb.AppendLine($"[Command(\"{template.Name}\")]");
                sb.AppendLine($"[Description(\"{template.Description}\")]");
                sb.Append($"public static async Task {template.CommandName}(IMessage msg");
                if (template.Parameters != null && template.Parameters.Any())
                {
                    foreach (var param in template.Parameters)
                    {
                        sb.Append(", ");
                        if (!string.IsNullOrWhiteSpace(param.Item3))
                        {
                            sb.Append($"[{param.Item3}] ");
                        }
                        sb.Append($"{param.Item1} {param.Item2}");

                    }
                }
                sb.AppendLine(")");
                sb.AppendLine("{\n" + template.FunctionString + "\n}");
            }

            if (startIndex == -1)
                return sb;
            else
                return builder.Insert(startIndex, sb.ToString());
        }

        /// <summary>
        /// Appends `usings` to first lines of builder
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="references"></param>
        /// <returns></returns>
        internal static StringBuilder WithUsings(this StringBuilder builder,List<string> references)
        {
            foreach (var usin in references.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                builder.Insert(0, $"using {usin};\n");
            }
            return builder;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="brackets"></param>
        /// <returns></returns>
        internal static StringBuilder Close(this StringBuilder builder, int brackets = 2)
        {
            for (int i = 0; i < 2; i++)
            {
                builder.Append("}");
            }
            return builder;
        }

        internal static StringBuilder UnClose(this StringBuilder builder, int brackets = 2)
        {
           

            for (int i = 0; i < 2; i++)
            {
                var s = builder.ToString();
                builder.Remove(s.LastIndexOf('}'), 1);
            }
            return builder;
        }

        internal static StringBuilder RemoveCommand(this StringBuilder builder, string templateName)
        {
            var s = builder.ToString();
            Regex removerregex = new Regex(@"(?<cmd>\[Command\(" + $"\"{templateName}\"" + @"\)\](?s).*?)(\[Command|\}\s*\}\s*$)");
            var m = removerregex.Match(s);
            if (!m.Success) throw new ArgumentException("Could not find command");
            builder.Remove(m.Groups["cmd"].Index, m.Groups["cmd"].Length);
            return builder;
        }
    }
}
