using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using TradeLink.API;
using TradeLink.Common;

namespace TradeLink.AppKit
{
    /// <summary>
    /// Used to pass changes to barlists
    /// </summary>
    public delegate void BarListUpdated(BarListImpl newbl);

    /// <summary>
    /// A generic charting form that plots BarList objects
    /// </summary>
    public partial class ChartControl : UserControl
    {

        //public event SecurityDelegate FetchStock;
        BarList bl = null;
        public BarList Bars 
        { 
            get { return bl; } 
            set { NewBarList(value); } 
        }
        bool _alwaysupdate = false;
        /// <summary>
        /// if set, control will autorefresh with each tick.
        /// otherwise, refresh must be called manually.
        /// manual is recommended during rapid updates, as the chart may flash otherwise.
        /// </summary>
        public bool AutoUpdate { get { return _alwaysupdate; } set { _alwaysupdate = value; } }
        /// <summary>
        /// create bars from ticks
        /// </summary>
        /// <param name="k"></param>
        public void newTick(Tick k)
        {
            if (bl == null)
            {
                Symbol = k.symbol;
                bl = new BarListImpl(k.symbol);
            }
            bl.newTick(k);
            if (k.isTrade)
            {
                if (k.trade > highesth)
                    highesth = k.trade;
                if (k.trade < lowestl)
                    lowestl = k.trade;
            }
            barc = bl.Count;
            if (_alwaysupdate)
                refresh();
        }
        /// <summary>
        /// create bars from points
        /// </summary>
        /// <param name="p"></param>
        /// <param name="time"></param>
        /// <param name="date"></param>
        /// <param name="size"></param>
        public void newPoint(decimal p, int time, int date, int size)
        {
            if (bl == null)
            {
                Symbol = string.Empty;
                highesth = SMALLVAL;
                bl = new BarListImpl(BarInterval.FiveMin, string.Empty);
            }
            bl.newPoint(p, time, date, size);
            if (p!=0)
            {
                if (p > highesth)
                    highesth = p;
                if (p < lowestl)
                    lowestl = p;
            }
            barc = bl.Count;
            if (_alwaysupdate)
                refresh();
        }

        /// <summary>
        /// force a manual refresh of the grid
        /// </summary>
        public void refresh()
        {
            if (InvokeRequired)
                Invoke(new VoidDelegate(refresh));
            else
            {
                Invalidate(true);
            }
        }

        /// <summary>
        /// reset the chart and underlying data structures
        /// </summary>
        public void Reset()
        {
            if (bl!=null)
                bl.Reset();
            highesth = 0;
            lowestl = BIGVAL;
            barc = 0;
            refresh();
        }
        string sym = string.Empty;
        public string Symbol { get { return sym; } set { sym = value; Text = Title; } }
        Graphics g = null;
        string mlabel = null;
        decimal highesth = 0;
        const decimal SMALLVAL = -100000000000000;
        const decimal BIGVAL = 10000000000000000000;
        decimal lowestl = BIGVAL;
        int barc = 0;
        Rectangle r;
        int border = 60;
        int hborder = 60;
        decimal pixperbar = 0;
        decimal  pixperdollar = 0;



        
        public ChartControl() : this(null,false) { }
        public ChartControl(BarList b) : this(b, false) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Chart"/> class.
        /// </summary>
        /// <param name="b">The barlist.</param>
        /// <param name="allowtype">if set to <c>true</c> [allowtype] will allow typing/changing of new symbols on the chart window.</param>
        public ChartControl(BarList b,bool allowtype)
        {
            InitializeComponent();
            Paint += new PaintEventHandler(Chart_Paint);
            MouseDoubleClick += new MouseEventHandler(ChartControl_MouseDoubleClick);
            MouseWheel +=new MouseEventHandler(Chart_MouseUp);
            if (b != null)
            {
                bl = b;
                Symbol = b.Symbol;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            try
            {
                if (DisplayCursor && (bl != null))
                {
                    ChartControl f = this;
                    g = CreateGraphics();
                    float size = g.MeasureString(highesth.ToString(), f.Font).Width + g.MeasureString("255000 ", f.Font).Width;
                    g.FillRectangle(new SolidBrush(f.BackColor), r.Width - size, r.Height - f.Font.Height, size, f.Font.Height);
                    _curbar = getBar(MousePosition.X);
                    _curprice = getPrice(MousePosition.Y);
                    size = g.MeasureString(highesth.ToString(), f.Font).Width + g.MeasureString("255000 ", f.Font).Width;
                    string time = _curbar == 0 ? string.Empty : bl[_curbar].Bartime.ToString();
                    string price = _curprice == 0 ? string.Empty : _curprice.ToString("F2");
                    g.DrawString(time + " " + price, f.Font, new SolidBrush(Color.Black), r.Width - size, r.Height - f.Font.Height);

                }
            }
            catch { }
            base.OnMouseMove(e);
        }

        public event DebugDelegate SendDebug;
        internal void debug(string msg)
        {
            if (SendDebug != null)
                SendDebug(msg);
        }

        void ChartControl_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string fn = string.Empty;
            try
            {
                fn = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + Symbol + bl.Date()[bl.Last]+ ".png";
                ScreenCapture sc = new ScreenCapture();
                sc.CaptureWindowToFile(Handle, fn, System.Drawing.Imaging.ImageFormat.Png);
            }
            catch (Exception ex)
            {
                debug("Error writing: " + fn);
                debug(ex.Message + ex.StackTrace);
            }


        }

