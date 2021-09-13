using System;
using System.IO;
using System.Text;
using System.Text.Encodings;
using System.Web;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Cortex.ASE.Requests {
    class RequestManager {
        public static void Respond(RequestClient client, string content, HttpStatusCode statusCode = HttpStatusCode.OK, string contentType = "text/html") {
            byte[] data = Encoding.UTF8.GetBytes(GetClientPage(client, "index.html", new Dictionary<string, string>() {
                { "content", content }
            }));

            client.Context.Response.StatusCode = (int)statusCode;
            client.Context.Response.ContentType = contentType;
            client.Context.Response.ContentEncoding = Encoding.UTF8;
            client.Context.Response.ContentLength64 = data.LongLength;

            client.Context.Response.OutputStream.Write(data, 0, data.Length);
        }

        public static string GetClientPage(RequestClient client, string page, ICollection<KeyValuePair<string, string>> replacements = null) {
            string path = Path.Combine(new string[] { (string)Program.Config["ase"]["directories"]["www"], "pages", page });

            string document = File.ReadAllText(path);

            if(replacements != null)
                foreach(KeyValuePair<string, string> replacement in replacements)
                    document = document.Replace("${" + replacement.Key + "}", replacement.Value);

            document = Regex.Replace(document, @"\$\{.*?\}", "");

            foreach(Match match in Regex.Matches(document, @"(\%{)(.*?)(\})"))
                document = document.Replace(match.Value, GetClientPage(client, match.Groups[2].Value));
        
            return document;
        }
    }
}
