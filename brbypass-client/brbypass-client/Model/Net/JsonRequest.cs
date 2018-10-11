using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace brbypass_client.Model.Net
{
    public class JsonRequest
    {
        public string type;
        public string content;
        
        public JsonRequest(string type, string content)
        {
            this.type = type;
            this.content = content;
        }

        public static string getString(string type, string content)
        {
            JsonRequest request = new JsonRequest(type, content);
            return JsonConvert.SerializeObject(request);
        }
    }
}
