using UnityEngine;

/// <summary>
/// Script simples para ler e imprimir os dados de um OVRSkeleton (mão) a cada frame.
/// Basta arrastar este script para um GameObject e atribuir o OVRSkeleton
/// desejado (ex: LeftHandAnchor ou RightHandAnchor) no campo "Skeleton" no Inspector.
/// </summary>
public class ReadOVRSkeleton : MonoBehaviour
{
    [Tooltip("Arraste aqui o OVRSkeleton que você quer ler (mão esquerda ou direita).")]
    [SerializeField]
    private OVRSkeleton _skeleton;

    [Tooltip("Se true, imprime a cada frame (Update). Se false, imprime só quando chamar ManualPrint().")]
    [SerializeField]
    private bool _printEveryFrame = true;

    private void Update()
    {
        if (_printEveryFrame)
        {
            PrintSkeletonData();
        }
    }

    /// <summary>
    /// Chame este método manualmente (ex: por um botão ou outro script)
    /// caso não queira imprimir a cada frame.
    /// </summary>
    public void ManualPrint()
    {
        PrintSkeletonData();
    }

    private void PrintSkeletonData()
    {
        if (_skeleton == null)
        {
            Debug.LogWarning("[ReadOVRSkeleton] Nenhum OVRSkeleton atribuído no Inspector.");
            return;
        }

        if (!_skeleton.IsInitialized)
        {
            Debug.Log("[ReadOVRSkeleton] Skeleton ainda não inicializado.");
            return;
        }

        if (!_skeleton.IsDataValid)
        {
            Debug.Log("[ReadOVRSkeleton] Dados de tracking inválidos no momento.");
            return;
        }

        Debug.Log($"[ReadOVRSkeleton] Tipo: {_skeleton.GetSkeletonType()} | " +
                   $"Confiança alta: {_skeleton.IsDataHighConfidence} | " +
                   $"Total de ossos: {_skeleton.Bones.Count}");

        foreach (var bone in _skeleton.Bones)
        {
            if (bone.Transform == null) continue;

            string boneName = OVRSkeleton.BoneLabelFromBoneId(_skeleton.GetSkeletonType(), bone.Id);

            Debug.Log($"[ReadOVRSkeleton] Osso: {boneName} | " +
                       $"Posição local: {bone.Transform.localPosition} | " +
                       $"Rotação local: {bone.Transform.localRotation.eulerAngles}");
        }
    }
}