using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium.Server.Node
{
    public interface INodeProvider
    {
        IEnumerable<Node> Nodes { get; }

        Node FindNodeById(Guid id);
    }
}
