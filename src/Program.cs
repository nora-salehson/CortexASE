using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using MimeMapping;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Cortex.ASE.Requests;

namespace Cortex.ASE {
    class Program {
        public static string Database;

        static void Main(string[] args) {
            string directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            JObject manifest = JObject.Parse(File.ReadAllText(Path.Combine(directory, "Cortex.ASE.json")));

            JObject config = JObject.Parse(File.ReadAllText((string)manifest["configuration"]));


            Database = $"server={config["mysql"]["host"]};uid={config["mysql"]["credentials"]["name"]};pwd={config["mysql"]["credentials"]["password"]};database={config["mysql"]["database"]};SslMode={config["mysql"]["sslmode"]}";


            using HttpListener listener = new HttpListener();

            foreach(JToken prefix in config["ase"]["prefixes"])
                listener.Prefixes.Add((string)prefix);

            // netsh http add urlacl url=http://ase.local.cortex5.io:80/ user=caket
            listener.Start();

            var instances = System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where(a => a.GetConstructor(Type.EmptyTypes) != null).Select(Activator.CreateInstance).OfType<IRequestPage>();



            while(true) {
                HttpListenerContext context = listener.GetContext();

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                Console.WriteLine($"Receiving connection from {request.RemoteEndPoint.Address} for {request.RawUrl}");

                ThreadPool.QueueUserWorkItem((e) => {
                    try {
                        RequestClient client = new RequestClient(context);

                        if(client.Request.LastIndexOf('.') != -1) {
                            string path = Path.Combine(new string[] { (string)config["ase"]["directories"]["www"], "public", client.Request.Trim('/') });

                            if(File.Exists(path)) {
                                response.StatusCode = (int)HttpStatusCode.OK;
                                response.ContentType = MimeMapping.MimeUtility.GetMimeMapping(path);
                                response.ContentEncoding = Encoding.UTF8;

                                using(FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                                    response.AddHeader("Content-Length", stream.Length.ToString());

                                    stream.CopyTo(response.OutputStream);
                                }
                            }
                            else {
                                response.StatusCode = (int)HttpStatusCode.NotFound;
                            }
                        }
                        else if(client.Request.Length == 1) {
                            response.StatusCode = (int)HttpStatusCode.Redirect;

                            response.Redirect((client.Guest)?("/index"):("/home"));
                        }
                        else {
                            string methodName = context.Request.Url.Segments[1].Replace("/", "");
                            string[] strParams = context.Request.Url
                                                    .Segments
                                                    .Skip(2)
                                                    .Select(s=>s.Replace("/",""))
                                                    .ToArray();

                            var instance = instances.FirstOrDefault(x => x.GetType().GetCustomAttributes(true).Any(attribute => attribute is RequestPageMap && (attribute as RequestPageMap).Map.StartsWith(methodName)));

                            var methods = instance.GetType().GetMethods().Where(x => x.Name == "Respond");

                            var method = methods.FirstOrDefault(x => x.GetParameters().Length == strParams.Length + 1);

                            var parameters = method.GetParameters();

                            List<object> @params = new List<object>();

                            @params.Add(client);

                            for(int index = 1; index < parameters.Length; index++) {
                                @params.Add(Convert.ChangeType(strParams[index - 1], parameters[index].ParameterType));
                            }

                            method.Invoke(instance, @params.ToArray());
                        }
                    }
                    catch(Exception exception) {
                        Console.WriteLine(exception.Message);
                        Console.WriteLine(exception.StackTrace);

                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    }
                    finally {
                        response.Close();
                    }
                });
            }
        }
    }
}
