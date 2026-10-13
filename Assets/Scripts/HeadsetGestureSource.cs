using System;
using Oculus.Interaction;
using UnityEngine;

namespace MyoGuess.Gameplay
{
    [Serializable]
    public sealed class HeadsetGestureBinding
    {
        public GestureType gesture;
        [Tooltip("ActiveStateGroup da pose da mão direita.")]
        public MonoBehaviour activeState;
    }

    public sealed class HeadsetGestureSource : GestureSource
    {
        [SerializeField] private HeadsetGestureBinding[] bindings;

        private GestureType lastGesture;
        private string lastPoseName;
        private string lastReason;

        public override bool IsAvailable => isActiveAndEnabled &&
            bindings != null && bindings.Length > 0;

        public override bool CanRecognize(GestureType gesture)
        {
            if (bindings == null || gesture == GestureType.None) return false;
            foreach (var binding in bindings)
                if (binding != null && binding.gesture == gesture && binding.activeState is IActiveState)
                    return true;
            return false;
        }

        public override bool IsGestureActive(GestureType gesture)
        {
            lastGesture = gesture;
            lastPoseName = "sem vínculo";
            lastReason = "fonte indisponível";
            if (!IsAvailable) return false;

            foreach (var binding in bindings)
            {
                if (binding == null || binding.gesture != gesture || binding.activeState == null ||
                    !(binding.activeState is IActiveState state)) continue;
                lastPoseName = binding.activeState.name;
                if (state.Active)
                {
                    lastReason = "pose correspondente ativa";
                    return true;
                }
            }
            lastReason = lastPoseName == "sem vínculo" ? "gesto sem pose configurada" : "pose correspondente inativa";
            return false;
        }

        public string DescribeLastReading()
        {
            var activeGestures = new System.Collections.Generic.List<string>();
            if (bindings != null)
                foreach (var binding in bindings)
                    if (binding != null && binding.activeState != null &&
                        binding.activeState is IActiveState state && state.Active)
                        activeGestures.Add(binding.gesture.ToString());

            string active = activeGestures.Count == 0 ? "nenhum" : string.Join(", ", activeGestures);
            return $"alvo={lastGesture}, pose={lastPoseName}, " +
                $"gestosAtivos=[{active}], motivo={lastReason}";
        }
    }
}
