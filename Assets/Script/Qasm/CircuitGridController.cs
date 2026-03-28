using UnityEngine;
using TMPro;

public class CircuitGridController : MonoBehaviour
{
    public CircuitModel circuit;
    public QasmCodeView codeView;
    public QasmRunner runner;
    public BlochSphereAnimator[] blochAnimators;
    public ProbabilityDisplay probDisplay;
    public GameObject dataTextObj;

    private void Awake()
    {
        if (circuit == null) circuit = FindObjectOfType<CircuitModel>();
        if (codeView == null) codeView = FindObjectOfType<QasmCodeView>();
        if (runner == null) runner = FindObjectOfType<QasmRunner>();
        if (probDisplay == null) probDisplay = FindObjectOfType<ProbabilityDisplay>();

        if (blochAnimators == null || blochAnimators.Length == 0)
        {
            var found = FindObjectsOfType<BlochSphereAnimator>();
            blochAnimators = found ?? new BlochSphereAnimator[0];
        }
    }

    private void Start() => ForceResetVisuals();

    public void PlaceGate(int row, int col, string op)
    {
        if (circuit == null) return;
        if (op == "CX")
        {
            if (row + 1 < circuit.numWires)
            {
                circuit.PlaceGate(row, col, "C");
                circuit.PlaceGate(row + 1, col, "T");
            }
        }
        else circuit.PlaceGate(row, col, op);
        RefreshSystem();
    }

    public void DeleteGate(int row, int col)
    {
        if (circuit != null) circuit.DeleteGate(row, col);
        RefreshSystem();
    }

    public void ClearAllCircuit()
    {
        if (circuit != null) circuit.ClearAll();
        var allSlots = FindObjectsOfType<GateDropSlot>();
        if (allSlots != null)
        {
            foreach (var slot in allSlots)
                if (slot != null) foreach (Transform child in slot.transform) Destroy(child.gameObject);
        }
        ForceResetVisuals();
        RefreshSystem();
    }

    private void RefreshSystem()
    {
        if (codeView != null) codeView.Refresh();
        if (runner != null) runner.RunSimulation();
    }

    private void ForceResetVisuals()
    {
        if (probDisplay != null) probDisplay.UpdateProbabilities(0, 0, 0, 0);

        if (blochAnimators != null && blochAnimators.Length > 0)
        {
            foreach (var anim in blochAnimators)
            {
                if (anim != null) anim.ResetToZero();
            }
        }

        if (dataTextObj != null)
        {
            var tmp = dataTextObj.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = "Awaiting input...";
        }
    }

    public void UpdateUIPanel(QasmProgramIR data)
    {
        if (circuit == null) return;
        string qasm = circuit.GetQASM().ToLower();

        bool hasMeasureQ0 = qasm.Contains("measure q[0]");
        bool hasMeasureQ1 = qasm.Contains("measure q[1]");
        bool hasAnyMeasure = hasMeasureQ0 || hasMeasureQ1 || qasm.Contains("measure");

        // 如果連測量閘都沒有，清空機率條
        if (!hasAnyMeasure)
        {
            if (probDisplay != null) probDisplay.UpdateProbabilities(0, 0, 0, 0);
            return;
        }

        float p00 = 0f, p01 = 0f, p10 = 0f, p11 = 0f;

        if (data == null || data.probabilities == null || data.probabilities.Length == 0)
        {
            p00 = 1.0f;
        }
        else
        {
            p00 = (float)data.probabilities[0];
            p01 = data.probabilities.Length > 1 ? (float)data.probabilities[1] : 0;
            p10 = data.probabilities.Length > 2 ? (float)data.probabilities[2] : 0;
            p11 = data.probabilities.Length > 3 ? (float)data.probabilities[3] : 0;
        }

        // ==========================================
        // ★ 量子糾纏自動偵測系統 (Entanglement Detector) ★
        // ==========================================

        // 1. 計算單獨位元的邊緣機率
        float m_Q0_0 = p00 + p10;
        float m_Q0_1 = p01 + p11;
        float m_Q1_0 = p00 + p01;
        float m_Q1_1 = p10 + p11;

        // 2. 獨立性檢定：如果 P(AB) == P(A) * P(B)，代表兩顆球「沒有糾纏」
        bool isIndependent = Mathf.Abs(p00 - (m_Q1_0 * m_Q0_0)) < 0.01f &&
                             Mathf.Abs(p11 - (m_Q1_1 * m_Q0_1)) < 0.01f;

        // 3. UI 顯示邏輯分配
        if (isIndependent)
        {
            // 【狀況 A：兩顆球獨立不連動】-> 玩家測量誰，就只顯示誰的專屬結果！
            if (hasMeasureQ0 && !hasMeasureQ1)
            {
                // 只測量 Q0：把 Q0 的機率集中顯示，無視 Q1
                p00 = m_Q0_0;
                p01 = m_Q0_1;
                p10 = 0f;
                p11 = 0f;
            }
            else if (!hasMeasureQ0 && hasMeasureQ1)
            {
                // 只測量 Q1：把 Q1 的機率集中顯示，無視 Q0
                p00 = m_Q1_0;
                p10 = m_Q1_1;
                p01 = 0f;
                p11 = 0f;
            }
        }
        // 【狀況 B：兩顆球發生糾纏！】-> 放行原始的聯動機率 (p00, p01, p10, p11) 讓它們一起顯示！

        if (probDisplay != null) probDisplay.UpdateProbabilities(p00, p01, p10, p11);
    }

    public void UpdateBlochSpheres(QasmDebugResponse debugData)
    {
        string qasm = circuit != null ? circuit.GetQASM().ToLower() : "";

        if (debugData == null || debugData.debug_steps == null || debugData.debug_steps.Length == 0)
        {
            if (blochAnimators != null)
            {
                foreach (var anim in blochAnimators)
                    if (anim != null) anim.ResetToZero();
            }

            if (dataTextObj != null)
            {
                var tmp = dataTextObj.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    if (qasm.Contains("measure"))
                    {
                        bool mQ0 = qasm.Contains("measure q[0]");
                        bool mQ1 = qasm.Contains("measure q[1]");
                        tmp.text = $"Q0: r=1.00 (x:0.0, y:0.0, z:1.0) {(mQ0 ? "<color=green>[Observed]</color>" : "")}\n" +
                                   $"Q1: r=1.00 (x:0.0, y:0.0, z:1.0) {(mQ1 ? "<color=green>[Observed]</color>" : "")}\n";
                    }
                    else
                    {
                        tmp.text = "Awaiting input...";
                    }
                }
            }
            return;
        }

        var lastStep = debugData.debug_steps[debugData.debug_steps.Length - 1];
        if (lastStep.qubits == null) return;

        string statusText = "";
        for (int i = 0; i < lastStep.qubits.Length; i++)
        {
            var q = lastStep.qubits[i];

            if (blochAnimators != null && i < blochAnimators.Length && blochAnimators[i] != null)
                blochAnimators[i].SetState(q.x, q.y, q.z, q.radius);

            bool isThisQubitMeasured = qasm.Contains($"measure q[{i}]");

            statusText += $"Q{i}: r={q.radius:F2} (x:{q.x:F1}, y:{q.y:F1}, z:{q.z:F1}) " +
                          (isThisQubitMeasured ? "<color=green>[Observed]</color>\n" : "\n");
        }

        if (dataTextObj != null)
        {
            var tmp = dataTextObj.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = statusText;
        }
    }
}