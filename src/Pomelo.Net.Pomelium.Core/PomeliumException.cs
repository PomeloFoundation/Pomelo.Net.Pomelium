using System;

namespace Pomelo.Net.Pomelium
{
    public class PomeliumException : Exception
    {
        public PomeliumException(string error):base(error)
        {
        }
    }
}
