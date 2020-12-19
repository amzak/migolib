using System.Buffers;
using BenchmarkDotNet.Attributes;
using MigoLib.State;

namespace MigoLib.Benchmarks
{
    public class ParseModelBenchmarks
    {
        private MigoStateModel _migoStateModel;
        private PositionalSerializer<MigoStateModel> _positionalSerializer;
        private ReadOnlySpanAction<char, MigoStateModel> _intParser;
        private ReadOnlySpanAction<char, MigoStateModel> _doubleParser;

        [GlobalSetup]
        public void Init()
        {
            _migoStateModel = new MigoStateModel();
            _positionalSerializer = new PositionalSerializer<MigoStateModel>(';')
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
            _migoStateModel.BedTemp = intParam;
            
            paramStr = "0.0";
            double.TryParse(paramStr, out var doubleParam);
            _migoStateModel.HeadX = doubleParam;
        }

        [Benchmark]
        public void ParseModel()
        {
            var param = "90";
            _intParser(param, _migoStateModel);
            param = "0.0";
            _doubleParser(param, _migoStateModel);
        }
    }
}