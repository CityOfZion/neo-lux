using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEO.mwherman2000.MonitorEvents
{
    public class Helpers
    {
        public static string ToHex(string buffer)
        {
            string result = "0x";

            if (buffer == null) return "";

            Int16 length = (Int16)buffer.Length;

            if (length == 0) return result;

            int chArrayLength = length * 2;

            char[] chArray = new char[chArrayLength];
            int i = 0;
            int index = 0;
            for (i = 0; i < chArrayLength; i += 2)
            {
                byte b = (byte)buffer[index++];
                chArray[i] = ToHexChar(b / 16);
                chArray[i + 1] = ToHexChar(b % 16);
            }

            return (result + new String(chArray, 0, chArray.Length));
        }

        public static string ToHex(byte[] buffer)
        {
            string result = "0x";

            if (buffer == null) return "";

            Int16 length = (Int16)buffer.Length;

            if (length == 0) return result;

            int chArrayLength = length * 2;

            char[] chArray = new char[chArrayLength];
            int i = 0;
            int index = 0;
            for (i = 0; i < chArrayLength; i += 2)
            {
                byte b = buffer[index++];
                chArray[i] = ToHexChar(b / 16);
                chArray[i + 1] = ToHexChar(b % 16);
            }

            return (result + new String(chArray, 0, chArray.Length));
        }

        private static char ToHexChar(int i)
        {
            if (i >= 0 && i < 16)
            {
                if (i < 10)
                {
                    return (char)(i + '0');
                }
                else
                {
                    return (char)(i - 10 + 'A');
                }
            }
            else
            {
                return '?';
            }
        }
    }
}
