﻿using System;
using System.Collections.Generic;
using TradeLink.API;

namespace TradeLink.Common
{
    /// <summary>
    /// route your orders through this component to paper trade with a live data feed
    /// </summary>
    public class PapertradeTracker
    {
        IdTracker _idt;
        public PapertradeTracker() : this(new IdTracker()) { }
        public PapertradeTracker(IdTracker idt)
        {
            _idt = idt;
        }
        bool _usebidask = true;
        /// <summary>
        /// whether to use bid ask for fills, otherwise last is used.
        /// </summary>
        public bool UseBidAskFills { get { return _usebidask; } set { _usebidask = value; } }
        /// <summary>
        /// fill acknowledged, order filled.
        /// </summary>
        public event FillDelegate GotFillEvent;
        /// <summary>
        /// order acknowledgement, order placed.
        /// </summary>
        public event OrderDelegate GotOrderEvent;
        /// <summary>
        /// cancel acknowledgement, order is canceled
        /// </summary>
        public event LongDelegate GotCancelEvent;
        /// <summary>
        /// copy of the cancel request
        /// </summary>
        public event LongDelegate SendCancelEvent;

        TickTracker _kt = new TickTracker();
        /// <summary>
        /// pass data feed through to use for fills
        /// </summary>
        /// <param name="k"></param>
        public void newTick(Tick k)
        {
            _kt.addindex(k.symbol);
            _kt.newTick(k);
            process();
        }

        Queue<Order> aq = new Queue<Order>(100);
        Queue<long> can = new Queue<long>(100);

        /// <summary>
        /// send paper trade orders
        /// </summary>
        /// <param name="o"></param>
        public void sendorder(Order o)
        {
            if (o.id == 0)
                o.id = _idt.AssignId;
            lock (aq)
            {
                aq.Enqueue(o);
            }
            // ack the order
            if (GotOrderEvent != null)
                GotOrderEvent(o);
            process();
        }

        void process()
        {
            Order[] orders;
            long[] cancels;
            lock (aq)
            {
                // get copy of current orders
                 orders = aq.ToArray();
                //get current cancels
                 cancels = can.ToArray();
            }
            // get ready for unfilled orders
            List<Order> unfilled = new List<Order>();
            // try to fill every order
            for (int i = 0; i < orders.Length; i++)
            {
                // get order
                Order o = orders[i];
                // check for a cancel, if so remove cancel, acknowledge and continue
                int cidx = gotcancel(o.id, cancels);
                if (cidx >= 0)
                {
                    cancels[cidx] = 0;
                    if (GotCancelEvent != null)
                        GotCancelEvent(o.id);
                    continue;
                }

                // try to fill it
                bool filled = o.Fill(_kt[o.symbol], UseBidAskFills, false);
                // if it doesn't fill, add it back and continue
                if (!filled)
                {
                    unfilled.Add(o);
                    continue;
                }
                // check for partial fill
                Trade fill = (Trade)o;
                bool partial = fill.UnsignedSize != o.UnsignedSize;
                // if partial fill, update original order and add it back
                o.size = (o.UnsignedSize - fill.UnsignedSize) * (o.side ? 1 : -1);
                unfilled.Add(o);
                // acknowledge the fill
                if (GotFillEvent != null)
                    GotFillEvent(fill);
            }
            lock (aq)
            {
                // clear queues
                aq.Clear();
                // add orders back
                for (int i = 0; i < unfilled.Count; i++)
                    aq.Enqueue(unfilled[i]);
                // add cancels back
                can.Clear();
                for (int i = 0; i < cancels.Length; i++)
                    if (cancels[i] != 0)
                        can.Enqueue(cancels[i]);
            }
        }

        int gotcancel(long id, long[] ids)
        {
            if (id == 0) return -1;
            for (int i = 0; i < ids.Length; i++)
                if (id == ids[i]) return i;
            return -1;
        }

        /// <summary>
        /// cancel papertrade orders
        /// </summary>
        /// <param name="id"></param>
        public void sendcancel(long id)
        {
            if (id == 0) return;
            lock (aq)
            {
                can.Enqueue(id);
            }
            if (SendCancelEvent != null)
                SendCancelEvent(id);
            process();
        }
    }
}
