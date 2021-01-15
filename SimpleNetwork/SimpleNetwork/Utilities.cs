using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleNetwork
{
    internal static class Utilities
    {
        public static string CleanJson(string json, ref bool IsValid)
        {
            List<string> SplitObjects = new List<string>(json.Split('{', '}'));
            string cleanJson = "";
            for (int i = 0; i < SplitObjects.Count; i++)
            {
                if (SplitObjects[i].Trim('"', ';').Length == 0)
                {
                    SplitObjects.RemoveAt(i);
                    i--;
                }
                else if (json.Contains("{") && json.Contains("}"))
                {
                    SplitObjects[i] = '{' + SplitObjects[i] + '}';
                    cleanJson += SplitObjects[i];
                }
                else
                {
                    cleanJson += SplitObjects[i];
                }
            }
            //for (int i = 0; i < SplitObjects.Count; i++)
            //{
            //    List<string> CleanedJson = new List<string>(SplitObjects[i].Split('"', '"'));

            //    for (int I = 0; I < CleanedJson.Count; I++)
            //    {
            //        if (CleanedJson[i].Trim('"', ';').Length == 0)
            //        {
            //            CleanedJson.RemoveAt(i);
            //            i--;
            //        }
            //        else
            //        {
            //            CleanedJson[i] = '"' + CleanedJson[i] + '"';
            //            cleanJson += CleanedJson[i];
            //        }
            //    }
            //}
            return cleanJson;
        }

        //public static int IndexInByteArray(byte[] Bytes, byte[] SearchBytes, int offset = 0)
        //{
        //    int index = -1;
        //    int Difference = Bytes.Length - SearchBytes.Length;

        //    for (int i = offset; i <= Bytes.Length - SearchBytes.Length; i++)
        //    {
        //        if (i > Difference) break;
        //        if (Bytes.Skip(i).Take(SearchBytes.Length).SequenceEqual(SearchBytes))
        //        {
        //            index = i;
        //            break;
        //        }
        //    }
        //    return index;
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

        public static bool IsArray(string typeName) => typeName.Contains(',');

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
    }
}
