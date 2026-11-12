#include <windows.h>
#include <exdisp.h>
#include <shlobj.h>
#include <shlguid.h>
#include <wrl/client.h>
#include <fstream>
#include <vector>
using Microsoft::WRL::ComPtr;
namespace {
struct View {ComPtr<IFolderView> folder; ComPtr<IFolderView2> settings; HWND window{};};
HRESULT desktop(View& v) {
 ComPtr<IShellWindows> windows; HRESULT hr=CoCreateInstance(CLSID_ShellWindows,nullptr,CLSCTX_ALL,IID_PPV_ARGS(&windows));if(FAILED(hr))return hr;
 VARIANT loc{},empty{};loc.vt=VT_I4;loc.lVal=CSIDL_DESKTOP;long hwnd=0;ComPtr<IDispatch> dispatch;
 hr=windows->FindWindowSW(&loc,&empty,SWC_DESKTOP,&hwnd,SWFO_NEEDDISPATCH,&dispatch);if(FAILED(hr)||!dispatch)return E_FAIL;
 ComPtr<::IServiceProvider> provider;hr=dispatch.As(&provider);if(FAILED(hr))return hr;
 ComPtr<IShellBrowser> browser;hr=provider->QueryService(SID_STopLevelBrowser,IID_PPV_ARGS(&browser));if(FAILED(hr))return hr;
 ComPtr<IShellView> shell;hr=browser->QueryActiveShellView(&shell);if(FAILED(hr))return hr;
 shell->GetWindow(&v.window);hr=shell.As(&v.folder);shell.As(&v.settings);return hr;
}
struct Entry {std::vector<BYTE> id;POINT point;};
}
extern "C" __declspec(dllexport) HRESULT __cdecl SaveDesktopIcons(const wchar_t* path) {try {
 View v;HRESULT hr=desktop(v);if(FAILED(hr))return hr;int count=0;hr=v.folder->ItemCount(SVGIO_ALLVIEW,&count);if(FAILED(hr))return hr;
 DWORD flags=0;if(v.settings)v.settings->GetCurrentFolderFlags(&flags);
 std::vector<Entry> entries;for(int i=0;i<count;i++){PITEMID_CHILD id=nullptr;hr=v.folder->Item(i,&id);if(FAILED(hr))return hr;POINT point{};hr=v.folder->GetItemPosition(id,&point);if(FAILED(hr)){CoTaskMemFree(id);return hr;}ClientToScreen(v.window,&point);UINT size=ILGetSize(id);entries.push_back({std::vector<BYTE>((BYTE*)id,(BYTE*)id+size),point});CoTaskMemFree(id);}
 std::wstring temp=std::wstring(path)+L".tmp";std::ofstream out(temp,std::ios::binary);DWORD magic=0x324e4349,n=(DWORD)entries.size();out.write((char*)&magic,4);out.write((char*)&flags,4);out.write((char*)&n,4);for(auto& e:entries){DWORD size=(DWORD)e.id.size();out.write((char*)&size,4);out.write((char*)e.id.data(),size);out.write((char*)&e.point,sizeof(POINT));}out.close();if(!out)return E_FAIL;if(!MoveFileExW(temp.c_str(),path,MOVEFILE_REPLACE_EXISTING|MOVEFILE_WRITE_THROUGH))return HRESULT_FROM_WIN32(GetLastError());return S_OK;
 }catch(...){return E_FAIL;}}
extern "C" __declspec(dllexport) HRESULT __cdecl RestoreDesktopIcons(const wchar_t* path) {try {
 std::ifstream in(path,std::ios::binary);DWORD magic=0,flags=0,n=0;in.read((char*)&magic,4);in.read((char*)&flags,4);in.read((char*)&n,4);if(magic!=0x324e4349||n>100000)return E_INVALIDARG;
 std::vector<Entry> entries;for(DWORD i=0;i<n;i++){DWORD size=0;in.read((char*)&size,4);if(size<2||size>65536)return E_INVALIDARG;Entry e;e.id.resize(size);in.read((char*)e.id.data(),size);in.read((char*)&e.point,sizeof(POINT));if(!in||e.id[size-1]!=0||e.id[size-2]!=0)return E_INVALIDARG;entries.push_back(std::move(e));}
 View v;HRESULT hr=desktop(v);if(FAILED(hr))return hr;DWORD current=0;if(v.settings){v.settings->GetCurrentFolderFlags(&current);hr=v.settings->SetCurrentFolderFlags(FWF_AUTOARRANGE,0);if(FAILED(hr))return hr;}
 std::vector<PCUITEMID_CHILD> ids;std::vector<POINT> points;
 // Enumerate current items: deleted icons are ignored, newly created ones are retained.
 int count=0;v.folder->ItemCount(SVGIO_ALLVIEW,&count);for(int i=0;i<count;i++){PITEMID_CHILD id=nullptr;if(FAILED(v.folder->Item(i,&id)))continue;for(auto& e:entries)if(ILIsEqual(id,(PCIDLIST_ABSOLUTE)e.id.data())){POINT p=e.point;ScreenToClient(v.window,&p);ids.push_back((PCUITEMID_CHILD)e.id.data());points.push_back(p);break;}CoTaskMemFree(id);}
 hr=ids.empty()?S_OK:v.folder->SelectAndPositionItems((UINT)ids.size(),ids.data(),points.data(),SVSI_POSITIONITEM);
 if(SUCCEEDED(hr)&&!(flags&FWF_AUTOARRANGE)){for(size_t i=0;i<ids.size();i++){POINT actual{};if(FAILED(v.folder->GetItemPosition(ids[i],&actual))||abs(actual.x-points[i].x)>2||abs(actual.y-points[i].y)>2){hr=HRESULT_FROM_WIN32(ERROR_RETRY);break;}}}
 if(v.settings){HRESULT reset=v.settings->SetCurrentFolderFlags(FWF_AUTOARRANGE,flags&FWF_AUTOARRANGE);if(SUCCEEDED(hr))hr=reset;}return hr;
 }catch(...){return E_FAIL;}}
