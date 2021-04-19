using System.Collections.Generic;
using MigoLib;

namespace MigoToolGui.ViewModels
{
    public class EndpointsListModel
    {
        public IReadOnlyCollection<MigoEndpoint> Endpoints { get; set; }
    }
}