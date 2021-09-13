using System;
using System.Web;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Cortex.ASE.Requests {
    class RequestClient {
        public readonly HttpListenerContext Context;

        public readonly string Request;

        public readonly Dictionary<string, string> Parameters = new Dictionary<string, string>();

        public readonly string Key;

        public readonly bool Guest = true;

        public readonly int User;

        public RequestClient(HttpListenerContext context) {
            Context = context;

            
            Request = context.Request.RawUrl;

            if(Request.LastIndexOf('?') != -1) {
                Request = Request.Substring(0, Request.LastIndexOf('?'));

                string[] parameters = Request.Substring(Request.LastIndexOf('?') + 1, Request.Length).Split('&');

                foreach(string parameter in parameters) {
                    int index = parameter.IndexOf('=');

                    if(index == -1)
                        Parameters.Add(parameter, "");
                    else
                        Parameters.Add(parameter.Substring(0, index), parameter.Substring(index + 1, parameter.Length));
                }
            }


            Cookie key = Context.Request.Cookies.ToList().Find(x => x.Name == "key");

            if(key != null) {
                Key = key.Value;

                using MySqlConnection connection = new MySqlConnection(Program.Database);
                connection.Open();

                using MySqlCommand command = new MySqlCommand("SELECT * FROM user_keys WHERE `key` = @key", connection);
                command.Parameters.AddWithValue("key", Key);

                using MySqlDataReader reader = command.ExecuteReader();

                if(reader.Read()) {
                    Guest = false;

                    User = reader.GetInt32("user");
                }
                else
                    Key = "";
            }
        }
    }
}
