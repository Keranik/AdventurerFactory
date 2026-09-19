// ──────────────────────────────────────────────────────────────
// THE ONLY .cs FILE IN THE UNITY PROJECT
// All other Presentation logic lives inside the precompiled
// ForgeFlow.Presentation.Unity.dll in Assets/Plugins/ForgeFlow.Presentation/.
//
// This thin wrapper exists solely because Unity needs at least one
// MonoBehaviour script in Assets/ to attach to a GameObject in the scene.
// It delegates 100% of work to the DLL-compiled FactoryEntryPoint.
// ──────────────────────────────────────────────────────────────

/// <summary>
/// Attach this to the single GameObject in the scene.
/// Start() creates a <see cref="ForgeFlow.Presentation.Unity.FactoryEntryPoint"/>
/// (a plain C# class, NOT a MonoBehaviour) and delegates lifecycle calls to it.
/// </summary>
public class FactoryBootstrap : UnityEngine.MonoBehaviour
{
    [UnityEngine.Header("Configuration")]
    [UnityEngine.SerializeField] private string _modsPath = "Mods";

    private ForgeFlow.Presentation.Unity.FactoryEntryPoint _entryPoint;

    private void Start()
    {
        UnityEngine.Object.DontDestroyOnLoad(gameObject);
        _entryPoint = new ForgeFlow.Presentation.Unity.FactoryEntryPoint();
        _entryPoint.SetModsPath(_modsPath);
        _entryPoint.Initialize();
    }

    private void Update()
    {
        _entryPoint?.Tick(UnityEngine.Time.deltaTime);
    }

    private void OnDestroy()
    {
        _entryPoint?.Shutdown();
    }
}
