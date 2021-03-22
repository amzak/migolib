using System;

namespace MigoToolGui.ViewModels
{
    public struct TemperaturePoint
    {
        public TimeSpan Time { get; }
        public double Value { get; }

        public TemperaturePoint(TimeSpan time, double value)
        {
            Time = time;
            Value = value;
        }
    }
}