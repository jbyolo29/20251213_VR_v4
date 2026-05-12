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

    private Vector3 pendingRingSpawnPos;
    private Quaternion pendingRingRotation;
    private float pendingX, pendingY, pendingZ;

    private Coroutine autoStartCoroutine;
    private bool isWaitingForStartRing = false;

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

        if (autoStartCoroutine != null)
        {
            StopCoroutine(autoStartCoroutine);
            autoStartCoroutine = null;
        }

        if (currentBoardGates.Count == 0) ForceResetVisuals();
        else
        {
            ResetSimulationState();
            autoStartCoroutine = StartCoroutine(WaitAndRunSimulation());
        }
    }

    private IEnumerator WaitAndRunSimulation()
    {
        if (dataTextObj != null)
        {
            var tmp = dataTextObj.GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = "System Ready.\nStarting in 3...";
        }
        yield return new WaitForSeconds(1.0f);

        if (dataTextObj != null)
        {
            var tmp = dataTextObj.GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = "System Ready.\nStarting in 2...";
        }
        yield return new WaitForSeconds(1.0f);

        if (dataTextObj != null)
        {
            var tmp = dataTextObj.GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = "System Ready.\nStarting in 1...";
        }
        yield return new WaitForSeconds(1.0f);

        if (dataTextObj != null)
        {
            var tmp = dataTextObj.GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = "Touch the Top Ring to Execute!";
        }

        // 【修正開場光環位置】：向 Animator 詢問絕對的「正上方」在哪裡，避免跑到下方
        if (targetRingPrefab != null && blochAnimators != null && blochAnimators.Length > 0 && blochAnimators[0] != null)
        {
            Vector3 centerPos = blochAnimators[0].transform.position;
            Vector3 upDir = blochAnimators[0].GetWorldDirection(0, 0, 1); // 絕對的正 Z 軸

            Vector3 spawnPos = centerPos + (upDir * interactiveHaloRadius);
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, upDir);

            GameObject startRing = Instantiate(targetRingPrefab, spawnPos, rot);
            QuantumTarget targetScript = startRing.GetComponent<QuantumTarget>();
            if (targetScript != null)
            {
                targetScript.targetX = 0; targetScript.targetY = 0; targetScript.targetZ = 1;
            }
        }
        isWaitingForStartRing = true;
    }

    private void ResetSimulationState()
    {
        ClearExistingTargets();
        isWaitingForStartRing = false;

        if (blochAnimators != null)
        {
            foreach (var a in blochAnimators)
            {
                if (a != null)
                {
                    a.onTargetReached = null;
                    a.ResetToZero();
                }
            }
        }
        currentStepIndex = 0;
        currentDebugData = null;
        if (probDisplay != null) probDisplay.UpdateProbabilities(0f, 0f, 0f, 0f);
    }

    public void UpdateBlochSpheres(QasmDebugResponse data)
    {
        if (data == null || data.debug_steps == null) return;
        currentDebugData = data;
        currentStepIndex = 0;
        ShowCurrentStep();
    }

    public void AdvanceToNextStep()
    {
        if (isWaitingForStartRing)
        {
            isWaitingForStartRing = false;
            runner.RunSimulation();
            return;
        }

        currentStepIndex++;
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        if (currentDebugData == null || currentDebugData.debug_steps == null) return;

        ClearExistingTargets();
        if (blochAnimators != null && blochAnimators.Length > 0 && blochAnimators[0] != null)
            blochAnimators[0].onTargetReached = null;

        if (currentStepIndex >= currentDebugData.debug_steps.Length || currentStepIndex >= currentBoardGates.Count)
        {
            currentStepIndex = 0;
            if (blochAnimators != null && blochAnimators.Length > 0 && blochAnimators[0] != null)
                blochAnimators[0].ResetToZero();
            return;
        }

        for (int i = 0; i < currentBoardGates.Count; i++)
        {
            currentBoardGates[i].SetGateVisibility(true);
        }

        var stepData = currentDebugData.debug_steps[currentStepIndex];
        if (stepData.qubits == null || stepData.qubits.Length == 0) return;

        float x = stepData.qubits[0].x;
        float y = stepData.qubits[0].y;
        float z = stepData.qubits[0].z;
        string gateName = stepData.gate;

        if (blochAnimators != null && blochAnimators.Length > 0 && blochAnimators[0] != null)
        {
            Vector3 centerPos = blochAnimators[0].transform.position;

            // 【修正步驟光環位置】：使用 Animator 的絕對物理座標，再也不會抓到旋轉中的指針！
            Vector3 absoluteDir = blochAnimators[0].GetWorldDirection(x, y, z);

            pendingRingSpawnPos = centerPos + absoluteDir * interactiveHaloRadius;
            pendingRingRotation = Quaternion.FromToRotation(Vector3.up, absoluteDir);
            pendingX = x; pendingY = y; pendingZ = z;

            blochAnimators[0].onTargetReached = SpawnTargetRing;
            blochAnimators[0].SetState(x, y, z, 1.0f, gateName);
        }

        if (dataTextObj != null)
        {
            var tmp = dataTextObj.GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = $"Step {currentStepIndex + 1}/{currentBoardGates.Count} [{gateName.ToUpper()} Gate]\nQ0: (x:{x:F1}, y:{y:F1}, z:{z:F1})";
        }
    }

    private void SpawnTargetRing()
    {
        if (blochAnimators[0] != null)
            blochAnimators[0].onTargetReached = null;

        if (targetRingPrefab != null)
        {
            GameObject ring = Instantiate(targetRingPrefab, pendingRingSpawnPos, pendingRingRotation);
            QuantumTarget targetScript = ring.GetComponent<QuantumTarget>();
            if (targetScript != null)
            {
                targetScript.targetX = pendingX;
                targetScript.targetY = pendingY;
                targetScript.targetZ = pendingZ;
            }
        }
    }

    public void UpdateUIPanel(QasmProgramIR data)
    {
        if (circuit == null || probDisplay == null) return;
        float p00 = 0f, p01 = 0f, p10 = 0f, p11 = 0f;
        string qasmStr = circuit.GetQASM().ToLower();

        if (qasmStr.Contains("measure") && data != null && data.probabilities != null)
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
        isWaitingForStartRing = false;
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