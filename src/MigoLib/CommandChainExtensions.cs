using MigoLib.GCode;
using MigoLib.Print;
using MigoLib.PrinterInfo;
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

        public static CommandChain GetPrinterInfo(this CommandChain self)
        {
            var command = new GetPrinterInfoCommand();
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
        
        public static CommandChain StartPrint(this CommandChain self, string file)
        {
            var command = new StartPrintCommand(file);
            self.Append(command);
            return self;
        }
        
        public static CommandChain StopPrint(this CommandChain self)
        {
            var command = new StopPrintCommand();
            self.Append(command);
            return self;
        }
    }
}