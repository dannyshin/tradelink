using System;

namespace TradeLib
{
    [Serializable]
    public class Tick
    {
        public string sym = "";
        public int factor = 100;
        public int time;
        public int date;
        public int sec;
        public int size;
        public decimal trade;
        public decimal bid;
        public decimal ask;
        public int bs;
        public int os;
        public string be;
        public string oe;
        public string ex;
        public bool IndexTick { get { return size < 0; } }
        public bool hasBid { get { return (bid != 0) && (bs != 0); } }
        public bool hasAsk { get { return (ask != 0) && (os != 0); } }
        public bool FullQuote { get { return hasBid && hasAsk; } }
        public bool isQuote { get { return (!isTrade && (hasBid || hasAsk)); } }
        public bool isTrade { get { return (trade != 0) && (size != 0); } }
        public bool hasTick { get { return (this.isTrade || FullQuote); } }
        public bool atHigh(decimal high) { return (isTrade && (trade>=high)); }
        public bool atLow(decimal low) { return (isTrade && (trade <= low)); }
        public bool atHigh(BoxInfo bi) { return (isTrade && (trade >= bi.High)); }
        public bool atLow(BoxInfo bi) { return (isTrade && (trade <= bi.Low)); }
        public int ts { get { return size / 100; } } // normalized to bs/os
        public Tick() { }
        public Tick(string symbol) { this.sym = symbol; }
        public Tick(Tick c)
        {
            if (c.sym!="") sym = c.sym;
            time = c.time;
            date = c.date;
            sec = c.sec;
            size = c.size;
            trade = c.trade; 
            bid = c.bid; 
            ask = c.ask;
            bs = c.bs;
            os = c.os;
            be = c.be;
            oe = c.oe;
            ex = c.ex;
        }
        public Tick(Tick a, Tick b)
        {   // this constructor creates a new tick by combining two ticks
            // this is to handle tick updates that only provide bid/ask changes (like anvil)

            // a = old tick, b = new tick or tick update
            if (b.sym != a.sym) return; // don't combine different symbols
            if (b.time < a.time) return; // don't process old updates
            time = b.time;
            date = b.date;
            sec = b.sec;
            sym = b.sym;

            if (b.isTrade)
            {
                trade = b.trade;
                size = b.size;
                ex = b.ex;
                //
                bid = a.bid;
                ask = a.ask;
                os = a.os;
                bs = a.bs;
                be = a.be;
                oe = a.oe;
            }
            else if (b.hasAsk && b.hasBid)
            {
                bid = b.bid;
                ask = b.ask;
                bs = b.bs;
                os = b.os;
                be = b.be;
                oe = b.oe;
                //
                trade = a.trade;
                size = a.size;
                ex = a.ex;
            }
            else if (b.hasAsk)
            {
                ask = b.ask;
                os = b.os;
                oe = b.oe;
                //
                bid = a.bid;
                bs = a.bs;
                be = a.be;
                trade = a.trade;
                size = a.size;
                ex = a.ex;
            }
            else if (b.hasBid)
            {
                bid = b.bid;
                bs = b.bs;
                be = b.be;
                //
                ask = a.ask;
                os = a.os;
                oe = a.oe;
                trade = a.trade;
                size = a.size;
                ex = a.ex;
            }
        }

        public override string ToString()
        {
            if (!this.hasTick) return "";
            if (this.isTrade) return sym+" "+this.size + "@" + this.trade + " " + this.ex;
            else return sym+" "+this.bid + "x" + this.ask + " (" + this.bs + "x" + this.os + ") " + this.be + "," + this.oe;
        }

        public string toTLmsg()
        {
            Tick t = this;
            const char d = ',';
            string s = t.sym + d + t.date + d + t.time + d + t.sec + d + t.trade + d + t.size + d + t.ex + d + t.bid + d + t.ask + d + t.bs + d + t.os + d + t.be + d + t.oe + d;
            return s;
        }

        public void NewQuote(int date, int time, int sec, decimal bid, decimal ask, int bs, int os, string be, string oe) 
        {
            this.date = date;
            this.time = time;
            this.sec = sec;
            this.bid = bid;
            this.ask = ask;
            this.be = be.Trim();
            this.oe= oe.Trim();
            this.os = os;
            this.bs = bs;
            this.trade = 0;
            this.size = 0;

        }

        public void NewTrade(int date, int time, int sec, decimal trade, int size, string ex)
        {
            this.date = date;
            this.time = time;
            this.sec = sec;
            this.trade = trade;
            this.size = size;
            this.ex = ex.Trim();
            this.bid = 0;
        }
    }
}
