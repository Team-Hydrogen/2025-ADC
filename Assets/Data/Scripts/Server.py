from flask import Flask, request, jsonify, send_file
import Scripts.CislunarTrajectory as CislunarTrajectory
import numpy as np

app = Flask(__name__)

@app.route('/trajectory', methods=['POST'])
def trajectory():
    try:
        data = request.get_json()
        r1 = np.array(data['origin'])
        v1 = np.array(data['origin_v'])
        r2 = np.array(data['destination'])
        v2 = np.array(data['destination_v'])
        tof = data['flightTime']
        start_time = data['startTime']

        CislunarTrajectory.main(r1, r2, v1, v2, tof, start_time)
        return send_file('trajectory.csv', as_attachment=True)
    except Exception as e:
        return jsonify({'error': str(e)}), 400

if __name__ == '__main__':
    app.run(debug=True, port=8080)

