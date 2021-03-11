using MigoLib.State;

namespace MigoLib.Tests
{
    internal static class FakeReplies
    {
        public static string State
        {
            get
            {
                var migoStateModel = Some.FixedStateModel;
                var reply = $"@#state;{migoStateModel.HeadX.ToString("F2")};" +
                            $"{migoStateModel.HeadX.ToString("F2")};" +
                            $"{migoStateModel.BedTemp.ToString()};" +
                            $"{migoStateModel.NozzleTemp.ToString()};0;10;1;0;0;0#@";
                return reply;
            }
        }

        public static string Status => "@#xt;type:3;id:100196#@";
    }
}