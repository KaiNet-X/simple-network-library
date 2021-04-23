using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetwork
{
    internal static class Utilities
    {
        public static Dictionary<string, Type> NameTypeAssociations = new Dictionary<string, Type>();

        //public static string CleanJson(string json, ref bool IsValid)
        //{
        //    List<string> SplitObjects = new List<string>(json.Split('{', '}'));
        //    string cleanJson = "";
        //    for (int i = 0; i < SplitObjects.Count; i++)
        //    {
        //        if (SplitObjects[i].Trim('"', ';').Length == 0)
        //        {
        //            SplitObjects.RemoveAt(i);
        //            i--;
        //        }
        //        else if (json.Contains("{") && json.Contains("}"))
        //        {
        //            SplitObjects[i] = '{' + SplitObjects[i] + '}';
        //            cleanJson += SplitObjects[i];
        //        }
        //        else
        //        {
        //            cleanJson += SplitObjects[i];
        //        }
        //    }
        //    //for (int i = 0; i < SplitObjects.Count; i++)
        //    //{
        //    //    List<string> CleanedJson = new List<string>(SplitObjects[i].Split('"', '"'));

        //    //    for (int I = 0; I < CleanedJson.Count; I++)
        //    //    {
        //    //        if (CleanedJson[i].Trim('"', ';').Length == 0)
        //    //        {
        //    //            CleanedJson.RemoveAt(i);
        //    //            i--;
        //    //        }
        //    //        else
        //    //        {
        //    //            CleanedJson[i] = '"' + CleanedJson[i] + '"';
        //    //            cleanJson += CleanedJson[i];
        //    //        }
        //    //    }
        //    //}
        //    return cleanJson;
        //}

        public static int IndexInByteArray(byte[] Bytes, byte[] SearchBytes, int offset = 0)
        {
            for (int i = offset; i <= Bytes.Length - SearchBytes.Length; i++)
            {
                for (int I = 0; I < SearchBytes.Length; I++)
                {
                    if (!SearchBytes[I].Equals(Bytes[i + I]))
                    {
                        break;
                    }
                    else if (I == SearchBytes.Length - 1 && SearchBytes[I].Equals(Bytes[i + I]))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public static bool ByteArrayContains(byte[] Bytes, byte[] SearchBytes)
        {
            bool sequenceFound = false;

            for (int i = 0; i <= Bytes.Length - SearchBytes.Length; i++)
            {
                if (Bytes.Skip(i).Take(SearchBytes.Length).SequenceEqual(SearchBytes))
                {
                    sequenceFound = true;
                    break;
                }
            }

            return sequenceFound;
        }

        public static bool IsArray(string typeName) => typeName.Contains('[');

        public static Type GetTypeFromName(string name)
        {
            if (NameTypeAssociations.ContainsKey(name))
                return NameTypeAssociations[name];
            else
            {
                Type t = ResolveTypeFromName(name);
                try
                {
                    NameTypeAssociations.Add(name, t);
                }
                catch
                {
                    return NameTypeAssociations[name];
                }
                return t;
            }
        }

        public static Type ResolveTypeFromName(string name)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies()
                 .SelectMany(x => x.GetTypes())
                 .First(x => x.Name == GetBaseTypeName(name));

            if (!IsArray(name))
            {
                return type;
            }
            else if (name.Contains(","))
            {
                type = MultiDimensionalArrayType(type, (byte)name.Where(c => c == ',').Count());
            }
            else
            {
                type = JaggedArrayType(type, (byte)name.Where(c => c == '[').Count());
            }
            return type;
        }

        public static bool IsHerritableType<T>(Type obType)
        {
            return typeof(T).IsAssignableFrom(obType);
        }

        public static string GetBaseTypeName(string typeName) =>
            typeName.Replace("[", "").Replace(",", "").Replace("]", "");

        public static Type JaggedArrayType(Type baseType, byte dimensions)
        {
            Type type = baseType;
            for (int i = 0; i < dimensions; i++)
            {
                type = Array.CreateInstance(type, 0).GetType();
            }
            return type;
        }

        public static Type MultiDimensionalArrayType(Type baseType, byte dimensions)
        {
            int[] lengths = new int[dimensions + 1];
            for (int i = 0; i <= dimensions; i++)
                lengths[i] = 0;
            return Array.CreateInstance(baseType, lengths).GetType();
        }

        public static Task SendAsync(this Socket soc, byte[] bytes)
        {
            var tcs = new TaskCompletionSource<int>();

            soc.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, iar =>
            {
                try { tcs.TrySetResult(soc.EndSend(iar)); }
                catch (Exception exc) { tcs.TrySetException(exc); }
            }, null);
            
            return tcs.Task;
        }

        public static void RecursiveDelete(string path)
        {
            if (Directory.Exists(path))
            {
                foreach (string s in Directory.GetFiles(path))
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(s);
                }
                foreach (string s in Directory.GetDirectories(path))
                    RecursiveDelete(s);
            }
        }
    }
}
