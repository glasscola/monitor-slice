#include <windows.h>
#include <roapi.h>
#include <d3d11.h>
#include <dxgi1_2.h>
#include <windows.graphics.capture.interop.h>
#include <windows.graphics.directx.direct3d11.interop.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Graphics.Capture.h>
#include <winrt/Windows.Graphics.DirectX.h>
#include <winrt/Windows.Graphics.DirectX.Direct3D11.h>
#include <atomic>
#include <memory>
#include <mutex>
#include <thread>
#include <condition_variable>
using namespace winrt;
using namespace winrt::Windows::Graphics::Capture;
using namespace winrt::Windows::Graphics::DirectX;
using namespace winrt::Windows::Graphics::DirectX::Direct3D11;
static HMONITOR sharedMonitor=nullptr;static com_ptr<ID3D11Device> sharedDevice;static com_ptr<ID3D11DeviceContext> sharedContext;static std::mutex deviceGuard,contextGuard;
struct Capture:std::enable_shared_from_this<Capture>{
 com_ptr<ID3D11Device> device;com_ptr<ID3D11DeviceContext> context;com_ptr<IDXGISwapChain1> swap;com_ptr<ID3D11Texture2D> lastImage;
 Direct3D11CaptureFramePool pool{nullptr};GraphicsCaptureSession session{nullptr};event_token token{};
 std::thread worker;std::condition_variable wake;Direct3D11CaptureFrame latest{nullptr};
 std::mutex guard,localContextGuard;bool duplicationMode=false;std::atomic<bool> stopped{false};std::atomic<unsigned long long> frames{0},received{0},ageTicks{0};std::atomic<long> error{0};
 void openAnimation(HWND window){check_hresult(D3D11CreateDevice(nullptr,D3D_DRIVER_TYPE_HARDWARE,nullptr,D3D11_CREATE_DEVICE_BGRA_SUPPORT,nullptr,0,D3D11_SDK_VERSION,device.put(),nullptr,context.put()));auto dxgi=device.as<IDXGIDevice>();com_ptr<IDXGIAdapter> adapter;check_hresult(dxgi->GetAdapter(adapter.put()));com_ptr<IDXGIFactory2> factory;check_hresult(adapter->GetParent(__uuidof(IDXGIFactory2),factory.put_void()));RECT r{};GetClientRect(window,&r);DXGI_SWAP_CHAIN_DESC1 d{};d.Width=r.right;d.Height=r.bottom;d.Format=DXGI_FORMAT_B8G8R8A8_UNORM;d.SampleDesc.Count=1;d.BufferUsage=DXGI_USAGE_RENDER_TARGET_OUTPUT;d.BufferCount=2;d.SwapEffect=DXGI_SWAP_EFFECT_FLIP_DISCARD;check_hresult(factory->CreateSwapChainForHwnd(device.get(),window,&d,nullptr,nullptr,swap.put()));com_ptr<ID3D11Texture2D> back;check_hresult(swap->GetBuffer(0,__uuidof(ID3D11Texture2D),back.put_void()));com_ptr<ID3D11RenderTargetView> view;check_hresult(device->CreateRenderTargetView(back.get(),nullptr,view.put()));worker=std::thread([this,view]{while(!stopped){auto n=frames.load();float color[4]={(float)((n*7)%200+30)/255,(float)((n*11)%200+30)/255,(float)((n*17)%200+30)/255,1};context->ClearRenderTargetView(view.get(),color);HRESULT hr=swap->Present(1,0);if(hr==S_OK)frames++;else if(FAILED(hr)){error=hr;break;}}});}
 void openCopy(HWND window,int x,int y){worker=std::thread([this,window,x,y]{HDC screen=GetDC(nullptr),output=GetDC(window);while(!stopped){RECT r{};GetClientRect(window,&r);if(BitBlt(output,0,0,r.right,r.bottom,screen,x,y,SRCCOPY|CAPTUREBLT)){GdiFlush();received++;frames++;}else error=HRESULT_FROM_WIN32(GetLastError());Sleep(8);}ReleaseDC(window,output);ReleaseDC(nullptr,screen);});}
 void openDup(HWND window,HMONITOR monitor){
  duplicationMode=true;
  com_ptr<IDXGIFactory1> factory;check_hresult(CreateDXGIFactory1(__uuidof(IDXGIFactory1),factory.put_void()));com_ptr<IDXGIAdapter1> chosen;com_ptr<IDXGIOutput1> output;
  for(UINT i=0;;i++){com_ptr<IDXGIAdapter1> a;if(factory->EnumAdapters1(i,a.put())==DXGI_ERROR_NOT_FOUND)break;for(UINT j=0;;j++){com_ptr<IDXGIOutput> o;if(a->EnumOutputs(j,o.put())==DXGI_ERROR_NOT_FOUND)break;DXGI_OUTPUT_DESC d{};check_hresult(o->GetDesc(&d));if(d.Monitor==monitor){chosen=a;output=o.as<IDXGIOutput1>();}}}
  if(!output)throw hresult_error(E_INVALIDARG);
  check_hresult(D3D11CreateDevice(chosen.get(),D3D_DRIVER_TYPE_UNKNOWN,nullptr,D3D11_CREATE_DEVICE_BGRA_SUPPORT,nullptr,0,D3D11_SDK_VERSION,device.put(),nullptr,context.put()));
  com_ptr<IDXGIOutputDuplication> duplication;check_hresult(output->DuplicateOutput(device.get(),duplication.put()));DXGI_OUTDUPL_DESC dd{};duplication->GetDesc(&dd);com_ptr<IDXGIFactory2> dxFactory;check_hresult(chosen->GetParent(__uuidof(IDXGIFactory2),dxFactory.put_void()));DXGI_SWAP_CHAIN_DESC1 desc{};desc.Width=dd.ModeDesc.Width;desc.Height=dd.ModeDesc.Height;desc.Format=DXGI_FORMAT_B8G8R8A8_UNORM;desc.SampleDesc.Count=1;desc.BufferUsage=DXGI_USAGE_RENDER_TARGET_OUTPUT;desc.BufferCount=2;desc.SwapEffect=DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL;desc.Scaling=DXGI_SCALING_STRETCH;check_hresult(dxFactory->CreateSwapChainForHwnd(device.get(),window,&desc,nullptr,nullptr,swap.put()));
  worker=std::thread([this,duplication]{while(!stopped){std::unique_lock<std::mutex> frameLock(localContextGuard);DXGI_OUTDUPL_FRAME_INFO info{};com_ptr<IDXGIResource> resource;HRESULT hr=duplication->AcquireNextFrame(0,&info,resource.put());if(hr==DXGI_ERROR_WAIT_TIMEOUT){frameLock.unlock();Sleep(1);continue;}if(FAILED(hr)){error=hr;break;}try{auto texture=resource.as<ID3D11Texture2D>();com_ptr<ID3D11Texture2D> back;check_hresult(swap->GetBuffer(0,__uuidof(ID3D11Texture2D),back.put_void()));{context->CopyResource(back.get(),texture.get());if(wcsstr(GetCommandLineW(),L"--mirror-")){if(!lastImage){D3D11_TEXTURE2D_DESC snapshot{};back->GetDesc(&snapshot);snapshot.BindFlags=0;snapshot.MiscFlags=0;check_hresult(device->CreateTexture2D(&snapshot,nullptr,lastImage.put()));}context->CopyResource(lastImage.get(),back.get());}context->Flush();}received++;duplication->ReleaseFrame();hr=swap->Present(0,DXGI_PRESENT_DO_NOT_WAIT);if(hr==S_OK)frames++;else if(FAILED(hr)&&hr!=DXGI_ERROR_WAS_STILL_DRAWING)error=hr;}catch(...){duplication->ReleaseFrame();error=to_hresult();break;}}});
 }
 void open(HWND window,HMONITOR monitor){
  check_hresult(RoInitialize(RO_INIT_SINGLETHREADED));
  if(!GraphicsCaptureSession::IsSupported())throw hresult_error(E_NOTIMPL);
  {std::lock_guard<std::mutex> lock(deviceGuard);auto currentMonitor=MonitorFromWindow(window,MONITOR_DEFAULTTONEAREST);if(sharedMonitor!=currentMonitor){sharedDevice=nullptr;sharedContext=nullptr;sharedMonitor=currentMonitor;}if(!sharedDevice){
   com_ptr<IDXGIFactory1> factory;check_hresult(CreateDXGIFactory1(__uuidof(IDXGIFactory1),factory.put_void()));com_ptr<IDXGIAdapter1> chosen;HMONITOR panel=MonitorFromWindow(window,MONITOR_DEFAULTTONEAREST);
   for(UINT i=0;;i++){com_ptr<IDXGIAdapter1> candidate;if(factory->EnumAdapters1(i,candidate.put())==DXGI_ERROR_NOT_FOUND)break;for(UINT j=0;;j++){com_ptr<IDXGIOutput> output;if(candidate->EnumOutputs(j,output.put())==DXGI_ERROR_NOT_FOUND)break;DXGI_OUTPUT_DESC d{};if(SUCCEEDED(output->GetDesc(&d))&&d.Monitor==panel)chosen=candidate;}}
   check_hresult(D3D11CreateDevice(chosen.get(),chosen?D3D_DRIVER_TYPE_UNKNOWN:D3D_DRIVER_TYPE_HARDWARE,nullptr,D3D11_CREATE_DEVICE_BGRA_SUPPORT,nullptr,0,D3D11_SDK_VERSION,sharedDevice.put(),nullptr,sharedContext.put()));}
   device=sharedDevice;context=sharedContext;}

  auto dxgi=device.as<IDXGIDevice>();com_ptr<::IInspectable> inspectable;check_hresult(CreateDirect3D11DeviceFromDXGIDevice(dxgi.get(),inspectable.put()));auto runtime=inspectable.as<IDirect3DDevice>();
  GraphicsCaptureItem item{nullptr};auto factory=get_activation_factory<GraphicsCaptureItem,IGraphicsCaptureItemInterop>();check_hresult(factory->CreateForMonitor(monitor,guid_of<GraphicsCaptureItem>(),put_abi(item)));
  auto size=item.Size();com_ptr<IDXGIAdapter> adapter;check_hresult(dxgi->GetAdapter(adapter.put()));com_ptr<IDXGIFactory2> dxFactory;check_hresult(adapter->GetParent(__uuidof(IDXGIFactory2),dxFactory.put_void()));
  DXGI_SWAP_CHAIN_DESC1 desc{};desc.Width=size.Width;desc.Height=size.Height;desc.Format=DXGI_FORMAT_B8G8R8A8_UNORM;desc.SampleDesc.Count=1;desc.BufferUsage=DXGI_USAGE_RENDER_TARGET_OUTPUT;desc.BufferCount=2;desc.SwapEffect=DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL;desc.Scaling=DXGI_SCALING_STRETCH;
  check_hresult(dxFactory->CreateSwapChainForHwnd(device.get(),window,&desc,nullptr,nullptr,swap.put()));dxFactory->MakeWindowAssociation(window,DXGI_MWA_NO_ALT_ENTER);
  pool=Direct3D11CaptureFramePool::CreateFreeThreaded(runtime,DirectXPixelFormat::B8G8R8A8UIntNormalized,3,size);session=pool.CreateCaptureSession(item);session.IsCursorCaptureEnabled(false);try{session.MinUpdateInterval(std::chrono::milliseconds(0));}catch(...){}
  worker=std::thread([this]{
   for(;;){Direct3D11CaptureFrame frame{nullptr};{std::unique_lock<std::mutex> lock(guard);wake.wait(lock,[this]{return stopped||latest;});if(stopped)break;frame=std::move(latest);latest=nullptr;}
    try{auto access=frame.Surface().as<::Windows::Graphics::DirectX::Direct3D11::IDirect3DDxgiInterfaceAccess>();com_ptr<ID3D11Texture2D> texture;check_hresult(access->GetInterface(__uuidof(ID3D11Texture2D),texture.put_void()));com_ptr<ID3D11Texture2D> back;check_hresult(swap->GetBuffer(0,__uuidof(ID3D11Texture2D),back.put_void()));
     D3D11_TEXTURE2D_DESC src{},dst{};texture->GetDesc(&src);back->GetDesc(&dst);D3D11_BOX box{0,0,0,min(src.Width,dst.Width),min(src.Height,dst.Height),1};{std::lock_guard<std::mutex> lock(contextGuard);context->CopySubresourceRegion(back.get(),0,0,0,0,texture.get(),0,&box);if(wcsstr(GetCommandLineW(),L"--mirror-")){if(!lastImage){auto d=dst;d.BindFlags=0;d.MiscFlags=0;check_hresult(device->CreateTexture2D(&d,nullptr,lastImage.put()));}context->CopyResource(lastImage.get(),back.get());}context->Flush();}frame.Close();frame=nullptr;
     HRESULT result=swap->Present(0,DXGI_PRESENT_DO_NOT_WAIT);if(result==S_OK)frames++;else if(FAILED(result)&&result!=DXGI_ERROR_WAS_STILL_DRAWING)error=result;
    }catch(...){error=to_hresult();if(frame)frame.Close();}
   }
  });
  std::weak_ptr<Capture> weak=shared_from_this();token=pool.FrameArrived([weak](auto const& sender,auto const&){auto self=weak.lock();if(!self||self->stopped)return;
   try{auto frame=sender.TryGetNextFrame();if(!frame)return;std::lock_guard<std::mutex> lock(self->guard);if(self->stopped){frame.Close();return;}LARGE_INTEGER now{},frequency{};QueryPerformanceCounter(&now);QueryPerformanceFrequency(&frequency);auto age=(long long)(now.QuadPart*10000000.0/frequency.QuadPart)-frame.SystemRelativeTime().count();if(age>0)self->ageTicks+=(unsigned long long)age;self->received++;if(self->latest)self->latest.Close();self->latest=std::move(frame);self->wake.notify_one();}catch(...){self->error=to_hresult();}
  });session.StartCapture();
 }
 void close(){stopped=true;wake.notify_all();if(pool)pool.FrameArrived(token);if(session)session.Close();if(worker.joinable())worker.join();{std::lock_guard<std::mutex> lock(guard);if(latest)latest.Close();latest=nullptr;}if(pool)pool.Close();session=nullptr;pool=nullptr;swap=nullptr;context=nullptr;device=nullptr;}
 ~Capture(){try{close();}catch(...){}}

};
extern "C" __declspec(dllexport) HRESULT __cdecl StartCapture(HWND window,HMONITOR monitor,void** result){try{auto ptr=std::make_shared<Capture>();ptr->open(window,monitor);*result=new std::shared_ptr<Capture>(ptr);return S_OK;}catch(...){return to_hresult();}}
extern "C" __declspec(dllexport) void __cdecl StopCapture(void* handle){if(!handle)return;auto ptr=static_cast<std::shared_ptr<Capture>*>(handle);try{(*ptr)->close();}catch(...){}delete ptr;}
extern "C" __declspec(dllexport) unsigned long long __cdecl CaptureFrames(void* handle){return handle?(*static_cast<std::shared_ptr<Capture>*>(handle))->frames.load():0;}
extern "C" __declspec(dllexport) long __cdecl CaptureError(void* handle){return handle?(*static_cast<std::shared_ptr<Capture>*>(handle))->error.load():0;}



