using MigoLib.State;
using MigoLib.ZOffset;

namespace MigoLib
{
    public static class Parsers
    {
        public static IResultParser<ZOffsetModel> GetZOffset => new GetZOffsetParser();
        
        public static IResultParser<MigoStateModel> GetState => new GetStateParser();
    }
}