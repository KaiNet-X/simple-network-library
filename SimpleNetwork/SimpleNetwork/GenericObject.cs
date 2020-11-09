using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Text;

namespace SimpleNetwork
{
    //internal class GenericObject<O>
    //{
    //    private O ob;

    //    public GenericObject(O ob)
    //    {
    //        this.ob = ob;
    //    }

    //    public GenericObject()
    //    {

    //    }

    //    private string ConvertToString(O obj)
    //    {
    //        return JsonConvert.SerializeObject(obj);
    //    }

    //    public string ConvertToString() => ConvertToString(ob);

    //    private byte[] ConvertToBytes(O obj) => ConvertToBytes(ConvertToString(obj));
    //    private byte[] ConvertToBytes(string s)
    //    {
    //        return Encoding.ASCII.GetBytes(s);
    //    }
    //    public byte[] ConvertToBytes() => ConvertToBytes(ConvertToString(ob));

    //    public O BytesToObject(byte[] bytes)
    //    {
    //        string s = Encoding.ASCII.GetString(bytes);

    //        return JsonConvert.DeserializeObject<O>(s);
    //    }

    //    public static bool IsType(byte[] bytes)
    //    {
    //        try
    //        {
    //            string json = Encoding.ASCII.GetString(bytes);
    //            if (!typeof(O).IsPrimitive)
    //            {
    //                foreach (FieldInfo f in typeof(O).GetFields())
    //                {
    //                    if (!f.IsInitOnly && !json.Contains(f.Name)) return false;
    //                }
    //            }
    //            else
    //            {
    //                JsonConvert.DeserializeObject<O>(json);
    //            }
    //            return true;
    //        }
    //        catch(JsonException)
    //        {
    //            return false;
    //        }
    //    }
    //}
}
