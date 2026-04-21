using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class VRPokeButton : MonoBehaviour
{
    private Button btn;
    private bool isCooldown = false;

    void Awake()
    {
        btn = GetComponent<Button>();
    }

    void Update()
    {
        if (isCooldown) return;

        GameObject[] fingerTips = GameObject.FindGameObjectsWithTag("PlayerHand");

        // 直接使用按鈕的世界座標中心
        Vector3 targetPos = transform.position;

        foreach (var tip in fingerTips)
        {
            // 將距離放寬到 8 公分 (0.08f)，只要靠近就能按到！
            if (Vector3.Distance(tip.transform.position, targetPos) < 0.08f)
            {
                Debug.Log($"<color=green>[VRPokeButton]</color> 雷達觸發！{gameObject.name} 被你的 {tip.name} 按下了！");
                btn.onClick.Invoke();

                isCooldown = true;
                Invoke(nameof(ResetCooldown), 1.0f);
                return;
            }
        }
    }

    void ResetCooldown() => isCooldown = false;
}