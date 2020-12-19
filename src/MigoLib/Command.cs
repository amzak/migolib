using System.IO;

namespace MigoLib
{
    public abstract class Command
    {
        public abstract void Write(BinaryWriter writer);
    }
}