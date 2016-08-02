using BotConsole.Custom_Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BotConsole
{
    public class Program
    {
        #region Fields
        internal static CommandService _commands;
        private static DiscordSocketClient _client;
        public static DiscordSocketClient Client { get { return _client; } set { _client = value; } }
        internal static Custom_Commands.CustomCommandsModule CCModule { get; set; }
        #endregion
        public static void Main(string[] args)
        {
            new Program().Start().GetAwaiter().GetResult();
        }

        public Program()
        {

        }
        /// <summary>
        /// The actual starting up
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                AudioMode = Discord.Audio.AudioMode.Disabled,
                LogLevel = LogSeverity.Info
            });
            _commands = new CommandService();
            CCModule = new Custom_Commands.CustomCommandsModule();
            CCModule.LoadFromPath("customcommands.json");
            var token = "MTQ1ODk0NzY4NjEyODY4MDk2.CnUfaQ.qTJwv1ZzDqEIFYPPxLPNTYkVw_s";

            await InstallCommands();
            foreach (var cmd in _commands.Commands)
            {
                Console.WriteLine(cmd.Name);
            }

            await _client.LoginAsync(TokenType.User, token);
            await _client.ConnectAsync();
            Console.WriteLine("Connected and receiving commands");
            //after connection, initialize global variables


            // Block this task until the bot is exited.
            await Task.Delay(-1);
        }
        private async Task InstallCommands()
        {
            // Hook the MessageReceived Event into our Command Handler
            _client.MessageReceived += HandleCommand;
            // Discover all of the commands in this assembly and load them.
            var normalAssembly = Assembly.GetEntryAssembly();
            await _commands.LoadAssembly(normalAssembly);
            await ReloadCustomCommands();
            ////SimulateLoad(assembly);

        }

        /// <summary>
        /// Unloads the existing customcommands if they exist and loads the new assembly
        /// </summary>
        /// <returns></returns>
        internal async static Task ReloadCustomCommands()
        {

            var module = _commands.Modules.FirstOrDefault(x => x.Name == "GeneratedCommands");

            try
            {
                var assembly = CCModule.Compile();
                if (module != null) await _commands.Unload(module);
                await _commands.LoadAssembly(assembly);
            }
            catch
            {
                Console.WriteLine("Falling back to previous module if there was one");
            }
        }



        public async Task HandleCommand(IMessage msg)
        {
            // Internal integer, marks where the command begins
            int argPos = 0;
            // Get the current user (used for Mention parsing)
            var currentUser = await _client.GetCurrentUserAsync();

            if (msg.Author.Id == 131474815298174976 && IsCommand(msg, ref argPos))
            {

                // Execute the command. (result does not indicate a return value, 
                // rather an object stating if the command executed succesfully)
                var result = await _commands.Execute(msg, argPos);
                if (!result.IsSuccess)
                    await msg.Channel.SendMessageAsync(result.ErrorReason);
            }

        }

        private bool IsCommand(IMessage msg, ref int argPos)
        {
            //loop because ref isn't allowed in anonymous method
            foreach (var prefix in new[] { "dragon." })
            {
                if (msg.HasStringPrefix(prefix, ref argPos)) return true;
            }
            return false;
        }
        internal static async Task<bool> RemoveCommand(string commandName)
        {
            if (_commands.Commands.Any(x => x.Text == commandName && x.Module.Name == "GeneratedCommands"))
            {
                try
                {
                    CCModule.RemoveCommands(commandName);
                    await ReloadCustomCommands();
                    return true;
                }
                catch
                {
                    Console.WriteLine("Could not delete command");
                }
            }
            return false;
        }
        internal static async Task<bool> CreateCommand(CommandTemplate template)
        {
            try
            {
                CCModule.AddOrUpdateCommand(template);
                await ReloadCustomCommands();
                return true;

            }
            catch { }
            return false;
        }

    }
}
