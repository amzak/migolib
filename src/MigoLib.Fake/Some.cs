using System;
using System.IO;
using MigoLib.State;

namespace MigoLib.Fake
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
                HeadY = 0,
                Success = true
            };

        public static MigoStateModel State            
            => new()
            {
                BedTemp = Random.Next(100),
                NozzleTemp = Random.Next(240),
                HeadX = 0,
                HeadY = 0
            };
        
        public static string GCodeFile = "Resources/3DBenchy.gcode";
        public static string GCodeFileName = Path.GetFileName(GCodeFile);
    }
}