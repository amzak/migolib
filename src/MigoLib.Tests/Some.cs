using System;
using MigoLib.State;

namespace MigoLib.Tests
{
    public static class Some
    {
        private static readonly Random Random = new(); 
        
        public static MigoStateModel FixedStateModel 
            => new()
            {
                BedTemp = 100,
                NozzleTemp = 240,
                HeadX = 0,
                HeadY = 0
            };

        public static MigoStateModel State            
            => new()
            {
                BedTemp = Random.Next(100),
                NozzleTemp = Random.Next(240),
                HeadX = 0,
                HeadY = 0
            };
    }
}