using System;
using System.Collections.Generic;
using System.Numerics;

namespace QuantumViz.Core
{
    public class QasmSimulator
    {
        readonly Random rng = new();

        public (QuantumState state, int[] cbits) Run(string qasm, int qubits, int cbits)
        {
            var cmds = QasmMiniParser.Parse(qasm);
            var qs = new QuantumState(qubits);
            var classical = new int[cbits];

            foreach (var c in cmds)
            {
                switch (c.Op)
                {
                    case "h": qs.ApplySingleQubit(GateLibrary.H, c.Qubits[0]); break;
                    case "x": qs.ApplySingleQubit(GateLibrary.X, c.Qubits[0]); break;
                    case "y": qs.ApplySingleQubit(GateLibrary.Y, c.Qubits[0]); break;
                    case "z": qs.ApplySingleQubit(GateLibrary.Z, c.Qubits[0]); break;
                    case "s": qs.ApplySingleQubit(GateLibrary.S, c.Qubits[0]); break;
                    case "sdg": qs.ApplySingleQubit(GateLibrary.Sdg, c.Qubits[0]); break;
                    case "t": qs.ApplySingleQubit(GateLibrary.T, c.Qubits[0]); break;
                    case "tdg": qs.ApplySingleQubit(GateLibrary.Tdg, c.Qubits[0]); break;
                    case "rx": qs.ApplySingleQubit(GateLibrary.Rx(c.Param), c.Qubits[0]); break;
                    case "ry": qs.ApplySingleQubit(GateLibrary.Ry(c.Param), c.Qubits[0]); break;
                    case "rz": qs.ApplySingleQubit(GateLibrary.Rz(c.Param), c.Qubits[0]); break;
                    case "cx": qs.ApplyCX(c.Qubits[0], c.Qubits[1]); break;
                    case "cz": qs.ApplyCZ(c.Qubits[0], c.Qubits[1]); break;
                    case "measure":
                        {
                            int m = qs.MeasureZ(c.Qubits[0], rng);
                            if (c.Cbits.Count > 0 && c.Cbits[0] < classical.Length) classical[c.Cbits[0]] = m;
                            break;
                        }
                    case "reset": qs.Reset(c.Qubits[0], rng); break;
                        // nop / ¨ä¥L©¿²¤
                }
            }

            qs.Normalize();
            return (qs, classical);
        }
    }
}
