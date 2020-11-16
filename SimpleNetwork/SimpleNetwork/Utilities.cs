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

        public static int IndexInByteArray(byte[] Bytes, byte[] SearchBytes, int offset = 0)
        {
            int index = -1;

            for (int i = offset; i <= Bytes.Length - SearchBytes.Length; i++)
            {
                if (Bytes.Skip(i).Take(SearchBytes.Length).SequenceEqual(SearchBytes))
                {
                    index = i;
                    break;
                }
            }
            return index;
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
    }
}