        public void NewBarList(BarList barlist)
        {
            if ((barlist != null) && (barlist.isValid))
                Symbol = barlist.Symbol;
            if ((barlist == null) || (barlist.Intervals.Length==0) || (barlist.Count==0))
            {
                return;
            }
            bl = barlist;
            highesth = Calc.HH(bl);
            lowestl = Calc.LL(bl);
            barc = bl.Count;
            refresh();
        }


        // as we add bars and barindex gets higher, the xcoordinate moves right and gets higher
        int getX(int bar) { return (int)(bar * pixperbar) + (border/3); } 
        // as price goes up and pricemagnitude goes higher, the y coordinate moves up and goes lower
        int getY(decimal price) { return (int)(hborder+((highesth-price) * pixperdollar)); }

        int getBar(int X) 
        {
            if (bl == null) return 0;
            int b = (int)((X - (border / 3)) / pixperbar);
            if ((b < 0) || (b >= bl.Count)) return 0;
            return b;
        }
        decimal getPrice(int Y) 
        {
            if (bl == null) return 0;
            decimal p = (((Y - hborder)/pixperdollar)-highesth)*-1;
            if ((p > highesth) || (p < lowestl)) return 0;
            return p;
        }

        /// <summary>
        /// Gets the title of this chart.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get { if (bl == null) return Symbol; return Symbol + " " + Enum.GetName(typeof(BarInterval), bl.DefaultInterval).ToString(); } }
        void Chart_Paint(object sender, PaintEventArgs e)
        {
            // get form
            ChartControl f = (ChartControl)sender;
            // get graphics
            g = e.Graphics;
            // get window
            r = ClientRectangle;
            // get title
            Text = Title;
            // check for data
            if ((bl == null) || (bl.Intervals.Length==0) || (bl.Count == 0))
            {
                g.DrawString("Unknown symbol, or no data.", new Font(FontFamily.GenericSerif, 14, FontStyle.Bold), Brushes.Red, new PointF(r.Width / 3, r.Height / 2));
                return;
            }
            border = (int)(g.MeasureString(bl.High()[0].ToString(), f.Font).Width);
            // setup to draw
            Pen p = new Pen(Color.Black);
            g.Clear(f.BackColor);
            // get number of pixels available for each bar, based on screensize and barcount
            pixperbar = (((decimal)r.Width - (decimal)border - ((decimal)border/3)) / (decimal)barc);
            // pixels for each time stamp
            const int pixperbarlabel = 60;
            // number of labels we have room to draw
            int numbarlabels = (int)((double)(r.Width - border - ((double)border / 3)) / pixperbarlabel);
            // draw a label every so many bars (assume every bar to star)
            int labeleveryX = 1;
            // if there's more bars than space
            if (barc>numbarlabels)
                labeleveryX = (int)Math.Round(((double)barc / numbarlabels));
            // get dollar range for chart
            decimal range = (highesth - lowestl);
            // get pixels available for each dollar of movement
            pixperdollar = range == 0 ? 0 : (((decimal)r.Height - (decimal)hborder * 2) / range);


            Color fgcol = (f.BackColor == Color.Black) ? Color.White : Color.Black;

            // x-axis
            g.DrawLine(new Pen(fgcol),(int)(border/3), r.Height-hborder, r.Width - border, r.Height - hborder);
            // y-axis
            g.DrawLine(new Pen(fgcol), r.Width - border, r.Y + hborder, r.Width-border, r.Height - hborder);

            const int minxlabelwidth = 30;

            int lastlabelcoord = -500;

            for (int i = 0; i < barc; i++)
            {
                // get bar color
                Color bcolor = (bl.Close()[i] > bl.Open()[i]) ? Color.Green : Color.Red;
                p = new Pen(bcolor);
                try
                {
                    // draw high/low bar
                    g.DrawLine(p, getX(i), getY(bl.Low()[i]), getX(i), getY(bl.High()[i]));
                    // draw open bar
                    g.DrawLine(p, getX(i), getY(bl.Open()[i]), getX(i) - (int)(pixperbar / 3), getY(bl.Open()[i]));
                    // draw close bar
                    g.DrawLine(p, getX(i), getY(bl.Close()[i]), getX(i) + (int)(pixperbar / 3), getY(bl.Close()[i]));
                    // draw time labels (time @30min and date@noon)

                    // if interval is intra-day
                    if (bl.DefaultInterval != BarInterval.Day)
                    {
                        // every 6 bars draw the bartime
                        if ((i % labeleveryX) == 0) g.DrawString((bl.Time()[i]/100).ToString(), f.Font, new SolidBrush(fgcol), getX(i), r.Height - (f.Font.GetHeight() * 3));
                        // if it's noon, draw the date
                        if (bl.Time()[i]== 120000) g.DrawString(bl.Date()[i].ToString(), f.Font, new SolidBrush(fgcol), getX(i), r.Height - (float)(f.Font.GetHeight() * 1.5));
                    }
                    else // otherwise it's daily data
                    {
                        // get date
                        int[] date = Calc.Date(bl.Date()[i]);
                        int[] lastbardate = date;
                        // get previous bar date if we have one
                        if ((i - 1) > 0) lastbardate = Calc.Date(bl.Date()[i]);
                        // if we have room since last time we drew the year
                        if ((getX(lastlabelcoord) + minxlabelwidth) <= getX(i))
                        {
                            // get coordinate for present days label
                            lastlabelcoord = i;
                            // draw day
                            g.DrawString(date[2].ToString(), f.Font, new SolidBrush(fgcol), getX(i), r.Height - (f.Font.GetHeight() * 3));
                        }
                        // if it's first bar or a new month
                        if ((i == 0) || (lastbardate[1] != date[1]))
                        {
                            // get the month
                            string ds = date[1].ToString();
                            // if it sfirst bar or the year has changed, add year to month
                            if ((i == 0) || (lastbardate[0] != date[0])) ds += '/' + date[0].ToString();
                            // draw the month
                            g.DrawString(ds, f.Font, new SolidBrush(fgcol), getX(i), r.Height - (float)(f.Font.GetHeight() * 1.5));
                        }
                    }
                }
                catch (OverflowException) { }
            }

            // DRAW YLABELS
            // max number of even intervaled ylabels possible on yaxis
            int numlabels = (int)((r.Height - hborder) / (f.Font.GetHeight()*1.5));
            // nearest price units giving "pretty" even intervaled ylabels
            decimal priceunits = NearestPrettyPriceUnits(highesth - lowestl, numlabels);
            // starting price point from low end of range, including lowest low in barlist
            decimal lowstart = lowestl - ((lowestl * 100) % (priceunits * 100)) / 100;
            // original "non-pretty" price units calc
            //decimal priceunits = (highesth-lowestl)/numlabels;
            Pen priceline = new Pen(Color.BlueViolet);
            priceline.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

            for (decimal i = 0;i<=numlabels; i++) 
            {
                decimal price = lowstart+(i*priceunits);
                g.DrawString(price.ToString("C"),f.Font,new SolidBrush(fgcol), r.Width-border,getY(price)-f.Font.GetHeight());
                g.DrawLine(priceline, border/3, getY(price), r.Width - border, getY(price));
            }
            if (DisplayInterval && (bl!=null))
            {
                g.DrawString(bl.DefaultInterval.ToString(), f.Font, new SolidBrush(Color.Black), 3, 3);
            }

            DrawLabels();
        }