extern "C" __declspec(dllexport) unsigned long long __cdecl CaptureReceived(void* handle){return handle?(*static_cast<std::shared_ptr<Capture>*>(handle))->received.load():0;}


extern "C" __declspec(dllexport) HRESULT __cdecl StartDesktopCopy(HWND window,int x,int y,void** result){try{auto ptr=std::make_shared<Capture>();ptr->openCopy(window,x,y);*result=new std::shared_ptr<Capture>(ptr);return S_OK;}catch(...){return to_hresult();}}

extern "C" __declspec(dllexport) double __cdecl CaptureAgeMs(void* handle){if(!handle)return 0;auto p=*static_cast<std::shared_ptr<Capture>*>(handle);auto count=p->received.load();return count?p->ageTicks.load()/10000.0/count:0;}

extern "C" __declspec(dllexport) HRESULT __cdecl StartAnimation(HWND window,void** result){try{auto ptr=std::make_shared<Capture>();ptr->openAnimation(window);*result=new std::shared_ptr<Capture>(ptr);return S_OK;}catch(...){return to_hresult();}}

extern "C" __declspec(dllexport) HRESULT __cdecl StartDupCapture(HWND window,HMONITOR monitor,void** result){try{auto ptr=std::make_shared<Capture>();ptr->openDup(window,monitor);*result=new std::shared_ptr<Capture>(ptr);return S_OK;}catch(...){return to_hresult();}}

