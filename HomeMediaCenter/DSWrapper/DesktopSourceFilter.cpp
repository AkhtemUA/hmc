#include "StdAfx.h"
#include "DesktopSourceFilter.h"

DesktopSourceFilter::DesktopSourceFilter(LPUNKNOWN pUnk, HRESULT * phr, UINT32 fps) 
	: CSource(L"DesktopSourceFilter", pUnk, CLSID_NULL), m_sourcePin(NULL), m_sourceAudioPin(NULL)
{
	this->m_signaled[0] = FALSE;
	this->m_signaled[1] = FALSE;

	this->m_syncEvent[0] = CreateEvent(NULL, TRUE, FALSE /*nesignalovy stav*/, NULL); 
	this->m_syncEvent[1] = CreateEvent(NULL, TRUE, FALSE /*nesignalovy stav*/, NULL); 
	if (this->m_syncEvent[0] == NULL || this->m_syncEvent[1] == NULL)
	{
		*phr = E_OUTOFMEMORY;
		return;
	}

	this->m_sourcePin = new DesktopSourcePin(L"DesktopSourcePin", phr, this, L"Out", fps);
	if (this->m_sourcePin == NULL)
	{
		*phr = E_OUTOFMEMORY;
		return;
	}

	if (*phr != S_OK)
		return;

	this->m_sourceAudioPin = new DesktopSourceAudioPin(L"DesktopSourceAudioPin", phr, this, L"Out");
	if (this->m_sourceAudioPin == NULL)
	{
		*phr = E_OUTOFMEMORY;
		return;
	}

	//Nie je potrebne davat AddRef objektu DesktopSourcePin a DesktopSourceAudioPin, vsetko sa deleguje na DesktopSourceFilter
}

STDMETHODIMP DesktopSourceFilter::NonDelegatingQueryInterface(REFIID riid, void ** ppv)
{
	if (riid == IID_IAMFilterMiscFlags)
		return GetInterface((IAMFilterMiscFlags*) this, ppv);

	return CSource::NonDelegatingQueryInterface(riid, ppv);
}

DesktopSourceFilter::~DesktopSourceFilter(void)
{
	if (this->m_sourcePin != NULL)
		delete this->m_sourcePin;
	if (this->m_sourceAudioPin != NULL)
		delete this->m_sourceAudioPin;

	CloseHandle(this->m_syncEvent[0]);
	CloseHandle(this->m_syncEvent[1]);
}

void DesktopSourceFilter::SyncPins(DWORD index)
{
	if (this->m_sourcePin->IsConnected() && this->m_sourceAudioPin->IsConnected())
	{
		SetEvent(this->m_syncEvent[index]);

		//Caka, pokial audio aj video signalizuju, maximalne ale cas v konstante WaitToSync
		WaitForMultipleObjects(2, this->m_syncEvent, TRUE, WaitToSync);

		CAutoLock cAutoLock(&this->m_syncSection);

		this->m_signaled[index] = TRUE;
		if (this->m_signaled[0] && this->m_signaled[1])
		{
			//Ak audio aj video signalizovali, zmen do nesignaloveho stavu
			ResetEvent(this->m_syncEvent[0]);
			ResetEvent(this->m_syncEvent[1]);
			this->m_signaled[0] = FALSE;
			this->m_signaled[1] = FALSE;
		}
	}
}

//************ IAMFilterMiscFlags ************

ULONG STDMETHODCALLTYPE DesktopSourceFilter::GetMiscFlags(void)
{
	return AM_FILTER_MISC_FLAGS_IS_SOURCE;
}