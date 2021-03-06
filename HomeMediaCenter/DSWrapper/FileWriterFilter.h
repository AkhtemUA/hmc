#pragma once

#if !defined(FILEWRITERFILTER_DSWRAPPER_INCLUDED)
#define FILEWRITERFILTER_DSWRAPPER_INCLUDED

#include <strmif.h>

class FileWriterPin;

private class FileWriterFilter : public CBaseFilter, public IAMFilterMiscFlags
{
	friend class FileWriterPin;

	public:		FileWriterFilter(LPUNKNOWN pUnk, HRESULT * phr, System::IO::Stream^ outputStream, GUID inputSubtype, DWORD inactivityTimeout);
				virtual ~FileWriterFilter(void);

				DECLARE_IUNKNOWN
				STDMETHODIMP NonDelegatingQueryInterface(REFIID riid, void ** ppv);

				//CBaseFilter
				int GetPinCount(void);
				CBasePin * GetPin(int n);

				//IAMFilterMiscFlags
				ULONG STDMETHODCALLTYPE GetMiscFlags(void);

	private:	CCritSec m_critSection;
				CBasePin * m_writerPin;
				CPosPassThru * m_passThru;
};

private class FileWriterPin : public CBaseInputPin, public IStream
{
	public:		FileWriterPin(TCHAR * pObjectName, CBaseFilter * pFilter, CCritSec * pLock, HRESULT * phr, 
					LPCWSTR pName, System::IO::Stream^ outputStream, GUID inputSubtype, DWORD inactivityTimeout);
				virtual ~FileWriterPin(void);

				DECLARE_IUNKNOWN
				STDMETHODIMP NonDelegatingQueryInterface(REFIID riid, void ** ppv);
				
				//CBasePin
				HRESULT CheckMediaType(const CMediaType * pmt);
				HRESULT Active(void);
				HRESULT Inactive(void);

				//IPin
				STDMETHODIMP BeginFlush(void);
				STDMETHODIMP EndFlush(void);
				STDMETHODIMP EndOfStream(void);

				//IMemInputPin
				STDMETHODIMP Receive(IMediaSample * pSample);
				STDMETHODIMP GetAllocator(IMemAllocator ** ppAllocator);

				//IStream
				STDMETHODIMP Read(void * pv, ULONG cb, ULONG * pcbRead);
				STDMETHODIMP Write(void const * pv, ULONG cb, ULONG * pcbWritten);
				STDMETHODIMP Seek(LARGE_INTEGER dlibMove, DWORD dwOrigin, ULARGE_INTEGER * plibNewPosition);
				STDMETHODIMP SetSize(ULARGE_INTEGER libNewSize);
				STDMETHODIMP CopyTo(IStream * pstm, ULARGE_INTEGER cb, ULARGE_INTEGER * pcbRead, ULARGE_INTEGER * pcbWritten);
				STDMETHODIMP Commit(DWORD grfCommitFlags);
				STDMETHODIMP Revert();
				STDMETHODIMP LockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType);
				STDMETHODIMP UnlockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType);
				STDMETHODIMP Stat(STATSTG * pstatstg, DWORD grfStatFlag);
				STDMETHODIMP Clone(IStream ** ppstm);

				ref class TimerCallbackClass
				{
					bool m_started;
					FileWriterPin * m_pin;
					System::Timers::Timer ^ m_timer;

					public:	TimerCallbackClass(FileWriterPin * pin);
							~TimerCallbackClass();
							void Start(void);
							void Stop(void);
							void Callback(System::Object ^ source, System::Timers::ElapsedEventArgs ^ e);
 				};

	private:	gcroot<TimerCallbackClass^> m_actTimerCallback;
				gcroot<System::IO::Stream^> m_outputStream;
				GUID m_inputSubtype;
				LONGLONG m_position;
				CCritSec m_readWriteSect;
				DWORD m_lastRequest;
				DWORD m_inactivityTimeout;
};

private class NullWriterPin : public CBaseInputPin
{
	public:		NullWriterPin(TCHAR * pObjectName, CBaseFilter * pFilter, CCritSec * pLock, HRESULT * phr, 
					LPCWSTR pName);
				virtual ~NullWriterPin(void);

				DECLARE_IUNKNOWN
				STDMETHODIMP NonDelegatingQueryInterface(REFIID riid, void ** ppv);
				
				//CBasePin
				HRESULT CheckMediaType(const CMediaType * pmt);

				//IPin
				STDMETHODIMP EndOfStream(void);

				//IMemInputPin
				STDMETHODIMP Receive(IMediaSample * pSample);
				STDMETHODIMP GetAllocator(IMemAllocator ** ppAllocator);
};

#endif //FILEWRITERFILTER_DSWRAPPER_INCLUDED

