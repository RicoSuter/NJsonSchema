using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NJsonSchema.Demo
{
    public partial class DataService
    {
        public DataService() { }

        public DataService(string baseUrl)
        {
	        BaseUrl = baseUrl; 
        }

        partial void PrepareRequest(HttpClient request);

        partial void ProcessResponse(HttpClient request, HttpResponseMessage response);

        public string BaseUrl { get; set; }

        public async Task<long> FooAsync(long a, long b, object c )
        {
            var url = string.Format("{0}/{1}?", BaseUrl, "api/Sum/{0}/{1}");

            url = url.Replace("{0}", a.ToString());
            url = url.Replace("{1}", b.ToString());



            var client = new HttpClient();
            PrepareRequest(client);

            var content = new StringContent(JsonConvert.SerializeObject(c));

            var response = await client.PostAsync(url, content);
            ProcessResponse(client, response);
            var data = await response.Content.ReadAsStringAsync(); 
            var httpStatusCode = response.StatusCode;

            var isError = (int)httpStatusCode >= 400; 
			if (isError)
            {
                if (!string.IsNullOrEmpty(data))
                {
                    try
                    {
                        var exception = (JContainer)JsonConvert.DeserializeObject(data);
                        throw new JsdlException(
                            exception["code"] != null ? exception["code"].ToString() : null, 
                            exception["message"] != null ? exception["message"].ToString() : "", 
                            exception["stackTrace"] != null ? exception["stackTrace"].ToString() : null);
                    } catch { }
                }
                throw new JsdlException("http_" + (int)httpStatusCode, "HTTP error: " + httpStatusCode, null);
            }

            return JsonConvert.DeserializeObject<long>(data);		
        }


        public class JsdlException : Exception
        {
            public JsdlException(string code, string message, string serverStackTrace) : base(message)
            { 
                Code = code; 
                ServerStackTrace = serverStackTrace; 
            }

            public string Code { get; private set; }

            public string ServerStackTrace { get; private set; }
        }
    }

}