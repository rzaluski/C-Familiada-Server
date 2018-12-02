using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace FamiliadaClientForms
{
    class JMessage
    {
        public string MessageType { get; set; }
        public string ObjectJson { get; set; }

        public static string CreateMessage(string messageType, Object obj)
        {
            JMessage jMessage = new JMessage() { MessageType = messageType, ObjectJson = Newtonsoft.Json.JsonConvert.SerializeObject(obj) };
            return Newtonsoft.Json.JsonConvert.SerializeObject(jMessage);
        }

        public static JMessage Deserialize(string data)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<JMessage>(data);
        }

        public static T Deserialize<T>(string data)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(data);
        }
    }
}