extern "C" __declspec(dllexport) HRESULT __cdecl SampleCapturePixel(void* handle,int x,int y,unsigned* color){try{if(!handle||!color)return E_INVALIDARG;auto p=*static_cast<std::shared_ptr<Capture>*>(handle);std::lock_guard<std::mutex> lock(p->duplicationMode?p->localContextGuard:contextGuard);com_ptr<ID3D11Texture2D> back;if(p->lastImage)back=p->lastImage;else check_hresult(p->swap->GetBuffer(0,__uuidof(ID3D11Texture2D),back.put_void()));D3D11_TEXTURE2D_DESC d{};back->GetDesc(&d);if(x<0||y<0||x>=(int)d.Width||y>=(int)d.Height)return E_INVALIDARG;d.Width=d.Height=1;d.BindFlags=0;d.MiscFlags=0;d.Usage=D3D11_USAGE_STAGING;d.CPUAccessFlags=D3D11_CPU_ACCESS_READ;com_ptr<ID3D11Texture2D> sample;check_hresult(p->device->CreateTexture2D(&d,nullptr,sample.put()));D3D11_BOX box{(UINT)x,(UINT)y,0,(UINT)x+1,(UINT)y+1,1};p->context->CopySubresourceRegion(sample.get(),0,0,0,0,back.get(),0,&box);D3D11_MAPPED_SUBRESOURCE mapped{};check_hresult(p->context->Map(sample.get(),0,D3D11_MAP_READ,0,&mapped));*color=*static_cast<unsigned*>(mapped.pData);p->context->Unmap(sample.get(),0);return S_OK;}catch(...){return to_hresult();}}
