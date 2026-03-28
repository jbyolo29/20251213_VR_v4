using UnityEngine;
using TMPro;

public class QasmCodeView : MonoBehaviour
{
    [Header("References")]
    public CircuitModel circuit;       // 連結到電路模型
    public TextMeshProUGUI codeText;   // 連結到顯示文字的 UI

    private void Start()
    {
        // 自動抓取
        if (circuit == null) circuit = FindObjectOfType<CircuitModel>();
        Refresh();
    }

    public void Refresh()
    {
        if (circuit != null && codeText != null)
        {
            // 直接呼叫 circuit.GetQASM()，修正原本的錯誤
            codeText.text = circuit.GetQASM();
        }
    }
}