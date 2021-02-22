using System;
using System.Linq;

namespace SlippiCS
{
    public class FullWidth
    {
        public static string ToHalfWidth(string str)
        {
            Func<char, char> convertChar = (charCode) =>
            {
                if (charCode > 0xff00 && charCode < 0xff5f)
                {
                    return (char)(0x0020 + (charCode - 0xff00));
                }

                if (charCode == 0x3000)
                {
                    return (char)(0x0020);
                }

                return charCode;
            };

            var ret = str.Select(schar => convertChar(schar)).ToArray();
            return new string(ret);
        }
    }
}
