using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;

namespace TradeLib
{
    public class Util
    {
        public static string TLBaseDir { get { return @"c:\program files\tradelink\"; } }
        public static string TLProgramDir { get { return TLBaseDir + "TradeLink\\"; } }
        public static string TLTickDir { get { return TLBaseDir + "TickData\\"; } }
        public static DateTime ToDateTime(int TradeLinkDate)
        {
            if (TradeLinkDate < 10000) throw new Exception("Not a date, or invalid date provided");
            return ToDateTime(TradeLinkDate, 0, 0);
        }

        public static DateTime ToDateTime(int TradeLinkTime, int TradeLinkSec)
        {
            return ToDateTime(0, TradeLinkTime, TradeLinkSec);
        }


        public static DateTime ToDateTime(int TradeLinkDate, int TradeLinkTime, int TradeLinkSec)
        {
            int hour = (int)Math.Floor((decimal)TradeLinkTime / 100);
            int min = TradeLinkTime - (hour*100);
            int year = 1, day = 1, month = 1;
            if (TradeLinkDate != 0)
            {
                year = Convert.ToInt32(TradeLinkDate.ToString().Substring(0, 4));
                month = Convert.ToInt32(TradeLinkDate.ToString().Substring(4, 2));
                day = Convert.ToInt32(TradeLinkDate.ToString().Substring(6, 2));
            }
            return new DateTime(year, month, day, hour, min, TradeLinkSec);
        }

        public static int ToTLDate(DateTime dt)
        {
            return (dt.Year * 10000) + (dt.Month * 100) + dt.Day;
        }
        public static string CleanVer(string ver)
        {
            Regex re = new Regex("[0-9]+");
            Match m = re.Match(ver);
            if (m.Success) return m.Value;
            else return "0";
        }
		public static bool isEarlyClose(int today) 
        {
            try
            {
                return GetCloseTime().Contains(today);
            }
            catch (Exception) { return false; }
        }
        public static int GetEarlyClose(int today)
        {
            try
            {
                return (int)GetCloseTime()[today];
            }
            catch (Exception) { return 0; }
        }
        public static Hashtable GetCloseTime()
        {
            StreamReader f = new StreamReader("c:\\program files\\tradelink\\tradelink\\EarlyClose.csv");
            string[] r = new string[2];
            string line = "";
            Hashtable h = new Hashtable();
            while ((line = f.ReadLine())!=null)
            {
                r = line.Split(',');
                h.Add(Convert.ToInt32(r[0]),Convert.ToInt32(r[1]));
            }
            f.Close();
            return h; 
        }
        public static string Fills2StringDelim(Hashtable fills,string d)
        { // works on a queue of Trade objects
            string csv = "";
            IEnumerator e = fills.GetEnumerator();
            while (e.MoveNext())
            {
                DictionaryEntry dict = (DictionaryEntry)e.Current;
                Queue stocktrades = (Queue)fills[dict.Key];
                foreach (object o in stocktrades)
                {
                    Trade t = (Trade)o;
                    csv += t + "\n";
                }
            }
            return csv;
        }
        public static void String2FileAppend(string s, string filepath)
        {// will take any string and save it to a given file
            FileStream file = new FileStream(filepath, FileMode.Create);
            StreamWriter sw = new StreamWriter(file);
            string[] sfile = s.Split('\n');
            sw.WriteLine("Date,Time,Symbol,Side,xSize,xPrice,Comment");
            for (int line = 0; line<sfile.Length; line++)  sw.WriteLine(sfile[line]);
            sw.Close();
            file.Close();
        }

        static bool IsBox(Type t) { return (t.BaseType.IsSubclassOf(typeof(Box))) || t.BaseType.Equals(typeof(Box)); }
        public static List<string> GetBoxList(string boxdll)
        {
            List<string> boxlist = new List<string>();
            Assembly a;
            Type[] t;
            try
            {
                a = Assembly.LoadFrom(boxdll);
            }
            catch (Exception ex) { boxlist.Add(ex.Message); return boxlist; }
            try
            {
                t = a.GetTypes();
            }
            catch (Exception ex) { boxlist.Add(ex.Message); return boxlist; }
            for (int i = 0; i < t.GetLength(0); i++)
                if (IsBox(t[i])) boxlist.Add(t[i].FullName);
            return boxlist;
        }

        
    }
}
