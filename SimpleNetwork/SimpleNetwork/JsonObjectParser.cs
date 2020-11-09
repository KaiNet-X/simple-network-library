using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleNetwork
{
    internal static class JsonObjectParser
    {
        internal static Encoding encodingType = Encoding.ASCII;

        public static string ObjectToJson<T>(T obj) => JsonConvert.SerializeObject(obj);

        public static byte[] JsonToBytes(string json) => encodingType.GetBytes(json);

        public static byte[] ObjectToBytes<T>(T obj) => JsonToBytes(ObjectToJson(obj));

        public static string BytesToJson(byte[] bytes) => encodingType.GetString(bytes);

        public static T JsonToObject<T>(string json) => JsonConvert.DeserializeObject<T>(json);

        public static T BytesToObject<T>(byte[] bytes) => JsonToObject<T>(BytesToJson(bytes));

        public static bool IsType<T>(byte[] bytes)
        {
            try
            {
                string json = Encoding.ASCII.GetString(bytes);
                if (!typeof(T).IsPrimitive)
                {
                    foreach (FieldInfo f in typeof(T).GetFields())
                    {
                        if (!f.IsInitOnly && !json.Contains(f.Name)) return false;
                    }
                }
                JsonConvert.DeserializeObject<T>(json);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (ArgumentNullException)
            {
                return false;
            }
        }
    }
}
