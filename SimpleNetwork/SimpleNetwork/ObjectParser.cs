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
        internal static T BytesToObject<T>(byte[] bytes)
        {
            if (GlobalDefaults.ObjectEncodingType == GlobalDefaults.EncodingType.MESSAGE_PACK)
                return MessagePackSerializer.Deserialize<T>(bytes, GlobalDefaults.SerializerOptions);
            else
                return JsonConvert.DeserializeObject<T>(BytesToJson(bytes));
                //return CompatibleJsonParser.Deserialize<T>(BytesToJson(bytes));//JsonConvert.DeserializeObject<T>(BytesToJson(bytes));
            //return MessagePackSerializer.Deserialize<T>(bytes, GlobalDefaults.Serializer);
        }

        internal static byte[] ObjectToBytes<T>(T obj)
        {
            if (GlobalDefaults.ObjectEncodingType == GlobalDefaults.EncodingType.MESSAGE_PACK)
                return MessagePackSerializer.Serialize(obj, GlobalDefaults.SerializerOptions);
            else
                return JsonToBytes(JsonConvert.SerializeObject(obj));
                //return JsonToBytes(CompatibleJsonParser.Serialize(obj));//JsonToBytes(JsonConvert.SerializeObject(obj));
            //return MessagePackSerializer.Serialize<T>(obj, GlobalDefaults.Serializer);

        }

        internal static object BytesToObject(byte[] bytes, Type type)
        {
            if (GlobalDefaults.ObjectEncodingType == GlobalDefaults.EncodingType.MESSAGE_PACK)
                return MessagePackSerializer.Deserialize(type, bytes, GlobalDefaults.SerializerOptions);
            else
                return JsonConvert.DeserializeObject(BytesToJson(bytes), type);
        }

        internal static byte[] ObjectToBytes(object obj, Type type)
        {
            if (GlobalDefaults.ObjectEncodingType == GlobalDefaults.EncodingType.MESSAGE_PACK)
                return MessagePackSerializer.Serialize(type, obj, GlobalDefaults.SerializerOptions);
            else
                return JsonToBytes(JsonConvert.SerializeObject(obj));
        }

        internal static string Serialize<T>(T obj)
        {
            return BytesToJson(ObjectToBytes(obj));
        }

        internal static T Deserialize<T>(string json)
        {
            return BytesToObject<T>(JsonToBytes(json));
        }

        internal static string BytesToJson(byte[] bytes)
        {
            if (GlobalDefaults.ObjectEncodingType == GlobalDefaults.EncodingType.MESSAGE_PACK)
                return MessagePackSerializer.ConvertToJson(bytes, GlobalDefaults.SerializerOptions);
            else
                return Encoding.UTF8.GetString(bytes);
            //return MessagePackSerializer.ConvertToJson(bytes, GlobalDefaults.Serializer);
        }

        internal static byte[] JsonToBytes(string json)
        {
            if (GlobalDefaults.ObjectEncodingType == GlobalDefaults.EncodingType.MESSAGE_PACK)
                return MessagePackSerializer.ConvertFromJson(json, GlobalDefaults.SerializerOptions);
            else
                return Encoding.UTF8.GetBytes(json);
            //return MessagePackSerializer.ConvertFromJson(json, GlobalDefaults.Serializer);
        }

        internal static bool IsType<T>(byte[] bytes)
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
                    MessagePackSerializer.Deserialize<T>(bytes, GlobalDefaults.SerializerOptions);
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
