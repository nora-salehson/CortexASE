using System;
using System.Web;
using System.Text;
using System.Text.Encodings;
using System.Net;
using System.Net.Http;

namespace Cortex.ASE.Requests {
    [RequestPageMap("login")]
    class Index : IRequestPage {
        public void Respond(RequestClient client) {
            RequestManager.Respond(client, RequestManager.GetClientPage(client, "login.html"));
        }
        
        public void Respond(RequestClient client, string test) {
            byte[] data = Encoding.UTF8.GetBytes("hello world " + test);

            client.Context.Response.StatusCode = (int)HttpStatusCode.OK;
            client.Context.Response.ContentType = "text/html";
            client.Context.Response.ContentEncoding = Encoding.UTF8;
            client.Context.Response.ContentLength64 = data.LongLength;

            client.Context.Response.OutputStream.Write(data, 0, data.Length);
        }
    }
}
