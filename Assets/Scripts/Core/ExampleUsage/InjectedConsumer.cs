using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HeistNSeek.Core
{
    public class InjectedConsumer : MonoBehaviour
    {
        [Inject] private IPlainService plainService;
        [Inject] private MonoService monoService;
        [Inject] private IGlobalTestService globalService;
        [Inject] private IMonoGlobalService monoGlobalService;

        private void Start()
        {
            var a = plainService != null ? plainService.GetGreeting() : "<plainService null>";
            var b = monoService != null ? monoService.GetMessage() : "<monoService null>";
            var g = globalService != null ? globalService.GetMessage() : "<globalService null>";
            var mg = monoGlobalService != null ? monoGlobalService.GetInfo() : "<monoGlobalService null>";
            Debug.Log($"InjectedConsumer.Start -> {a} | {b} | {g} | {mg}");
        }
    }
}


