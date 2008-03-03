using System;
using System.Collections.Generic;


namespace TradeLib
{


    public class Bar
    {
        private decimal h = decimal.MinValue;
        private decimal l = decimal.MaxValue;
        private decimal o = 0;
        private decimal c = 0;
        private int v = 0;
        private BarInterval tunits = BarInterval.FiveMin; //5min bar default
        private int bartime = 0;
        private int bardate = 0;
        private bool DAYEND = false;
        public bool DayEnd { get { return DAYEND; } }
        public decimal High { get { return h; } }
        public decimal Low { get { return l; } }
        public decimal Open { get { return o; } }
        public decimal Close { get { return c; } }
        public int Volume { get { return v; } }

        public Bar() : this(new Tick(), BarInterval.FiveMin) { }
        public Bar(decimal open, decimal high, decimal low, decimal close, int vol, int date, int time)
        {
            h = high;
            o = open;
            l = low;
            c = close;
            v = vol;
            bardate = date;
            bartime = time;
        }
        public Bar(decimal open, decimal high, decimal low, decimal close, int vol, int date)
        {
            h = high;
            o = open;
            l = low;
            c = close;
            v = vol;
            bardate = date;
        }
        public Bar(Bar b)
        {
            h = b.High;
            l = b.Low;
            o = b.Open;
            c = b.Close;
            DAYEND = b.DAYEND;
            bartime = b.bartime;
            bardate = b.bardate;
        }
        
        
        public Bar(Tick t) : this(t, BarInterval.FiveMin) { }
        public Bar(Tick t, BarInterval tu) 
        {
            tunits = tu;
            Accept(t);
        }
        public int Bartime { get { return bartime; } }
        public int Bardate { get { return bardate; } }
        private int BarTime(int time) { return time - (time % (int)this.tunits); }

        public bool Accept(Tick t)
        {
            if (bartime == 0) { bartime = BarTime(t.time); bardate = t.date;}
            if (bardate != t.date) DAYEND = true;
            else DAYEND = false;
            if ((BarTime(t.time) != bartime) || (bardate!=t.date)) return false; // not our tick
            if (o == 0) o = t.trade;
            if (t.trade > h) h = t.trade;
            if (t.trade < l) l = t.trade;
            c = t.trade;
            return true;
        }
        public override string ToString() { return "OHLC ("+bartime+") " + o + "," + h + "," + l + "," + c; }
        public static Bar FromCSV(string record)
        {
            // google used as example
            string[] r = record.Split(',');
            if (r.Length < 6) return null;
            DateTime d = new DateTime();
            try
            {
                d = DateTime.Parse(r[0]);
            }
            catch (System.FormatException) { return null; }
            int date = (d.Year*10000)+(d.Month*100)+d.Day;
            decimal open = Convert.ToDecimal(r[1]);
            decimal high = Convert.ToDecimal(r[2]);
            decimal low = Convert.ToDecimal(r[3]);
            decimal close = Convert.ToDecimal(r[4]);
            int vol = Convert.ToInt32(r[5]);
            return new Bar(open,high,low,close,vol,date);
        }
    }

    public enum BarInterval
    {
        Minute = 1,
        FiveMin = 5,
        FifteenMin = 15,
        Hour = 60,
        Day = 450
    }
}
