using MessagePack;
using MessagePack.Resolvers;
using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Text;

namespace SimpleNetwork
{
    internal static class ObjectParser
    {
        public static T BytesToObject<T>(byte[] bytes)
        {
            if (GlobalDefaults.ObjectEncodingType == GlobalDefaults.EncodingType.MESSAGE_PACK)
                return MessagePackSerializer.Deserialize<T>(bytes, ContractlessStandardResolver.Options);
            else
                return JsonConvert.DeserializeObject<T>(BytesToJson(bytes));
        }

        public static byte[] ObjectToBytes<T>(T obj)
        {
            if (GlobalDefaults.ObjectEncodingType == GlobalDefaults.EncodingType.MESSAGE_PACK)
                return MessagePackSerializer.Serialize<T>(obj, ContractlessStandardResolver.Options);
            else
                return JsonToBytes(JsonConvert.SerializeObject(obj));

        }

        public static string BytesToJson(byte[] bytes)
        {
            if (GlobalDefaults.ObjectEncodingType == GlobalDefaults.EncodingType.MESSAGE_PACK)
                return MessagePackSerializer.ConvertToJson(bytes, ContractlessStandardResolver.Options);
            else
                return Encoding.UTF8.GetString(bytes);
        }

        public static byte[] JsonToBytes(string json)
        {
            if (GlobalDefaults.ObjectEncodingType == GlobalDefaults.EncodingType.MESSAGE_PACK)
                return MessagePackSerializer.ConvertFromJson(json, ContractlessStandardResolver.Options);
            else
                return Encoding.UTF8.GetBytes(json);
        }

        public static bool IsType<T>(byte[] bytes)
        {
            if (GlobalDefaults.ObjectEncodingType == GlobalDefaults.EncodingType.MESSAGE_PACK)
            {
                try
                {
                    string json = MessagePackSerializer.ConvertToJson(bytes);
                    if (!typeof(T).IsPrimitive)
                    {
                        foreach (FieldInfo f in typeof(T).GetFields())
                        {
                            if (!f.IsInitOnly && !json.Contains(f.Name)) return false;
                        }
                    }
                    MessagePackSerializer.Deserialize<T>(bytes, ContractlessStandardResolver.Options);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                try
                {
                    string json = Encoding.UTF8.GetString(bytes);
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
}
