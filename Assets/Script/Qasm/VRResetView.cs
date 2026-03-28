using UnityEngine;

public class VRResetView : MonoBehaviour
{
    [Header("拖入對應的物件")]
    public Transform xrRig;       // 拖入 OVRCameraRig
    public Transform vrHead;      // 拖入 CenterEyeAnchor
    public Transform resetPoint;  // 拖入 ResetPoint

    void Update()
    {
        // 觸發條件 1：電腦鍵盤的「空白鍵」(記得滑鼠要先點一下 Game 視窗)
        bool keyboardTrigger = Input.GetKeyDown(KeyCode.Space);

        // 觸發條件 2：Quest 右手控制器的「A 鍵」或「按下右搖桿」
        // (OVRInput 是 Oculus 專屬的輸入系統，OVRCameraRig 內建支援)
        bool vrTrigger = false;
        if (OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick))
        {
            vrTrigger = true;
        }

        // 只要有按鍵觸發，就執行瞬間移動！
        if (keyboardTrigger || vrTrigger)
        {
            if (xrRig != null && vrHead != null && resetPoint != null)
            {
                // 計算頭盔中心與攝影機的差距
                Vector3 offset = xrRig.position - vrHead.position;
                offset.y = 0; // 鎖定高度，不影響玩家身高

                // 瞬間傳送
                xrRig.position = resetPoint.position + offset;

                // 強制轉向面對桌子
                Vector3 lookDir = resetPoint.forward;
                lookDir.y = 0;
                xrRig.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }
}