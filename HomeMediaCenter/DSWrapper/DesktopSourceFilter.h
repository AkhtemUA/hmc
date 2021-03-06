#pragma once

#if !defined(DESKTOPSOURCEFILTER_DSWRAPPER_INCLUDED)
#define DESKTOPSOURCEFILTER_DSWRAPPER_INCLUDED

#include "DesktopSourcePin.h"
#include "DesktopSourceAudioPin.h"

private class DesktopSourceFilter : public CSource, public IAMFilterMiscFlags
{
	public:		DesktopSourceFilter(LPUNKNOWN pUnk, HRESULT * phr, UINT32 fps);
				~DesktopSourceFilter(void);

				DECLARE_IUNKNOWN
				STDMETHODIMP NonDelegatingQueryInterface(REFIID riid, void ** ppv);

				void SyncPins(DWORD index);
				//IAMFilterMiscFlags
				ULONG STDMETHODCALLTYPE GetMiscFlags(void);

				//Maximalna dlzka cakania na synchronizaciu
				static const DWORD WaitToSync = 10000;

	private:	DesktopSourcePin * m_sourcePin;
				DesktopSourceAudioPin * m_sourceAudioPin;
				CCritSec m_syncSection;
				HANDLE m_syncEvent[2];
				BOOL m_signaled[2];
};

#endif //DESKTOPSOURCEFILTER_DSWRAPPER_INCLUDED
