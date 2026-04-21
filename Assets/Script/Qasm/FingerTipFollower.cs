using UnityEngine;

public class FingerTipFollower : MonoBehaviour
{
    [Header("請把你的 OVRHandPrefab (手部骨架) 拖進來")]
    public OVRSkeleton handSkeleton;

    [Header("要追蹤的骨骼 (預設為食指指尖)")]
    public OVRSkeleton.BoneId fingerBone = OVRSkeleton.BoneId.Hand_IndexTip;

    void Update()
    {
        // 確保手部骨架已經成功生成
        if (handSkeleton != null && handSkeleton.IsInitialized)
        {
            // 尋找我們指定的骨骼 (食指指尖)
            foreach (var bone in handSkeleton.Bones)
            {
                if (bone.Id == fingerBone)
                {
                    // 把這個碰撞體的位置，瞬間移動到食指指尖！
                    transform.position = bone.Transform.position;
                    transform.rotation = bone.Transform.rotation;
                    break;
                }
            }
        }
    }
}