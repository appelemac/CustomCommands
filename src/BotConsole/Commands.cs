using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using System.Text.RegularExpressions;
using System.Text;
using BotConsole.Custom_Commands;

namespace BotConsole
{
    [Module]
    public class Commands
    {
        [Command("help")]
        [Description("HELP ME NOW!!!!")]
        public async Task helpCommand(IMessage msg, [Remainder] string text)
        {
            await msg.Channel.SendMessageAsync("hello to you too");
        }

        [Command("createCommand")]
        [Description("Create a command with your own custom things!")]
        public async Task createCommand(IMessage msg, string name, [Remainder] string returnMessage)
        {
            CommandTemplate template = new CommandTemplate()
            {
                Name = name,
                CommandName = $"{name}Command",
                Description = $"A generated command",
                FunctionString = @"await msg.Channel.SendMessageAsync($" + $"\"{returnMessage}\");"
            };
            await Program.CreateCommand(template);
            await msg.Channel.SendMessageAsync($"Succesfully created command {name}");
        }

        [Command("removeCommand")]
        [Description("Remove a given *custom* command")]
        public async Task DeleteCommand(IMessage msg, string commandName)
        {
                var succes = await Program.RemoveCommand(commandName);
                await msg.Channel.SendMessageAsync($"Command was " + (succes ? "successfully" : "unsuccesfully") + " deleted");
           
        }
        [Command("list")]
        public async Task ListCommand(IMessage msg)
        {
            var sb = new StringBuilder("Current commands are: ```xl\n");
            foreach (var cmd in Program._commands.Commands)
            {
                sb.AppendLine($"-{cmd.Text}:{cmd.Description}");
            }
            await msg.Channel.SendMessageAsync(sb.Append("```").ToString());
        }
    }

}

