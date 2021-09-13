using System;

namespace Cortex.ASE.Requests {
    class RequestPageMap : Attribute {
        public string Map;
        
        public RequestPageMap(string map) {
            Map = map;
        }
    }
}
