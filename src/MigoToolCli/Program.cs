﻿using System.CommandLine;
using System.Threading.Tasks;
using MigoToolCli.Commands;

namespace MigoToolCli
{
    class Program
    {
        private const string AppDescription = "Unofficial Migo CLI tool";
        
        static async Task<int> Main(string[] args)
        {
            var root = new RootCommand(AppDescription);
            root.AddOption(new Option<MigoEndpoint>("--endpoint"));

            root.AddCommand(new SetCommands());

            return await root.InvokeAsync(args).ConfigureAwait(false);
        }
    }
}