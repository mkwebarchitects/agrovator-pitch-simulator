using UnityEngine;

namespace Agrovator.PitchSimulator.UI
{
    /// <summary>
    /// Records which builder generation produced an owned scene root. The
    /// project builder's per-scene contract predicates are hand-maintained
    /// property whitelists: any builder change they do not happen to inspect
    /// would otherwise leave the saved scene stale with no failing test. The
    /// stamp makes regeneration depend on one declared version instead, so the
    /// property checks stay only as a safety net.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GeneratedSceneStamp : MonoBehaviour
    {
        [SerializeField] private int generatorVersion;

        public int GeneratorVersion => generatorVersion;
    }
}
