using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brbypass_client.Model.Net
{
    public class JsonResponse
    {
        public string type;
        public string status;
        public string content;

        public JsonResponse()
        {
            type = "";
            status = "";
            content = "";
        }
        public JsonResponse(string type, string status)
        {
            this.type = type;
            this.status = status;
            content = "";
        }
        public JsonResponse(string type,string status,string content)
        {
            this.type = type;
            this.status = status;
            this.content = content;
        }
    }
}
