using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace SimpleNetwork
{
    public static class CompatibleJsonParser
    {
        static BindingFlags bind = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

        static Dictionary<Type, (PropertyInfo[], FieldInfo[])> TypeDecoding = new Dictionary<Type, (PropertyInfo[], FieldInfo[])>();

        public static bool IsType<T>(string json)
        {
            if (typeof(T).IsPrimitive)
            {
                
                int index = json.IndexOf('"');
                bool b = index != -1 && json[index - 1] != ':' && json.IndexOf('"', json.IndexOf('"', index)) == -1;
                if (b)
                    try
                    {
                        T test = (T)Convert.ChangeType(json.Substring(index, json.IndexOf('"', index + 1) - index), typeof(T));
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
            }

            foreach (PropertyInfo info in typeof(T).GetProperties(bind))
            {
                if ((info.GetSetMethod() != null || info.GetSetMethod(true) != null) && 
                    (info.PropertyType.IsPrimitive || 
                    info.PropertyType.IsEnum ||
                    info.PropertyType.Name.Equals(typeof(String).Name) ||
                    info.PropertyType.Name.Equals(typeof(Decimal).Name) ||
                    (info.PropertyType.IsArray && 
                    info.PropertyType.GetElementType().IsPrimitive)))
                {
                    if (!json.Contains(info.Name))
                    {
                        return false;
                    }
                }
            }

            foreach (FieldInfo info in typeof(T).GetFields(bind))
            {
                if (info.IsPublic &&
                    !info.IsLiteral &&
                    (info.FieldType.IsPrimitive || 
                    info.FieldType.IsEnum ||
                    info.FieldType.Name.Equals(typeof(String).Name) ||
                    info.FieldType.Name.Equals(typeof(Decimal).Name) ||
                    (info.FieldType.IsArray && 
                    info.FieldType.GetElementType().IsPrimitive)))
                {
                    if (!json.Contains(info.Name))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static string SerializeObject<T>(T obj)
        {
            string json = "{";

            if (typeof(T).IsPrimitive)
            {
                json += $"\"{obj}\"}}";
                return json;
            }

            foreach (PropertyInfo info in typeof(T).GetProperties(bind))
            {
                if ((info.GetSetMethod() != null || info.GetSetMethod(true) != null) && 
                    (info.PropertyType.IsPrimitive ||
                    info.PropertyType.IsEnum ||
                    info.PropertyType.Name.Equals(typeof(String).Name) ||
                    info.PropertyType.Name.Equals(typeof(Decimal).Name) ||
                    (info.PropertyType.IsArray &&
                    info.PropertyType.GetElementType().IsPrimitive)))
                {
                    if (info.PropertyType.IsArray)
                    {
                        json += $"{info.Name}:[";

                        Type et = info.PropertyType.GetElementType();

                        foreach (object o in (object[])info.GetValue(obj))
                        {
                            json += $"{obj},";
                        }
                        json += "]";
                    }
                    else if (
                        info.PropertyType.IsPrimitive || 
                        info.PropertyType.IsEnum ||
                        info.PropertyType.Name.Equals(typeof(String).Name) ||
                        info.PropertyType.Name.Equals(typeof(Decimal).Name))
                    {
                        json += $"{info.Name}:\"{info.GetValue(obj)}\"";
                    }
                }
            }

            foreach (FieldInfo info in typeof(T).GetFields(bind))
            {
                if (info.IsPublic &&
                    !info.IsLiteral &&
                    (info.FieldType.IsPrimitive ||
                    info.FieldType.IsEnum ||
                    info.FieldType.Name.Equals(typeof(String).Name) ||
                    info.FieldType.Name.Equals(typeof(Decimal).Name) ||
                    (info.FieldType.IsArray &&
                    info.FieldType.GetElementType().IsPrimitive)))
                {
                    if (info.FieldType.IsArray)
                    {
                        json += $"{info.Name}:";

                        Type et = info.FieldType.GetElementType();

                        foreach (object o in (object[])info.GetValue(obj))
                        {
                            json += $"{obj},";
                        }
                        json += "]";
                    }
                    else if (
                        info.FieldType.IsPrimitive || 
                        info.FieldType.IsEnum ||
                        info.FieldType.Name.Equals(typeof(String).Name) ||
                        info.FieldType.Name.Equals(typeof(Decimal).Name))
                    {
                        json += $"{info.Name}:\"{info.GetValue(obj)}\"";
                    }
                }
            }
            return json + '}';
        }

        public static T DeserializeObject<T>(string json)
        {
            if (!IsType<T>(json)) throw new Exception("Tried deserializing incorrect type");

            if (typeof(T).IsPrimitive)
            {
                int index = json.IndexOf('"');
                return (T)Convert.ChangeType(json.Substring(index, json.IndexOf('"', index + 1) - index), typeof(T));
            }

            T obj = (T)Activator.CreateInstance(typeof(T), true);

            foreach (PropertyInfo info in typeof(T).GetProperties(bind))
            {
                if ((info.GetSetMethod() != null || info.GetSetMethod(true) != null) && 
                    (info.PropertyType.IsPrimitive ||
                    info.PropertyType.IsEnum ||
                    info.PropertyType.Name.Equals(typeof(String).Name) ||
                    info.PropertyType.Name.Equals(typeof(Decimal).Name) ||
                    (info.PropertyType.IsArray &&
                    info.PropertyType.GetElementType().IsPrimitive)))
                {
                    if (info.PropertyType.IsArray)
                    {
                        int index = json.IndexOf(info.Name) + info.Name.Length + 2;
                        string val = json.Substring(index, json.IndexOf(']', index) - index);
                        if (string.IsNullOrEmpty(val))
                            info.SetValue(obj, default);
                        else
                            info.SetValue(obj, Convert.ChangeType(val.Split(','), info.PropertyType));
                    }
                    else if (
                        info.PropertyType.IsPrimitive ||
                        info.PropertyType.IsEnum ||
                        info.PropertyType.Name.Equals(typeof(String).Name) ||
                        info.PropertyType.Name.Equals(typeof(Decimal).Name))
                    {
                        int index = json.IndexOf(info.Name) + info.Name.Length + 2;
                        string val = json.Substring(index, json.IndexOf('"', index) - index);
                        if (string.IsNullOrEmpty(val))
                            info.SetValue(obj, default);
                        else
                            info.SetValue(obj, Convert.ChangeType(val, info.PropertyType));
                    }
                }
            }

            foreach (FieldInfo info in typeof(T).GetFields(bind))
            {
                if (info.IsPublic &&
                    !info.IsLiteral &&
                    (info.FieldType.IsPrimitive ||
                    info.FieldType.IsEnum ||
                    info.FieldType.Name.Equals(typeof(String).Name) ||
                    info.FieldType.Name.Equals(typeof(Decimal).Name) ||
                    (info.FieldType.IsArray &&
                    info.FieldType.GetElementType().IsPrimitive)))
                {
                    if (info.FieldType.IsArray)
                    {
                        int index = json.IndexOf(info.Name) + info.Name.Length + 2;
                        string val = json.Substring(index, json.IndexOf(']', index) - index);
                        if (string.IsNullOrEmpty(val))
                            info.SetValue(obj, default);
                        else
                            info.SetValue(obj, Convert.ChangeType(val.Split(','), info.FieldType));
                    }
                    else if (
                        info.FieldType.IsPrimitive ||
                        info.FieldType.IsEnum ||
                        info.FieldType.Name.Equals(typeof(String).Name) ||
                        info.FieldType.Name.Equals(typeof(Decimal).Name))
                    {
                        int index = json.IndexOf(info.Name) + info.Name.Length + 2;
                        string val = json.Substring(index, json.IndexOf('"', index) - index);
                        if (string.IsNullOrEmpty(val))
                            info.SetValue(obj, default);
                        else
                            info.SetValue(obj, Convert.ChangeType(val, info.FieldType));
                    }
                }
            }
            return obj;
        }

        public static string Serialize<T>(T obj)
        {
            return new Json(typeof(T), obj).Serialize();//Serialize(typeof(T), obj);
        }

        private static string Serialize(Type type, object obj)
        {
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type.IsEnum)
                return $"{{{Format(obj)}}}";

            string Json = "{";

            var InfoPair = GetInfo(type);

            foreach (PropertyInfo i in InfoPair.Item1)
            {
                if (i.PropertyType.IsPrimitive || i.PropertyType == typeof(string) || i.PropertyType == typeof(decimal) || i.PropertyType.IsEnum)
                {
                    try
                    {
                        object o = i.GetValue(obj);
                        Json += $"\"{i.Name}\":{(o == null ? "null" : Format(o))}";
                    }
                    catch (TargetException)
                    {
                        Json += $"\"{i.Name}\":null,";
                    }
                }
                else if (i.PropertyType.IsArray)
                {
                    Type t = i.PropertyType.GetElementType();

                    Json += $"\"{i.Name}\":[";
                    Array array = (Array)i.GetValue(obj);

                    if (t.IsPrimitive || t == typeof(string) || t == typeof(decimal) || t.IsEnum)
                    {
                        foreach (object o in array)
                        {
                            Json += $"{Format(o)},";
                        }
                    }
                    else
                    {
                        foreach (object o in array)
                        {
                            Json += Serialize(t, o) + ",";
                        }
                    }
                    Json = Json.Substring(0, Json.Length - 1) + "],";
                }
                else
                {
                    try
                    {
                        Json += $"\"{i.Name}\":{Serialize(i.PropertyType, i.GetValue(obj))},";
                    }
                    catch (TargetException)
                    {
                        Json += $"\"{i.Name}\":null,";
                    }
                }
            }
            foreach (FieldInfo i in InfoPair.Item2)
            {
                if (i.FieldType.IsPrimitive || i.FieldType == typeof(string) || i.FieldType == typeof(decimal) || i.FieldType.IsEnum)
                {
                    try
                    {
                        object o = i.GetValue(obj);

                        Json += $"\"{i.Name}\":{(o == null ? "null" : Format(o))},";
                    }
                    catch (TargetException)
                    {
                        Json += $"\"{i.Name}\":null,";
                    }
                }
                else if (i.FieldType.IsArray)
                {
                    Type t = i.FieldType.GetElementType();

                    Json += $"\"{i.Name}\":[";

                    Array array = (Array)i.GetValue(obj);

                    if (t.IsPrimitive || t == typeof(string) || t == typeof(decimal) || t.IsEnum)
                    {
                        foreach (object o in array)
                        {
                            Json += $"{Format(o)},";
                        }
                    }
                    else
                    {
                        foreach (object o in array)
                        {
                            Json += Serialize(t, o) + ",";
                        }
                    }
                    Json = Json.Substring(0, Json.Length - 1) + "],";
                }
                else
                {
                    try
                    {
                        object o = Serialize(i.FieldType, i.GetValue(obj));
                        Json += $"\"{i.Name}\":{(o == null ? "null" : o)},";
                    }
                    catch (TargetException)
                    {
                        Json += $"\"{i.Name}\":null,";
                    }
                }
            }
            Json += "}";
            return Json;
        }

        public static T Deserialize<T>(string json)
        {
            return (T)new Json(json).Deserialize(typeof(T));//Deserialize(json, typeof(T));
        }

        private static object Deserialize(string Json, Type type)
        {
            (PropertyInfo[], FieldInfo[]) contents = GetInfo(type);

            object Ob = Activator.CreateInstance(type, true);

            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type.IsEnum)
            {
                Ob = Convert.ChangeType(RemoveFormatting(Json), type);
                return Ob;
            }

            foreach (PropertyInfo i in contents.Item1)
            {
                if (i.PropertyType.IsPrimitive || i.PropertyType == typeof(string) || i.PropertyType == typeof(decimal) || i.PropertyType.IsEnum)
                {
                    i.SetValue(Ob, Convert.ChangeType(GetObjectString(ref Json, i.Name, i.PropertyType), i.PropertyType));
                }
                else if (i.PropertyType.IsArray)
                {
                    Type t = i.PropertyType.GetElementType();

                    List<string> items = GetArrayItems(ref Json, i.Name, t);

                    List<object> objects = new List<object>();

                    foreach (string item in items)
                    {
                        objects.Add(Deserialize(item, t));
                    }
                    Array newArr = Array.CreateInstance(t, objects.Count);
                    Array.Copy(objects.ToArray(), newArr, objects.Count);
                    i.SetValue(Ob, newArr);
                }
                else
                {
                    i.SetValue(Ob, Deserialize(Json, i.PropertyType));
                }
            }
            foreach (FieldInfo i in contents.Item2)
            {
                if (i.FieldType.IsPrimitive || i.FieldType == typeof(string) || i.FieldType == typeof(decimal) || i.FieldType.IsEnum)
                {
                    i.SetValue(Ob, Convert.ChangeType(GetObjectString(ref Json, i.Name, i.FieldType), i.FieldType));
                }
                else if (i.FieldType.IsArray)
                {
                    Type t = i.FieldType.GetElementType();

                    List<string> items = GetArrayItems(ref Json, i.Name, t);

                    List<object> objects = new List<object>();

                    foreach (string item in items)
                    {
                        objects.Add(Deserialize(item, t));
                    }
                    Array newArr = Array.CreateInstance(t, objects.Count);
                    Array.Copy(objects.ToArray(), newArr, objects.Count);
                    i.SetValue(Ob, newArr);
                }
                else
                {
                    i.SetValue(Ob, Deserialize(Json, i.FieldType));
                }
            }

            return Ob;
        }

        public static (PropertyInfo[], FieldInfo[]) GetInfo<T>()
        {
            Type t = typeof(T);

            if (t.IsPrimitive || t.Equals(typeof(decimal)) || t.Equals(typeof(string)))
                return (new PropertyInfo[0], new FieldInfo[0]);
            if (t.GetConstructors(bind).Where(c => c.GetParameters().Length == 0).ToArray().Length == 0)
                throw new Exception("Cannot serialize or deserialize a complex object withput parameterless constructors");

            if (!TypeDecoding.ContainsKey(t))
            {
                PropertyInfo[] UsableProps = t.GetProperties(bind).Where(info =>
                info.CanRead && info.CanWrite).ToArray();

                FieldInfo[] UsableFields = t.GetFields(bind).Where(
                    f => !f.IsLiteral).ToArray();
                TypeDecoding.Add(t, (UsableProps, UsableFields));
            }
            return TypeDecoding[t];
        }

        static (PropertyInfo[], FieldInfo[]) GetInfo(Type T)
        {
            if (!TypeDecoding.ContainsKey(T))
            {
                PropertyInfo[] UsableProps = T.GetProperties(bind).Where(info =>
                info.CanRead && info.CanWrite).ToArray();

                FieldInfo[] UsableFields = T.GetFields(bind).Where(
                    f => !f.IsLiteral).ToArray();
                TypeDecoding.Add(T, (UsableProps, UsableFields));
            }
            return TypeDecoding[T];
        }

        public static string GetObjectString(ref string Json, string Name, Type type)
        {
            string val = "";
            int depth = 0;
            int i = Json.IndexOf(Name) + Name.Length + 3;

            while (i < Json.Length && i >= 0)
            {
                if (Json[i - 1] != '\\')
                    switch (Json[i])
                    {
                        case '{':
                            depth++;
                            break;
                        case '}':
                            depth--;
                            break;
                    }
                if (depth < 0)
                    break;

                val += Json[i];

                i++;
            }
            if (val.Length > 0)
                Json = Json.Remove(Json.IndexOf(Name) - 1, Name.Length + 5 + val.Length);
            return RemoveFormatting(val);
        }

        public static string GetArrayString(ref string Json, string Name, Type type)
        {
            string val = "";
            int depth = 0;
            int i = Json.IndexOf(Name) + Name.Length + 3;

            while (i < Json.Length && i >= 0)
            {
                if (i > 0 && Json[i - 1] != '\\')
                    switch (Json[i])
                    {
                        case '[':
                            depth++;
                            break;
                        case ']':
                            depth--;
                            break;
                    }
                if (depth < 0)
                    break;

                val += Json[i];

                i++;
            }
            if (val.Length > 0)
                Json = Json.Remove(Json.IndexOf(Name) - 1, Name.Length + 5 + val.Length);
            return RemoveFormatting(val);
        }

        public static List<string> GetArrayItems(ref string Json, string Name, Type type)
        {
            string ArrayString = GetArrayString(ref Json, Name, type);

            List<string> items = new List<string>();
            
            if (type.IsPrimitive || type.IsEnum || type.Equals(typeof(string)) || type.Equals(typeof(decimal)))
            {
                string current = "";

                for (int i = 0; i < ArrayString.Length; i++)
                {
                    if (ArrayString[i] == ',' && ArrayString[i - 1] != '\\')
                    {
                        items.Add(current);
                        current = "";
                    }
                    else current += ArrayString[i];
                }
            }
            else
            {

            }

            return items;
        }

        private static string Format(object o)
        {
            string objStr = $"{o}";
            for (int i = 0; i < objStr.Length; i++)
            {
                char c = objStr[i];
                if (c == '"' || c == '{' || c == '}' || c == '[' || c == ']' || c == '\\' || c == ',')
                {
                    objStr = objStr.Insert(i, "\\");
                    i++;
                }
            }
            return objStr;
        }

        private static string RemoveFormatting(string s)
        {
            bool skip = false;

            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\')
                {
                    if (s[i + 1] == '\\')
                        skip = true;
                    s = s.Remove(i, 1);
                    i--;
                }
                else skip = false;
            }
            return s;
        }

        private class Json
        {
            public string Name;
            public string Value;

            public readonly JsonType jsonType;

            public List<Json> Contents = new List<Json>();

            public Json(string json, string name = null)
            {
                Name = name;
                int depth = 0;
                string NextName = "";
                string value = "";
                List<string> arrayValues = new List<string>();
                bool array = json[0] == '[' && json[json.Length - 1] == ']';
                bool esc = false;
                bool naming = true;
                bool isValue = true;
                bool primitiveField = false;
                bool endPrim = false;

                for (int i = !array ? 1 : 0; i < json.Length; i++)
                {
                    if (!esc)
                    {
                        if (!array)
                        {
                            switch (json[i])
                            {
                                case '"':
                                    if (NextName.Length == 0 && (depth == 0 || array))
                                    {
                                        //i++;
                                        //NextName += '"';
                                        naming = true;
                                    }
                                    else if (naming)
                                    {
                                        naming = false;
                                        i++;
                                    }
                                    else if (NextName.Length > 0)
                                        value += '"';
                                    break;
                                case '{':
                                    value += '{';
                                    if (!array)
                                    {
                                        depth++;
                                        array = false;
                                    }
                                    break;
                                case '}':
                                    if (!(i == json.Length - 1))
                                        value += '}';
                                    if (!array)
                                    {
                                        depth--;
                                        array = false;
                                    }
                                    if (depth < 0 && primitiveField)
                                        endPrim = true;
                                    break;
                                case '[':
                                    //if (!array.HasValue || array.Value)
                                    //{
                                    //    depth++;
                                    //    array = true;
                                    //}
                                    //else if (depth >= 1)
                                    //{
                                    //    value += '[';
                                    //}
                                    depth++;
                                    if (!array)
                                        value += '[';
                                    break;
                                case ']':
                                    //if (!array.HasValue || array.Value)
                                    //{
                                    //    depth--;
                                    //    array = true;
                                    //}
                                    //if (depth > 0)
                                    //{
                                    //    value += ']';
                                    //}
                                    if (!array)
                                        value += ']';
                                    depth--;
                                    break;
                                case ',':
                                    if (depth == 0)
                                    {
                                        if (primitiveField)
                                        {
                                            endPrim = true;
                                        }
                                    }
                                    else if (depth == 1 && array)
                                    {
                                        arrayValues.Add(value);
                                        value = "";
                                    }
                                    else
                                    {
                                        value += ',';
                                    }
                                    break;
                                case '\\':
                                    esc = true;
                                    if (value.Length > 0) value += '\\';
                                    break;
                                case ':':
                                    if (depth == 0 && json[i + 1] != '[' && json[i + 1] != '{')
                                        primitiveField = true;
                                    else
                                        value += ':';
                                    break;
                                default:
                                    if (depth == 0 && value.Length == 0 && naming)
                                    {
                                        NextName += json[i];
                                    }
                                    else
                                    {
                                        if (value.Length == 0) primitiveField = true;
                                        value += json[i];
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            switch(json[i])
                            {
                                case ',':
                                    if (depth > 1)
                                        value += ',';
                                    else
                                    {
                                        arrayValues.Add(value);
                                        value = "";
                                    }
                                    break;
                                case '\\':
                                    esc = true;
                                    break;
                                case '[':
                                    if (depth > 0)
                                        value += '[';
                                    depth++;
                                    break;
                                case ']':
                                    if (depth > 1)
                                        value += ']';
                                    depth--;
                                    break;
                                case '{':
                                    value += '{';
                                    depth++;
                                    break;
                                case '}':
                                    value += '}';
                                    depth--;
                                    break;
                                default:
                                    value += json[i];
                                    break;
                            }
                        }
                    }
                    else
                    {
                        esc = false;
                        if (depth == 0 && value.Length == 0 && naming)
                        {
                            NextName += json[i];
                        }
                        else
                        {
                            value += json[i];
                        }
                    }
                    if ((!primitiveField && value.Length > 0 && depth == 0) || (primitiveField && (endPrim || (array && depth == 0))))
                    {
                        isValue = false;
                        primitiveField = false;
                        endPrim = false;
                        if (array)
                        {
                            arrayValues.Add(value);
                            foreach (string s in arrayValues)
                            {
                                Contents.Add(new Json(s));
                            }
                            arrayValues.Clear();
                            NextName = "";
                            value = "";
                        }
                        else
                        {
                            Contents.Add(new Json(value, NextName));
                            value = "";
                            NextName = "";
                        }
                    }
                }
                if (isValue)
                {
                    Value = json;
                }
            }

            public Json (Type type, object obj, string name = null)
            {
                this.Name = name;
                if (type.IsPrimitive || type.Equals(typeof(string)) || type.Equals(typeof(decimal)))
                {
                    jsonType = JsonType.Primitive;
                    if (obj == null) return;
                    Value = obj.ToString();
                    return;
                }
                else if (type.IsArray)
                {
                    jsonType = JsonType.Array;
                    if (obj == null) return;
                    Array items = (Array)obj;
                    foreach (object item in items)
                    {
                        Contents.Add(new Json(type.GetElementType(), item));
                    }
                    return;
                }
                else
                    jsonType = JsonType.Object;

                if (obj == null) return;
                (PropertyInfo[], FieldInfo[]) inf = GetInfo(type);

                foreach (PropertyInfo i in inf.Item1)
                {
                    Contents.Add(new Json(i.PropertyType, i.GetValue(obj), i.Name));
                }
                foreach (FieldInfo i in inf.Item2)
                {
                    Contents.Add(new Json(i.FieldType, i.GetValue(obj), i.Name));
                }
            }

            public string Serialize()
            {
                int jt = (int)jsonType;
                string Json = (Name != null ? $"\"{Name}\"" + ":" : "") + (jt == 0 ? Format(Value) : jt == 1 ? '[' : '{');

                foreach (Json json in Contents)
                {
                    Json += json.Serialize() + ',';
                }
                if (jt != 0 && Json[Json.Length - 1] == ',') Json = Json.Substring(0, Json.Length - 1);
                return Json + (jt == 0 ? "" : jt == 1 ? "]" : "}");
            }

            public object Deserialize(Type type)
            {
                if (type.IsPrimitive || type.Equals(typeof(string)) || type.Equals(typeof(decimal)) || type.Equals(typeof(object)))
                {
                    return Convert.ChangeType(Value, type);
                }
                else if (type.IsArray)
                {
                    List<object> objects = new List<object>();

                    foreach (Json o in Contents)
                    {
                        objects.Add(o.Deserialize(type.GetElementType()));
                    }
                    Array arr = Array.CreateInstance(type.GetElementType(), objects.Count);
                    Array.Copy(objects.ToArray(), arr, arr.Length);
                    return arr;
                }
                else
                {
                    object ob = Activator.CreateInstance(type);

                    var inf = GetInfo(type);
                    int i = 0;

                    foreach (PropertyInfo I in inf.Item1)
                    {
                        I.SetValue(ob, Contents[i].Deserialize(I.PropertyType));
                        i++;
                    }
                    foreach (FieldInfo I in inf.Item2)
                    {
                        I.SetValue(ob, Contents[i].Deserialize(I.FieldType));
                        i++;
                    }

                    return ob;
                }
            }

            public enum JsonType
            {
                Primitive,
                Array,
                Object
            }
        }
    }
}
