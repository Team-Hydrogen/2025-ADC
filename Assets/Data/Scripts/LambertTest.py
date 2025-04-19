import numpy as np
import pandas as pd
from lamberthub import izzo2015
from scipy.integrate import solve_ivp
from matplotlib import pyplot as plt

MU_EARTH = 398600.4418  # km^3/s^2
MU_MOON = 4902.800066  # km^3/s^2
EARTH_POSITION = np.array([0.0, 0.0, 0.0])
MOON_POSITION = np.array([384400.0, 0.0, 0.0])  # km (approximate average distance from Earth)

# def two_body_equations(t, y, mu):
#     r = y[:3]
#     v = y[3:]
#     r_norm = np.linalg.norm(r)
#     a = -mu * r / r_norm**3
#     return np.hstack((v, a))

def three_body_equations(t, y, mu_earth, mu_moon, moon_position):
    r = y[:3]  # Position vector of the spacecraft
    v = y[3:]  # Velocity vector of the spacecraft
    
    # Gravitational acceleration due to Earth
    r_earth = r - EARTH_POSITION
    r_earth_norm = np.linalg.norm(r_earth)
    a_earth = -mu_earth * r_earth / r_earth_norm**3
    
    # Gravitational acceleration due to Moon
    r_moon = r - moon_position
    r_moon_norm = np.linalg.norm(r_moon)
    a_moon = -mu_moon * r_moon / r_moon_norm**3
    
    # Total acceleration
    a = a_earth + a_moon
    
    return np.hstack((v, a))


def propagate_orbit(r0, v0, tof, mu_earth, mu_moon, moon_position):
    y0 = np.hstack((r0, v0))
    t_span = (0, tof)
    t_eval = np.arange(0, tof + 1, 1)  # Evaluate every second
    sol = solve_ivp(three_body_equations, t_span, y0, args=(mu_earth, mu_moon, moon_position), t_eval=t_eval, rtol=1e-9, atol=1e-9)
    return sol.t, sol.y.T

def main(r1, r2, tof,start_time):
    
    # Solve Lambert's problem to find initial and final velocities
    v1_lambert, v2_lambert = izzo2015(MU_EARTH, r1, r2, tof)
    
    # Propagate the orbit from the initial conditions
    times, states = propagate_orbit(r1, v1_lambert, tof, MU_EARTH, MU_MOON, MOON_POSITION)
    
    # Save the results to a CSV file
    df = pd.DataFrame(states, columns=['x (km)', 'y (km)', 'z (km)', 'vx (km/s)', 'vy (km/s)', 'vz (km/s)'])
    df.insert(0, 'Time (min)', (times / 60)+start_time)  # Convert time to minutes
    df.to_csv('trajectory.csv', index=False)
    print("Trajectory data saved to 'trajectory.csv'.")
    
    # 3D plot of trajectory
    # fig = plt.figure()
    # ax = fig.add_subplot(111, projection='3d')
    # ax.plot(states[:, 0], states[:, 1], states[:, 2])
    # ax.set_xlabel('x (km)')
    # ax.set_ylabel('y (km)')
    # ax.set_zlabel('z (km)')
    # plt.show()