        /// <summary>
        /// draws text label on a chart.
        /// if price is less than zero, all labels are cleared.
        /// </summary>
        /// <param name="price"></param>
        /// <param name="bar"></param>
        /// <param name="label"></param>
        public void DrawChartLabel(decimal price, int time, string label, Color color)
        {
            if (price < 0)
            {
                _colpoints.Remove(color);
                _collineend.Remove(color);
                return;
            }
            List<Label> tmp;
            if (!_colpoints.TryGetValue(color, out tmp))
            {
                tmp = new List<Label>();
                _colpoints.Add(color, tmp);
                _collineend.Add(color,new List<int>());
            }
            _colpoints[color].Add(new Label(time, price, label, color));
            if (_alwaysupdate)
                refresh();
        }

        private decimal NearestPrettyPriceUnits(decimal pricerange,int maxlabels)
        {
            decimal[] prettyunits = new decimal[] { .01m, .02m, .04m, .05m, .1m, .2m, .25m, .4m, .5m, 1,2,5,10,25,50,100,1000,2000,4000,5000,10000 };
            for (int i = prettyunits.Length-1; i>=0; i--)
            {
                int numprettylabels = (int)(pricerange / prettyunits[i]);
                if (numprettylabels < maxlabels) continue;
                else return (i == (prettyunits.Length - 1)) ? prettyunits[prettyunits.Length - 1] : prettyunits[i + 1];
            }
            return prettyunits[0];
        }

