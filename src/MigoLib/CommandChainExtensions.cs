using MigoLib.GCode;
using MigoLib.ZOffset;

namespace MigoLib
{
    public static class CommandChainExtensions
    {
        public static CommandChain GetZOffset(this CommandChain self)
        {
            var command = new GetZOffsetCommand();
            self.Append(command);
            return self;
        }
        
        public static CommandChain SetZOffset(this CommandChain self, double zOffset)
        {
            var command = new SetZOffsetCommand(zOffset);
            self.Append(command);
            return self;
        }
        
        public static CommandChain ExecuteGCode(this CommandChain self, string[] lines)
        {
            var command = new GCodeCommand(lines);
            self.Append(command);
            return self;
        }
    }
}