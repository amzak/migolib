using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MigoLib.CurrentPosition
{
    public class SetCurrentPositionCommand : Command
    {
        private readonly string _command;

        public SetCurrentPositionCommand(Position pos)
        {
            var builder = new StringBuilder();
            builder.Append("setcurposition:");
            builder.Append(pos.X.ToString("#.##"));
            builder.Append(';');
            builder.Append(pos.Y.ToString("#.##"));
            builder.Append(';');
            builder.Append(pos.Z.ToString("#.##"));
            builder.Append(';');
            _command = builder.ToString();
        }
        
        public override Task Write(BinaryWriter writer)
        {
            writer.Write(_command);
            return Task.CompletedTask;
        }
    }
}