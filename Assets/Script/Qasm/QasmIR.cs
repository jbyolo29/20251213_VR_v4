using System;

[Serializable]
public class QasmProgramIR
{
    public string status;
    public string message;      // 修復 QasmRunner 的報錯
    public int n_qubits;
    public double[] probabilities;
    public ComplexState[] raw_statevector; // 修復 Controller 的報錯
}

[Serializable]
public class ComplexState
{
    public string state;
    public double real;
    public double imag;
    public double probability;
    public double phase; // 儲存 Phi 相位角
}