using System.IO;
using System.Threading.Tasks;

namespace MigoLib
{
    public abstract class Command
    {
        public abstract Task Write(BinaryWriter writer);
    }
}