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
            public bool identifier_enabled;
            public SubFunction()
            {
                id = string.Empty;
                name = string.Empty;
                parameter = string.Empty;
                identifier_enabled = true;
            }
        }

        public override string ToString()
        {
            string strings = string.Empty;
            strings += sid;
            if (sid != "2F")
            {
                strings += sub_function_selectd.id;
            }
            if (sub_function_selectd.identifier_enabled)
            {
                strings += identifier_selected.id;
                if (sid == "2F")
                {
                    strings += sub_function_selectd.id;
                    if(sub_function_selectd.id == "03")
                    {
                        strings += identifier_selected.parameter;
                    }
                }
                else
                {
                    strings += identifier_selected.parameter;
                    strings += sub_function_selectd.parameter;
                }
            }
            else
            {
                strings += sub_function_selectd.parameter;
            }
            strings += parameter;
            return strings;
        }
    }
}
