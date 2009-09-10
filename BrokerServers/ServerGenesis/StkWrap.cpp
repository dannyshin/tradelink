
#include "stdafx.h"
#include "testapi.h"
#include "TestAPIDlg.h"
#include "StkWrap.h"

using namespace TradeLibFast;

StkWrap::StkWrap(GTSession &session, LPCSTR pszStock)
	: GTStock(session, pszStock)
{

	tl = NULL;

	time_t now;
	time(&now);
	CTime ct(now);
	date = (ct.GetYear()*10000) + (ct.GetMonth()*100) + ct.GetDay();


#ifdef UDP_QUOTE
	m_level2.m_bid.m_nMaxLevels = 10;
	m_level2.m_ask.m_nMaxLevels = 10;

	m_prints.SetMinMax(128, 256);
#endif
}

StkWrap::~StkWrap()
{
	tl = NULL;
}

int StkWrap::OnExecMsgErrMsg(const GTErrMsg &err)
{
	CString m;
	m.Format("error: %s %s %l",err.szStock,err.szText,err.dwOrderSeqNo);
	tl->D(m);
	return GTStock::OnExecMsgErrMsg(err);
}

int StkWrap::OnExecMsgCancel(const GTCancel &cancel)
{
	tl->SrvGotCancel((int)cancel.dwTicketNo);


	return GTStock::OnExecMsgCancel(cancel);
}

int StkWrap::OnGotQuoteLevel1(GTLevel1 *pRcd)
{
#ifdef UDP_QUOTE
	return GTStock::OnGotQuoteLevel1(pRcd);
#else
	CString strStock;

	if(strStock != pRcd->szStock)
		return GTStock::OnGotQuoteLevel1(pRcd);

	return GTStock::OnGotQuoteLevel1(pRcd);
#endif	
}
#define SHAREUNITS 100
int StkWrap::OnGotLevel2Record(GTLevel2 *pRcd)
{
#ifdef UDP_QUOTE
	return GTStock::OnGotLevel2Record(pRcd);
#else
	CString strStock;

	int rc = GTStock::OnGotLevel2Record(pRcd);
	int depth = (int)(pRcd->dblLevelPrice * m_session.m_setting.m_nLevelRate);

	if (depth>tl->_depth) return rc;

	char chSide = pRcd->chSide;


	TLTick k;
	k.sym = CString(pRcd->szStock);
	k.depth = depth;
	k.time = pRcd->gtime.dwTime/100;
	k.date = date;
	
	if (pRcd->chSide=='B')
	{
		k.bid = pRcd->dblPrice;
		k.bs = pRcd->dwShares / SHAREUNITS;
		k.be = CAST_MMID_TEXT(pRcd->mmid);
	}
	else
	{
		k.ask = pRcd->dblPrice;
		k.os = pRcd->dwShares / SHAREUNITS;
		k.oe = CAST_MMID_TEXT(pRcd->mmid);

	}
	tl->SrvGotTick(k);

	return rc;
#endif
}





int StkWrap::OnTick()
{
	GTStock::OnTick();

	return 0;
}

int StkWrap::OnGotQuotePrint(GTPrint *pRcd)
{
	TLTick k;
	k.sym = CString(pRcd->szStock);
	k.trade = pRcd->dblPrice;
	k.size = (int)pRcd->dwShares;
	k.ex = CString(pRcd->chSource);
	k.time = pRcd->gtime.dwTime/100;
	k.date = date;

	tl->SrvGotTick(k);
	
	return GTStock::OnGotQuotePrint(pRcd);
}

int StkWrap::OnExecMsgSending(const GTSending &pRcd)
{

	return GTStock::OnExecMsgSending(pRcd);
}

CString tifstring(int TIF)
{
	switch (TIF)
	{
		case TIF_DAY : return CString("DAY"); break;
		case TIF_MGTC : return CString("GTC"); break;
		case TIF_IOC : return CString("IOC"); break;
	}
	return CString("DAY");
}

int StkWrap::OnBestAskPriceChanged()
{
	TLTick k;
	k.sym = CString(m_szStock);
	k.ask = m_level1.dblAskPrice;
	k.os = m_level1.nAskSize;
	k.oe = CString(m_level1.locAskExchangeCode);
	k.date = date;
	k.time = m_level1.gtime.dwTime/100;

	tl->SrvGotTick(k);
	return OK;
}

int StkWrap::OnBestBidPriceChanged()
{
	TLTick k;
	k.sym = CString(m_szStock);
	k.ask = m_level1.dblBidPrice;
	k.os = m_level1.nBidSize;
	k.oe = CString(m_level1.locBidExchangeCode);
	k.date = date;
	k.time = m_level1.gtime.dwTime/100;

	tl->SrvGotTick(k);

	return OK;


}


int StkWrap::OnExecMsgPending(const GTPending &pRcd)
{
	TLOrder o;
	o.symbol = CString(pRcd.szStock);
	o.side = pRcd.chPendSide == 'B';
	o.size = pRcd.nEntryShares;
	o.TIF = tifstring(pRcd.nEntryTIF);
	o.time = pRcd.nPendTime;
	o.date = pRcd.nEntryDate;
	o.price = pRcd.dblEntryPrice;
	o.stop = pRcd.dblPendStopLimitPrice;
	o.id = (uint)pRcd.dwTicketNo;
	o.account = CString(pRcd.szAccountID);
	o.exchange = CAST_MMID_TEXT(pRcd.place);

	tl->SrvGotOrder(o);

	return GTStock::OnExecMsgPending(pRcd);
}

int StkWrap::OnExecMsgTrade(const GTTrade &pRcd)
{
	TLTrade t;
	t.symbol = CString(pRcd.szStock);
	t.account = CString(pRcd.szAccountID);
	t.id = (uint)pRcd.dwTicketNo;
	t.xdate = pRcd.nExecDate;
	t.xtime = pRcd.nExecTime;
	t.xprice = pRcd.dblExecPrice;
	t.xsize = pRcd.nExecShares;
	t.exchange = CAST_MMID_TEXT(pRcd.execfirm);

	tl->SrvGotFill(t);

	return GTStock::OnExecMsgTrade(pRcd);
}

int StkWrap::OnExecMsgOpenPosition(const GTOpenPosition &open)
{
	return GTStock::OnExecMsgOpenPosition(open);
}