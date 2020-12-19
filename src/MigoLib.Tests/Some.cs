using MigoLib.State;

namespace MigoLib.Tests
{
    public static class Some
    {
        public static MigoStateModel MigoStateModel 
            => new MigoStateModel
            {
                BedTemp = 100,
                NozzleTemp = 240,
                HeadX = 0,
                HeadY = 0
            }; 
    }
}