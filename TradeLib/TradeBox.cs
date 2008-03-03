using System;
using System.Collections.Generic;
using System.Text;

namespace TradeLib
{
    // class to mirror (and eventually load javascript equivalent of)
    // eSignal TradeBox class's functionaltiy
    public class TradeBox : Box
    {
        public TradeBox(NewsService ns) : base(ns) { EntrySize = 100; Stop = .1m; ProfitTarget = 0; }
        protected decimal sPrice = 0;
        protected int lotsize = 0;
        protected decimal stop = 1;
        protected decimal profit = 0;
        protected int profitsize = 0;
        protected bool side = true;
        public bool Side { get { return side; } set { side = value; } }
        public decimal Stop { get { return stop; } set { stop = value; } }
        public decimal ProfitTarget { get { return profit; } set { profit = value; } }
        public int ProfitSize { get { return profitsize; } set { profitsize = value; } }
        protected BarList bl;
        protected Tick tick;
        protected decimal getMostRecentTrade() { return tick.trade; }
        protected decimal getMostRecentBid() { return tick.bid; }
        protected decimal getMostRecentAsk() { return tick.ask; }
        protected decimal getMostRecentTradeSize() { return tick.size; }
        protected decimal getMostRecentBidSize() { return tick.bs; }
        protected decimal getMostRecentAskSize() { return tick.os; }
        protected bool newTrade() { return (PosSize != 0) && (AvgPrice == 0); }
        protected int EntrySize { get { return lotsize; } set { lotsize = value; } }
        protected virtual bool EnterLong() { return false; }
        protected virtual bool EnterShort() { return false;  }
        protected virtual bool Exit() { return false; }
        protected virtual bool IgnoreTick() { return false; }
        protected decimal Profit { get { return (PosSize > 0) ? tick.trade - AvgPrice : AvgPrice - tick.trade; } }
        void checkMoveStop()
        {
            if (Math.Abs(sPrice - this.tick.trade) > (this.stop * 1.5m)) sPrice = tick.trade - ((stop * 1.5m) * ((PosSize < 0) ? -1 : 1));
        }
        bool hitStop() { return (((tick.trade - sPrice) * ((PosSize < 0) ? 1 : -1))<=0); }
        bool hitProfit() { return (ProfitSize>0) && (ProfitTarget>0) && (Profit > ProfitTarget) && (PosSize == EntrySize); }
        void getStop() { if (Stop!=0) sPrice = AvgPrice - (stop * ((PosSize>0) ? 1 : -1)); }
        bool Enter()
        {
            Side = true;
            if (this.EnterLong()) return true;
            Side = false;
            if (this.EnterShort()) return true;
            return false;
        }
        BoxInfo _boxinfo = new BoxInfo();
        public BoxInfo Info { get { return _boxinfo; } }
        protected override int Read(Tick t,BarList barlist,BoxInfo bi)
        {
            this.tick = t; // save tick to member for child classes
            this.bl = barlist; // save bars for same purpose
            _boxinfo = bi;

            int adjust = 0;
            if (newTrade()) getStop(); 
            else if (PosSize != 0) // if we have entry price, check exits
            {
                if (!IgnoreTick()) checkMoveStop();
                if (!IgnoreTick() && this.Exit()) adjust = PosSize * -1;
                else if (this.hitProfit()) adjust = (PosSize > 0) ? ProfitSize : ProfitSize * -1;
                else if (this.hitStop()) adjust = PosSize * -1;
            }
            else if (IgnoreTick()) adjust = 0;
            else if (this.Enter()) // if haven't entered, check entry criteria
                adjust = EntrySize * (Side ? 1 : -1);
            return adjust;
        }
    }
}
