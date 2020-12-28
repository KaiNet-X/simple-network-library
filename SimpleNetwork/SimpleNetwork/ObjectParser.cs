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
                //return CompatibleJsonParser.Deserialize<T>(BytesToJson(bytes));//JsonConvert.DeserializeObject<T>(BytesToJson(bytes));
            //return MessagePackSerializer.Deserialize<T>(bytes, GlobalDefaults.Serializer);
        }

        public static byte[] ObjectToBytes<T>(T obj)
        {
            if (GlobalDefaults.ObjectEncodingType == GlobalDefaults.EncodingType.MESSAGE_PACK)
                return MessagePackSerializer.Serialize<T>(obj, ContractlessStandardResolver.Options);
            else
                return JsonToBytes(JsonConvert.SerializeObject(obj));
                //return JsonToBytes(CompatibleJsonParser.Serialize(obj));//JsonToBytes(JsonConvert.SerializeObject(obj));
            //return MessagePackSerializer.Serialize<T>(obj, GlobalDefaults.Serializer);

        }

        public static object BytesToObject(byte[] bytes, Type type)
        {
            if (GlobalDefaults.ObjectEncodingType == GlobalDefaults.EncodingType.MESSAGE_PACK)
                return MessagePackSerializer.Deserialize(type, bytes, ContractlessStandardResolver.Options);
            else
                return JsonConvert.DeserializeObject(BytesToJson(bytes), type);
        }

        public static byte[] ObjectToBytes(object obj, Type type)
        {
            if (GlobalDefaults.ObjectEncodingType == GlobalDefaults.EncodingType.MESSAGE_PACK)
                return MessagePackSerializer.Serialize(type, obj, ContractlessStandardResolver.Options);
            else
                return JsonToBytes(JsonConvert.SerializeObject(obj));
        }

        public static string Serialize<T>(T obj)
        {
            return BytesToJson(ObjectToBytes(obj));
        }

        public static T Deserialize<T>(string json)
        {
            return BytesToObject<T>(JsonToBytes(json));
        }

        public static string BytesToJson(byte[] bytes)
        {
            if (GlobalDefaults.ObjectEncodingType == GlobalDefaults.EncodingType.MESSAGE_PACK)
                return MessagePackSerializer.ConvertToJson(bytes, ContractlessStandardResolver.Options);
            else
                return Encoding.UTF8.GetString(bytes);
            //return MessagePackSerializer.ConvertToJson(bytes, GlobalDefaults.Serializer);
        }

        public static byte[] JsonToBytes(string json)
        {
            if (GlobalDefaults.ObjectEncodingType == GlobalDefaults.EncodingType.MESSAGE_PACK)
                return MessagePackSerializer.ConvertFromJson(json, ContractlessStandardResolver.Options);
            else
                return Encoding.UTF8.GetBytes(json);
            //return MessagePackSerializer.ConvertFromJson(json, GlobalDefaults.Serializer);
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

        //public static bool IsType<T>(byte[] bytes)
        //{
        //    if (GlobalDefaults.ObjectEncodingType == GlobalDefaults.EncodingType.JSON)
        //    {
        //        try
        //        {
        //            var inf = CompatibleJsonParser.GetInfo<T>();
        //            string json = BytesToJson(bytes);
        //            foreach (PropertyInfo i in inf.Item1)
        //            {
        //                if (!json.Contains(i.Name))
        //                {
        //                    return false;
        //                }
        //            }
        //            foreach (FieldInfo i in inf.Item2)
        //            {
        //                if (!json.Contains(i.Name))
        //                {
        //                    return false;
        //                }
        //            }

        //            CompatibleJsonParser.Deserialize<T>(BytesToJson(bytes));
        //            return true;
        //        }
        //        catch
        //        {
        //            return false;
        //        }
        //    }
        //    else
        //        try
        //        {
        //            string json = MessagePackSerializer.ConvertToJson(bytes);
        //            if (!typeof(T).IsPrimitive)
        //            {
        //                foreach (FieldInfo f in typeof(T).GetFields())
        //                {
        //                    if (!f.IsInitOnly && !json.Contains(f.Name)) return false;
        //                }
        //            }
        //            MessagePackSerializer.Deserialize<T>(bytes, ContractlessStandardResolver.Options);
        //            return true;
        //        }
        //        catch
        //        {
        //            return false;
        //        }
        //}
    }
}
