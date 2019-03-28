using System.Collections.Generic;
using System.Net;

namespace SimpleProxy
{
    public  class IPEndPointMapping
    {
        public MappingType MappingType { get; set; }

        public KeyValuePair<IPEndPoint, IPEndPoint> Mapping { get; set; }
    }
}
