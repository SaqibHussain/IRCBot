using System.Text;
using System.Text.RegularExpressions;

namespace IRCBot
{
    public static class Common
    {
        private const string CTRLK = "\u0003";
        private const string CTRLB = "\u0002";

        //Strip off the anything with a Ctrl+K or Ctrl+B followed by two numbers
        public static string StripIRCColours(string input) => 
            Regex.Replace(input, "[\u0001-\u0003][0-9]?[0-9]?", "").Trim();

        public static string GenerateCommaList(string[] arr)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string s in arr)
            {
                sb.Append(s + ", ");
            }

            return sb.ToString().Trim().TrimEnd(',');
        }

        #region Extension methods
        public static string GetStringFromElements(this string[] data, int startIndex)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = startIndex; i < data.Length; i++)
                sb.Append(data[i]).Append(" ");
            return sb.ToString();
        }
        #endregion
    }
}
