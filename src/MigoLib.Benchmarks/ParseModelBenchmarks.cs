using System.Buffers;
using BenchmarkDotNet.Attributes;
using MigoLib.State;

namespace MigoLib.Benchmarks
{
    public class ParseModelBenchmarks
    {
        private MigoState _migoState;
        private PositionalSerializer<MigoState> _positionalSerializer;
        private ReadOnlySpanAction<char, MigoState> _intParser;
        private ReadOnlySpanAction<char, MigoState> _doubleParser;

        [GlobalSetup]
        public void Init()
        {
            _migoState = new MigoState();
            _positionalSerializer = new PositionalSerializer<MigoState>(';')
                .Field(x => x.BedTemp)
                .Field(x => x.HeadX);
            
            _intParser = _positionalSerializer.GetParser(0);
            _doubleParser = _positionalSerializer.GetParser(1);
        }
        
        [Benchmark(Baseline = true)]
        public void Baseline()
        {
            var paramStr = "90";
            int.TryParse(paramStr, out var intParam);
            _migoState.BedTemp = intParam;
            
            paramStr = "0.0";
            double.TryParse(paramStr, out var doubleParam);
            _migoState.HeadX = doubleParam;
        }

        [Benchmark]
        public void ParseModel()
        {
            var param = "90";
            _intParser(param, _migoState);
            param = "0.0";
            _doubleParser(param, _migoState);
        }
    }
}