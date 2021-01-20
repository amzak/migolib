using System.Collections.Generic;
using System.CommandLine;

namespace MigoToolCli.Commands
{
    public class GetCommands : Command
    {
        private new const string Name = "get";
        private new const string Description = "Gets parameter";

        public GetCommands() : base(Name, Description)
        {
            foreach (var command in Commands())
            {
                AddCommand(command);
            }
        }
        
        private static IEnumerable<Command> Commands()
        {
            yield return new GetZOffsetCommand();
        }
    }
}