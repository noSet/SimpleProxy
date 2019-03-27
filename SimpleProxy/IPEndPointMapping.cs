using System.Collections.Generic;
using System.Net;

namespace SimpleProxy
{
    public class IPEndPointMapping
    {
        public KeyValuePair<IPEndPoint, IPEndPoint> Mapping { get; set; }
    }
}
