using System.Collections.Generic;
using System.CommandLine;

namespace MigoToolCli.Commands
{
    public class SetCommands : Command
    {
        private new const string Name = "set";
        private new const string Description = "Sets parameter";
        
        public SetCommands() : base(Name, Description)
        {
            foreach (var command in Commands())
            {
                AddCommand(command);
            }
        }

        private static IEnumerable<Command> Commands()
        {
            yield return new SetZOffsetCommand();
            yield return new SetTemperatureCommand();
        }
    }
}