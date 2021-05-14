namespace MigoLib.CurrentPosition
{
    public readonly struct Position
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;

        public Position(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}