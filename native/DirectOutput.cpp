#include <windows.h>
#include <winrt/base.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Devices.Display.Core.h>

// Acquires and immediately releases the selected target. No mode is applied.
// Preserve the actual HRESULT instead of inferring access from OS edition.
extern "C" __declspec(dllexport) HRESULT __cdecl ProbeDirectTarget(
    unsigned low, int high, unsigned id, int* acquired) noexcept {
    if (!acquired) return E_POINTER;
    *acquired = 0;
    try {
        winrt::init_apartment(winrt::apartment_type::multi_threaded);
        struct ApartmentScope { ~ApartmentScope() { winrt::uninit_apartment(); } } apartment;
        using namespace winrt::Windows::Devices::Display::Core;
        auto manager = DisplayManager::Create(DisplayManagerOptions::EnforceSourceOwnership);
        for (auto target : manager.GetCurrentTargets()) {
            auto adapter = target.Adapter().Id();
            if (adapter.LowPart != low || adapter.HighPart != high || target.AdapterRelativeId() != id) continue;
            auto targets = winrt::single_threaded_vector<DisplayTarget>({target});
            auto result = manager.TryAcquireTargetsAndReadCurrentState(targets);
            if (result.ErrorCode() != DisplayManagerResult::Success) {
                auto error = result.ExtendedErrorCode();
                return FAILED(error) ? static_cast<HRESULT>(error) : E_ACCESSDENIED;
            }
            manager.ReleaseTarget(target);
            *acquired = 1;
            return S_OK;
        }
        return HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
    } catch (...) { return winrt::to_hresult(); }
}
