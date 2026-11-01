using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.SharedSysFuncs
{
    public static class NumberToWords
    {
        public static string convert(int num)
        {
            try
            {
                return n2w(num, "", "");
            }
            catch (Exception ex) { throw ex; }
        }
        private static string n2w(int num, string wrds, string numLvl)
        {
            WordCollections wrdColl = new WordCollections();
            try
            {
                var numStr = num.ToString();
                var len = numStr.Length;
                var genWrds = "";

                if (len == 1)
                    genWrds = wrdColl.onesDc[num];
                else if (len > 1 && num < 20)
                    genWrds = wrdColl.teensDc[num];
                else if (len == 2 && num >= 20)
                {
                    genWrds = wrdColl.tensDc[Convert.ToInt32(numStr.Substring(0, 1))];
                    if (Convert.ToInt32(numStr.Substring(1, 1)) > 0)
                        genWrds = n2w(Convert.ToInt32(numStr.Substring(1, 1)), genWrds, "");
                }
                else if (len == 3)
                {
                    genWrds = wrdColl.onesDc[Convert.ToInt32(numStr.Substring(0, 1))];
                    if (numStr.Substring(1, 2) == "00")
                        genWrds = genWrds + " Hundred";
                    else
                        genWrds = n2w(Convert.ToInt32(numStr.Substring(1, 2)), genWrds + " Hundred and", "");
                }
                else
                {
                    string tmpNumLvl = "";
                    int eftNum, remNum;
                    int numMod = len % 3;

                    if (len > 0 && numMod > 0)
                    {
                        eftNum = Convert.ToInt32(numStr.Substring(0, numMod));
                        remNum = Convert.ToInt32(numStr.Substring(numMod, numStr.Length - numMod));
                    }
                    else
                    {
                        eftNum = Convert.ToInt32(numStr.Substring(0, 3));
                        remNum = Convert.ToInt32(numStr.Substring(3, numStr.Length - 3));
                    }

                    if (len > 3 && len < 7) // Thousand
                        tmpNumLvl = "Thousand";
                    else if (len >= 7 && len < 10) // Million
                        tmpNumLvl = "Million";
                    else if (len >= 10 && len < 13) // Billion
                        tmpNumLvl = "Billion";
                    else if (len >= 12) // Trillion
                        tmpNumLvl = "Trillion";
                    else
                        genWrds = "Zero or out of range.";

                    genWrds = n2w(eftNum, genWrds, tmpNumLvl);
                    if (remNum > 0)
                        genWrds = n2w(remNum, genWrds, "");
                    else
                        genWrds = genWrds + " " + numLvl;
                }
                return (wrds + " " + genWrds + " " + numLvl).Trim();
            }
            catch (Exception ex) { throw ex; }
        }

        partial class WordCollections
        {
            #region Member Variables & Collections
            public Dictionary<int, string> onesDc = new Dictionary<int, string>();
            public Dictionary<int, string> teensDc = new Dictionary<int, string>();
            public Dictionary<int, string> tensDc = new Dictionary<int, string>();

            #endregion Member Variables & Collections

            #region Constructors
            public WordCollections()
            {
                // Load Ones Dictionary
                onesDc.Add(1, "One");
                onesDc.Add(2, "Two");
                onesDc.Add(3, "Three");
                onesDc.Add(4, "Four");
                onesDc.Add(5, "Five");
                onesDc.Add(6, "Six");
                onesDc.Add(7, "Seven");
                onesDc.Add(8, "Eight");
                onesDc.Add(9, "Nine");

                // Load Teens Dictionary
                teensDc.Add(10, "Ten");
                teensDc.Add(11, "Eleven");
                teensDc.Add(12, "Twelve");
                teensDc.Add(13, "Thirteen");
                teensDc.Add(14, "Fourteen");
                teensDc.Add(15, "Fifteen");
                teensDc.Add(16, "Sixteen");
                teensDc.Add(17, "Seventeen");
                teensDc.Add(18, "Eighteen");
                teensDc.Add(19, "Nineteen");

                // Load Tens Dictionary
                tensDc.Add(2, "Twenty");
                tensDc.Add(3, "Thirty");
                tensDc.Add(4, "Forty");
                tensDc.Add(5, "Fifty");
                tensDc.Add(6, "Sixty");
                tensDc.Add(7, "Seventy");
                tensDc.Add(8, "Eighty");
                tensDc.Add(9, "Ninty");
            }
            #endregion Constructors
        }
    }
}
