

namespace TradeLib
{
    public class MarketOrder : Order
    {
        public MarketOrder(string sym, int size) : base(sym, size) { }
        public MarketOrder(string sym, bool side, int size) : base(sym, side, System.Math.Abs(size)) { }
        public MarketOrder(string sym, bool side, int size, string comment) : base(sym, side, System.Math.Abs(size), comment) { }
    }
    public class BuyMarket : Order
    {
        public BuyMarket(string sym, int size) : base(sym, true, System.Math.Abs(size)) { }
        public BuyMarket(string sym, int size, string comment) : base(sym, true, System.Math.Abs(size), comment) { }
    }
    public class SellMarket : Order
    {
        public SellMarket(string sym, int size) : base(sym, false, System.Math.Abs(size)) { }
        public SellMarket(string sym, int size, string comment) : base(sym, false, System.Math.Abs(size), comment) { }
    }
}