        private void Chart_Resize(object sender, EventArgs e)
        {
            refresh();
        }

        struct Label
        {
            public bool isLineEnd { get { return (Text == null) || (Text == string.Empty); } }
            public int Time;
            public decimal Price;
            public string Text;
            public Color Color;
            public Label(int bar, decimal price, string text, Color color)
            {
                Time = bar;
                Price = price;
                Text = text;
                Color = color;
            }
        }
        Dictionary<Color, List<Label>> _colpoints = new Dictionary<Color, List<Label>>();
        Dictionary<Color, List<int>> _collineend = new Dictionary<Color,List<int>>();
        private void DrawLabels()
        {
            Graphics gd = CreateGraphics();
            Font font = new Font(FontFamily.GenericSerif, 8,FontStyle.Regular);
            foreach (Color c in _colpoints.Keys)
            {
                List<Label> points = _colpoints[c];
                for (int i = 0; i < points.Count; i++)
                {
                    // draw lines
                    if (points[i].isLineEnd)
                    {
                        _collineend[c].Add(i);
                        // can't draw from first point
                        if (i == 0) continue;
                        // draw from previous point
                        gd.DrawLine(new Pen(c), 
                            getX(BarListImpl.GetNearestIntraBar(bl,points[_collineend[c][_collineend[c].Count - 2]].Time,bl.DefaultInterval)), 
                            getY(points[_collineend[c][_collineend[c].Count - 2]].Price), 
                            getX(BarListImpl.GetNearestIntraBar(bl,points[i].Time,bl.DefaultInterval)), 
                            getY(points[i].Price));

                    }
                    else // draw labels
                        gd.DrawString(points[i].Text, font, new SolidBrush(c), getX(BarListImpl.GetNearestIntraBar(bl,points[i].Time,bl.DefaultInterval)), getY(points[i].Price));
                }
            }



        }

        Color _mancolor = Color.Turquoise;
        /// <summary>
        /// color used for manual chart drawings
        /// </summary>
        public Color ManualColor { get { return _mancolor; } set { _mancolor = value; } }

        private void Chart_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (mlabel == null) return;
                DrawChartLabel(getPrice(e.Y), bl.Time()[getBar(e.X)],mlabel,ManualColor);
            }
            else if (e.Button == MouseButtons.Middle) 
            {

                DrawChartLabel(-1, 0, string.Empty, ManualColor);
            }
            refresh();
        }

        private void redToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mlabel = "2";
        }

        private void yellowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mlabel = "1";
        }

        private void greenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mlabel = "0";
        }

        private void blackToolStripMenuItem_Click(object sender, EventArgs e)
        { //sell
            mlabel = "S";
        }

        private void blueToolStripMenuItem_Click(object sender, EventArgs e)
        {// buy
            mlabel = "B";
        }

        private void offToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mlabel = null;
        }

        private void clearAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DrawChartLabel(-1, 0, string.Empty, ManualColor);
            refresh();

        }
        bool _dispint = true;
        public bool DisplayInterval { get { return _dispint; } set { _dispint = value; } }

        bool _dispcur = true;
        public bool DisplayCursor { get { return _dispcur; } set { _dispcur = value; } }

        int _curbar = 0;
        decimal _curprice = 0;

        public void Chart_MouseUp(object sender, MouseEventArgs e)
        {
            if (bl == null) return;
            if (e.Delta == 0) return;
            BarInterval old = bl.DefaultInterval;
            BarInterval [] v = bl.Intervals;
            int biord = 0;
            for (int i = 0;i<v.Length;i++) if (old==v[i]) biord = i;

            if (e.Delta > 0)
            {
                bl.DefaultInterval = (biord + 1 < v.Length) ? v[biord + 1] : v[0];
            }
            else
            {
                bl.DefaultInterval = (biord - 1 < 0) ? v[v.Length - 1] : v[biord - 1];
            }
            if ((bl.DefaultInterval != old) && bl.Has(1,bl.DefaultInterval)) 
                NewBarList(this.bl);
        }

        private void _custom_Click(object sender, EventArgs e)
        {
            mlabel = Microsoft.VisualBasic.Interaction.InputBox("Enter label: ", "Custom chart label", "?", Location.X + 5, Location.Y + 5);
        }

        private void _point_Click(object sender, EventArgs e)
        {
            mlabel = string.Empty;
        }
    }
    public class TextLabel
    {
        public TextLabel(string label, int xCoord, int yCoord) { tlabel = label; x = xCoord; y = yCoord; }
        string tlabel = "";
        int x = 0;
        int y = 0;
        public string Label { get { return tlabel; } }
        public int X { get { return x; } }
        public int Y { get { return y; } }
    }
}