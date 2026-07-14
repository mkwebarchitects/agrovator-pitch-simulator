using UnityEngine;

namespace Agrovator.PitchSimulator.Dialogue.Unity
{
    [CreateAssetMenu(fileName = "Scenario", menuName = "Pitch Simulator/Scenario")]
    public sealed class ScenarioAsset : ScriptableObject
    {
        [SerializeField]
        private TextAsset _json;

        [SerializeField]
        private UnityEngine.Object _judge;

        [SerializeField]
        private UnityEngine.Object _audio;

        public TextAsset Json => _json;

        public UnityEngine.Object Judge => _judge;

        public UnityEngine.Object Audio => _audio;
    }
}
