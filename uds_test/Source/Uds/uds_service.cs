using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Uds
{
    class uds_service
    {
        public string sid;
        public string name;
        public string parameter;
        public SubFunction sub_function_selectd;
        public Identifier identifier_selected;
        public List<Identifier> identifier_list;
        public List<SubFunction> sub_function_list;

        public uds_service()
        {
            sid = string.Empty;
            name = string.Empty;
            parameter = string.Empty;
            sub_function_selectd = new SubFunction();
            identifier_selected = new Identifier();
            identifier_list = new List<Identifier>();
            sub_function_list = new List<SubFunction>();
        }

        public class Identifier
        {
            public string id;
            public string name;
            public string parameter;
            public Identifier()
            {
                id = string.Empty;
                name = string.Empty;
                parameter = string.Empty;
            }
        }

        public class SubFunction
        {
            public string id;
            public string name;
            public string parameter;
            public SubFunction()
            {
                id = string.Empty;
                name = string.Empty;
                parameter = string.Empty;
            }
        }

        public override string ToString()
        {
            return sid
                + sub_function_selectd.id
                + sub_function_selectd.parameter
                + identifier_selected.id
                + identifier_selected.parameter
                + parameter;
        }
    }
}
