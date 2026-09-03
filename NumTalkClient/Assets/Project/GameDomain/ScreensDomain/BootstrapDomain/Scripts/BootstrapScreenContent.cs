using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Project.GameDomain.ScreensDomain.BootstrapDomain.Scripts
{
    [CreateAssetMenu(fileName = "BootstrapScreenContent", menuName = "Project/Content/BootstrapScreenContent")]
    public class BootstrapScreenContent : ScriptableObject
    {
        [field: SerializeField] public AssetReference Splash { get; private set; }
    }
}
