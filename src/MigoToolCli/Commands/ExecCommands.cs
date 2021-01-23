using System.Collections.Generic;
using System.CommandLine;

namespace MigoToolCli.Commands
{
    public class ExecCommands : Command
    {
        private new const string Name = "exec";
        private new const string Description = "Executes something";
        
        public ExecCommands() 
            : base(Name, Description)
        {
            foreach (var command in Commands())
            {
                AddCommand(command);
            }
        }
        
        private static IEnumerable<Command> Commands()
        {
            yield return new ExecuteGCodeCommand();
            yield return new ExecuteGCodeFileCommand();
        }
    }
}