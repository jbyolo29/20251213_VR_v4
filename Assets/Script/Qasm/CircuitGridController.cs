using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CircuitGridController : MonoBehaviour
{
    public CircuitModel circuit;
    public QasmRunner runner;
    public BlochSphereAnimator[] blochAnimators;
    public GameObject dataTextObj;
    public ProbabilityDisplay probDisplay;

    [Header("Embodiment Visuals")]
    public GameObject targetRingPrefab;
    public float interactiveHaloRadius = 0.6f;

    private int currentStepIndex = 0;
    private QasmDebugResponse currentDebugData;
    private List<GateDragItem> currentBoardGates = new List<GateDragItem>();

    private void Awake()
    {
        if (circuit == null) circuit = FindObjectOfType<CircuitModel>();
        if (runner == null) runner = FindObjectOfType<QasmRunner>();
        if (probDisplay == null) probDisplay = FindObjectOfType<ProbabilityDisplay>();
        if (blochAnimators == null || blochAnimators.Length == 0) blochAnimators = FindObjectsOfType<BlochSphereAnimator>();
    }

    private void Start() => ForceResetVisuals();

    public void PlaceGate(int row, int col, string op) => StartCoroutine(DelayedRestart());
    public void DeleteGate(int row, int col) => StartCoroutine(DelayedRestart());

    IEnumerator DelayedRestart()
    {
        yield return new WaitForEndOfFrame();

        currentBoardGates = FindObjectsOfType<GateDragItem>()
            .Where(g => g.isFromCircuitBoard)
            .OrderBy(g => g.originalCol)
            .ToList();

        if (circuit != null)
        {
            circuit.ClearAll();
            foreach (var g in currentBoardGates)
            {
                circuit.PlaceGate(g.originalRow, g.originalCol, g.op);
            }
        }

        if (currentBoardGates.Count == 0)
        {
            ForceResetVisuals();
        }
        else
        {
            ClearExistingTargets();
            runner.RunSimulation(); // 完整電路送給 Python 算
        }
    }

    // 接收 Python 算好的每一步驟座標
    public void UpdateBlochSpheres(QasmDebugResponse data)
    {
        if (data == null || data.debug_steps == null) return;
        currentDebugData = data;
        currentStepIndex = 0;
        ShowCurrentStep();
    }

    public void AdvanceToNextStep()
    {
        currentStepIndex++;
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        ClearExistingTargets();

        if (currentDebugData == null || currentDebugData.debug_steps == null) return;

        // ★ 修改 3：如果全部步驟碰完了，不要刪除邏輯閘！只要清空光環並重置進度就好
        if (currentStepIndex >= currentDebugData.debug_steps.Length || currentStepIndex >= currentBoardGates.Count)
        {
            ClearExistingTargets();
            currentStepIndex = 0; // 進度歸零，方便下次操作
            return; // 不再呼叫 ClearAllCircuit()，保留電路板上的閘
        }

        // ★ 修改 2：拿掉多餘的閃爍干擾，讓所有在電路板上的閘保持正常顯示
        for (int i = 0; i < currentBoardGates.Count; i++)
        {
            currentBoardGates[i].SetGateVisibility(true);
        }

        // 讀取這一步的 Qubit 座標
        var stepData = currentDebugData.debug_steps[currentStepIndex];
        if (stepData.qubits == null || stepData.qubits.Length == 0) return;

        float x = stepData.qubits[0].x;
        float y = stepData.qubits[0].y;
        float z = stepData.qubits[0].z;

        if (blochAnimators != null && blochAnimators.Length > 0 && blochAnimators[0] != null)
        {
            blochAnimators[0].SetState(x, y, z, 1.0f);

            // 畫出光環
            if (targetRingPrefab != null)
            {
                Vector3 dir = new Vector3(y, z, x).normalized;
                if (dir == Vector3.zero) dir = Vector3.up;

                Vector3 spawnPos = blochAnimators[0].transform.position + dir * interactiveHaloRadius;

                // ★ 修改 1：使用 FromToRotation 讓圓環的 Y 軸（法線）對齊 dir，完美服貼球體表面！
                Quaternion flushRotation = Quaternion.FromToRotation(Vector3.up, dir);
                GameObject ring = Instantiate(targetRingPrefab, spawnPos, flushRotation);

                QuantumTarget targetScript = ring.GetComponent<QuantumTarget>();
                if (targetScript != null)
                {
                    targetScript.targetX = x; targetScript.targetY = y; targetScript.targetZ = z;
                }
            }
        }

        if (dataTextObj != null)
        {
            var tmp = dataTextObj.GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = $"Step {currentStepIndex + 1}/{currentBoardGates.Count}\nQ0: (x:{x:F1}, y:{y:F1}, z:{z:F1})";
        }
    }

    public void UpdateUIPanel(QasmProgramIR data)
    {
        if (circuit == null || probDisplay == null) return;

        float p00 = 0f, p01 = 0f, p10 = 0f, p11 = 0f;
        string qasmStr = circuit.GetQASM().ToLower();

        // 只有在電路包含 measure (測量閘) 的時候，才顯示機率條
        if (qasmStr.Contains("measure") && data != null && data.probabilities != null && data.probabilities.Length > 0)
        {
            p00 = (float)data.probabilities[0];
            p01 = data.probabilities.Length > 1 ? (float)data.probabilities[1] : 0;
            p10 = data.probabilities.Length > 2 ? (float)data.probabilities[2] : 0;
            p11 = data.probabilities.Length > 3 ? (float)data.probabilities[3] : 0;
        }

        probDisplay.UpdateProbabilities(p00, p01, p10, p11);
    }

    public void ClearAllCircuit()
    {
        if (circuit != null) circuit.ClearAll();
        foreach (var slot in FindObjectsOfType<GateDropSlot>())
        {
            if (slot != null)
            {
                foreach (Transform child in slot.transform)
                {
                    if (child.GetComponent<GateDragItem>() != null) Destroy(child.gameObject);
                }
            }
        }
        ForceResetVisuals();
    }

    private void ClearExistingTargets()
    {
        foreach (var t in FindObjectsOfType<QuantumTarget>()) Destroy(t.gameObject);
    }

    private void ForceResetVisuals()
    {
        ClearExistingTargets();
        if (blochAnimators != null) foreach (var a in blochAnimators) if (a != null) a.ResetToZero();
        if (dataTextObj != null)
        {
            var tmp = dataTextObj.GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = "System Ready";
        }
        if (probDisplay != null) probDisplay.UpdateProbabilities(0f, 0f, 0f, 0f);
        currentStepIndex = 0;
        if (currentBoardGates != null) currentBoardGates.Clear();
        currentDebugData = null;
    }
}