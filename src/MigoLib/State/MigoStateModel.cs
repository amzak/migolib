namespace MigoLib.State
{
    public class MigoStateModel : ParseResult
    {    
        public double HeadX { get; set; }
        public double HeadY { get; set; }
        public int NozzleTemp { get; set; }
        public int BedTemp { get; set; }
    }
}