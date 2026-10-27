using System;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.PoseDetection;
using UnityEngine;

namespace MyoGuess.Gameplay
{
    [Serializable]
    public sealed class HeadsetGestureBinding
    {
        public GestureType gesture;
        [Tooltip("ActiveStateGroup da pose da mão direita.")]
        public MonoBehaviour activeState;
        public Sprite image;
    }

    public sealed class HeadsetGestureSource : GestureSource
    {
        [SerializeField] private HeadsetGestureBinding[] bindings;
        [SerializeField] private UnityEngine.UI.Image recognizedGestureImage;

        private GestureType lastGesture;
        private string lastPoseName;
        private string lastReason;

        public override bool IsAvailable => isActiveAndEnabled &&
            bindings != null && bindings.Length > 0;

        private void Awake()
        {
            if (bindings == null) return;

            foreach (var binding in bindings)
            {
                if (binding?.activeState == null ||
                    binding.activeState.GetComponent<ShapeRecognizerActiveState>() == null) continue;

                // Reconhece o formato dos dedos em qualquer direção da mão.
                // Mantém os grupos e seus eventos; remove só as exigências de orientação
                // nas instâncias usadas pelo jogo, sem alterar os assets do SDK.
                foreach (var orientation in binding.activeState.GetComponents<TransformRecognizerActiveState>())
                {
                    orientation.InjectTransformFeatureList(
                        TransformFeatureConfigList.Create(new List<TransformFeatureConfig>()));
                }
            }
        }

        // Usa a mesma prioridade do indicador de gesto da partida, sem favorecer o alvo esperado.
        public GestureType GetRecognizedGesture() => FindRecognizedBinding()?.gesture ?? GestureType.None;

        public override bool CanRecognize(GestureType gesture)
        {
            if (bindings == null || gesture == GestureType.None) return false;
            foreach (var binding in bindings)
                if (binding != null && binding.gesture == gesture && binding.activeState is IActiveState)
                    return true;
            return false;
        }

        private void LateUpdate()
        {
            if (recognizedGestureImage == null) return;
            var recognized = FindRecognizedBinding();
            recognizedGestureImage.sprite = recognized?.image;
            recognizedGestureImage.enabled = recognized?.image != null;
        }

        private void OnDisable()
        {
            if (recognizedGestureImage != null)
            {
                recognizedGestureImage.sprite = null;
                recognizedGestureImage.enabled = false;
            }
        }

        private HeadsetGestureBinding FindRecognizedBinding()
        {
            if (!IsAvailable) return null;
            HeadsetGestureBinding recognized = null;
            foreach (var binding in bindings)
            {
                if (binding == null || binding.gesture == GestureType.None ||
                    binding.activeState == null || !binding.activeState.isActiveAndEnabled ||
                    !(binding.activeState is IActiveState state) || !state.Active) continue;

                // Rest is permissive; a more specific pose takes precedence in the HUD.
                if (recognized == null || RecognitionPriority(binding.gesture) > RecognitionPriority(recognized.gesture))
                    recognized = binding;
            }
            return recognized;
        }

        private static int RecognitionPriority(GestureType gesture)
        {
            switch (gesture)
            {
                case GestureType.Tripod: return 5;
                case GestureType.ThumbsUp: return 4;
                case GestureType.Rock: return 3;
                case GestureType.Pointer: return 2;
                case GestureType.OpenHand: return 1;
                default: return 0;
            }
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
