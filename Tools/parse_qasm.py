from flask import Flask, request, jsonify
from qiskit import QuantumCircuit, transpile
from qiskit_aer import AerSimulator
from qiskit.quantum_info import DensityMatrix, partial_trace, Pauli
import numpy as np

app = Flask(__name__)
simulator = AerSimulator()

@app.route('/api/run_qasm_debug', methods=['POST'])
def run_qasm_debug():
    try:
        data = request.get_json()
        if not data or 'qasm' not in data:
            return jsonify({'error': 'No QASM code provided'}), 400

        qasm_code = data['qasm']
        qc = QuantumCircuit.from_qasm_str(qasm_code)
        
        steps_log = []
        current_qc = QuantumCircuit(*qc.qregs, *qc.cregs)
        
        has_measure = False

        # 1. 物理軌跡計算 (跳過測量閘，保留完美的疊加與糾纏態)
        for i, instruction in enumerate(qc.data):
            if instruction.operation.name == 'measure':
                has_measure = True
                continue
                
            current_qc.append(instruction)
            sim_qc = current_qc.copy()
            sim_qc.save_statevector()
            result = simulator.run(transpile(sim_qc, simulator)).result()
            
            sv = result.get_statevector()
            dm = DensityMatrix(sv)
            
            qubit_states = []
            for q in range(qc.num_qubits):
                # 處理糾纏：偏跡掉 (Partial Trace) 其他位元，取得單一 Qubit 的局部狀態
                others = [x for x in range(qc.num_qubits) if x != q]
                rho_q = partial_trace(dm, others) if others else dm
                
                x = float(np.real(rho_q.expectation_value(Pauli('X'))))
                y = float(np.real(rho_q.expectation_value(Pauli('Y'))))
                z = float(np.real(rho_q.expectation_value(Pauli('Z'))))
                radius = float(np.sqrt(x**2 + y**2 + z**2))
                
                qubit_states.append({
                    'qubit': q, 
                    'x': round(x, 4), 
                    'y': round(y, 4), 
                    'z': round(z, 4), 
                    'radius': round(radius, 4)
                })
            
            steps_log.append({'step': i + 1, 'gate': instruction.operation.name, 'qubits': qubit_states})

        # 2. 系統全局機率計算 (滿足需求：只要有觀測，就顯示全系統的機率)
        probabilities = []
        if has_measure:
            # current_qc 已經包含了所有非測量閘，直接取它的最終 Statevector 來算機率
            sim_qc = current_qc.copy()
            sim_qc.save_statevector()
            result = simulator.run(transpile(sim_qc, simulator)).result()
            sv = result.get_statevector()
            
            # sv.probabilities() 會自動算出精準的 [P(00), P(01), P(10), P(11)]
            probs = sv.probabilities()
            probabilities = [float(p) for p in probs]

        return jsonify({
            'probabilities': probabilities, 
            'debug_steps': steps_log
        })

    except Exception as e:
        print(f"Server Error: {str(e)}")
        return jsonify({'error': str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)