using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Uds
{
    class uds_seriver
    {
        public string sid = string.Empty;
        public string name = string.Empty;
        
        public class SubFunction
        {
            public string id = string.Empty;
            public string name = string.Empty;
            public string parameter = string.Empty;
        }
        public string parameter = string.Empty;

        public List<SubFunction> sub_function_list = new List<SubFunction>();
    }
}
