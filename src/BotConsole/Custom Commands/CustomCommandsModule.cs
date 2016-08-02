using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace BotConsole.Custom_Commands
{
    public class CustomCommandsModule
    {
        private CustomCommandBuilder _builder { get; set; }
        public ConcurrentDictionary<string, CommandTemplate> CustomCommands { get; set; }
        List<string> CompiledCommands = new List<string>();

        public CustomCommandsModule()
        {
            _builder = new CustomCommandBuilder();
            CustomCommands = new ConcurrentDictionary<string, CommandTemplate>();
        }

        public Assembly Compile()
        {
            
            var temp = CustomCommands.Where(x => !CompiledCommands.Contains(x.Key)).Select(x => x.Value).ToList();
            
            _builder.AddCommands(temp);
            CompiledCommands.AddRange(CustomCommands.Keys);
            CompiledCommands = CompiledCommands.Distinct().ToList();
            return _builder.Compile();
        }
        public void SaveReferences(string path)
        {
            Tuple<IEnumerable<string>, List<string>> content = Tuple.Create(_builder.References.Select(x => x.FilePath), _builder.Usings);
            File.WriteAllText(path, JsonConvert.SerializeObject(content, Formatting.Indented));
        }

        public void LoadReferences(string path)
        {
            var content = File.ReadAllText(path);
            var temp = JsonConvert.DeserializeObject<Tuple<List<string>, List<string>>>(content);
            _builder.AddReferenceLocations(temp.Item1, temp.Item2);
        }

        /// <summary>
        /// Load Commands from file 
        /// Replaces current Dictionary!
        /// </summary>
        /// <param name="path"></param>
        public void LoadFromPath(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException();
            List<CommandTemplate> list = JsonConvert.DeserializeObject<List<CommandTemplate>>(File.ReadAllText(path));
            CustomCommands = new ConcurrentDictionary<string, CommandTemplate>(list.Select(x => new KeyValuePair<string, CommandTemplate>(x.Name, x)).AsEnumerable());
        }

        /// <summary>
        /// Load Commands from file, adding to existing Commands.
        /// </summary>
        /// <param name="path"></param>
        public void AddFromPath(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException();
            List<CommandTemplate> list = JsonConvert.DeserializeObject<List<CommandTemplate>>(File.ReadAllText(path));
            foreach (var template in list)
            {
                CustomCommands.TryAdd(template.Name, template);
            }
        }



        /// <summary>
        /// Add commands to the code
        /// </summary>
        /// <param name="templates"></param>
        public void AddOrUpdateCommand(params CommandTemplate[] templates)
        {
            foreach (var template in templates)
                CustomCommands.AddOrUpdate(template.Name, template, (x, y) =>
                {
                    if (CompiledCommands.Contains(template.Name))
                    {
                        _builder.RemoveCommands(template.Name);
                        CompiledCommands.Remove(template.Name);
                    }
                    return template;
                });

        }

        /// <summary>
        /// Remove the given command from the builder
        /// </summary>
        /// <param name="templates"></param>
        public void RemoveCommands(params string[] names)
        {
            foreach (var name in names)
            {
                try
                {
                    _builder.RemoveCommands(name);
                }
                catch (ArgumentException)
                {
                    Console.WriteLine($"Failed deleting command {name}");
                }
            }
        }

        /// <summary>
        /// Saves Dictionary to given path
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            var temp = CustomCommands.Select(x => x.Value);
            File.WriteAllText(path, JsonConvert.SerializeObject(temp, Formatting.Indented));
        }


        private CommandTemplate exampleCommand = new CommandTemplate()
        {
            CommandName = "helloCommand",
            Name = "hello",
            Aliases = new List<string>() { "welcome" },
            Description = "Saying hello",
            Parameters = new List<Tuple<string, string, string>>
                {
                    Tuple.Create("string", "message", "Remainder")
                },
            FunctionString = "await msg.Channel.SendMessageAsync($\"hello {message}\");"
        };
    }

}
