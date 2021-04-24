using System.Collections.Generic;
using MigoLib;

namespace MigoToolGui
{
    public struct Config
    {
        public MigoEndpoint SelectedEndpoint { get; set; }

        public IReadOnlyCollection<MigoEndpoint> Endpoints { get; set; }

        public Config(MigoEndpoint selectedEndpoint, IReadOnlyCollection<MigoEndpoint> endpoints)
        {
            SelectedEndpoint = selectedEndpoint;
            Endpoints = endpoints;
        }

        public static Config Default
        {
            get
            {
                MigoEndpoint defaultEndpoint = new("127.0.0.1:10086");
                return new(defaultEndpoint, new[] {defaultEndpoint});
            }
        }
    }
}