#!/usr/bin/env python3
"""
Updated Cislunar Trajectory Simulation

This program simulates a spacecraft trajectory between two points in cislunar space,
taking into account the gravitational effects of both Earth and Moon.
It uses the three-body problem model and numerical integration to calculate
realistic trajectories with specified position and velocity constraints.

The program accepts:
- Start and end position vectors (geocentric x,y,z in kilometers)
- Start and end velocity vectors (in kilometers/second)
- Time of flight (in seconds)
- Starting time (in minutes)

The program outputs position and velocity vectors at 60-second intervals to a CSV file.
"""

import numpy as np
from scipy.integrate import solve_ivp
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D
import csv
import os
from datetime import datetime, timedelta
import pandas as pd

# Physical constants
G = 6.67430e-11  # Gravitational constant (m^3 kg^-1 s^-2)
M_EARTH = 5.97217e24  # Earth mass (kg)
M_MOON = 7.34767e22  # Moon mass (kg)
R_EARTH_MOON = 384400  # Average Earth-Moon distance (km)

# Conversion factors
KM_TO_M = 1000  # Convert kilometers to meters
M_TO_KM = 0.001  # Convert meters to kilometers

# Earth is at the origin (0,0,0)
EARTH_POSITION = np.array([0, 0, 0])

# Moon position relative to Earth (simplified as fixed for now)
# In a more realistic model, this would be time-dependent
MOON_POSITION = np.array([R_EARTH_MOON, 0, 0])

def three_body_equations(t, state, earth_mass, moon_mass):
    """
    Defines the differential equations for the three-body problem.
    
    Arguments:
        t : float
            Current time
        state : array
            Current state [x, y, z, vx, vy, vz]
            Note: x, y, z are in kilometers, vx, vy, vz are in kilometers/second
        earth_mass : float
            Mass of Earth
        moon_mass : float
            Mass of Moon
    
    Returns:
        dstate : array
            Derivatives [vx, vy, vz, ax, ay, az]
    """
    # Unpack state vector
    x, y, z, vx, vy, vz = state
    
    # Spacecraft position (in kilometers)
    r_sc = np.array([x, y, z])
    
    # Calculate distances (in kilometers)
    r_sc_earth = r_sc - EARTH_POSITION
    r_sc_moon = r_sc - MOON_POSITION
    
    # Calculate magnitudes of distances (in kilometers)
    r_sc_earth_mag = np.linalg.norm(r_sc_earth)
    r_sc_moon_mag = np.linalg.norm(r_sc_moon)
    
    # Convert distances to meters for acceleration calculation
    r_sc_earth_m = r_sc_earth * KM_TO_M
    r_sc_moon_m = r_sc_moon * KM_TO_M
    r_sc_earth_mag_m = r_sc_earth_mag * KM_TO_M
    r_sc_moon_mag_m = r_sc_moon_mag * KM_TO_M
    
    # Calculate accelerations due to Earth and Moon (in m/s^2)
    a_earth_m = -G * earth_mass * r_sc_earth_m / (r_sc_earth_mag_m**3)
    a_moon_m = -G * moon_mass * r_sc_moon_m / (r_sc_moon_mag_m**3)
    
    # Total acceleration (in m/s^2)
    a_total_m = a_earth_m + a_moon_m
    
    # Convert acceleration to km/s^2
    a_total = a_total_m * M_TO_KM
    
    # Return derivatives
    return [vx, vy, vz, a_total[0], a_total[1], a_total[2]]

def calculate_trajectory(start_pos, end_pos, start_vel, end_vel, flight_time_seconds, start_time_minutes):
    """
    Calculate a trajectory between two points in cislunar space with specified velocity constraints.
    
    Arguments:
        start_pos : array
            Starting position [x, y, z] in kilometers with (0,0,0) as Earth center
        end_pos : array
            Final position [x, y, z] in kilometers with (0,0,0) as Earth center
        start_vel : array
            Starting velocity [vx, vy, vz] in kilometers/second
        end_vel : array
            Final velocity [vx, vy, vz] in kilometers/second
        flight_time_seconds : float
            Time of flight in seconds
        start_time_minutes : float
            Starting time in minutes
    
    Returns:
        times : array
            Time points
        positions : array
            Position vectors at each time point
        velocities : array
            Velocity vectors at each time point
    """
    # Convert start time to seconds
    start_time_seconds = start_time_minutes * 60
    
    # Initial state vector [x, y, z, vx, vy, vz]
    initial_state = np.concatenate((start_pos, start_vel))
    
    # Time span for integration
    t_span = (0, flight_time_seconds)
    
    # Time points for output (every 60 seconds)
    t_eval = np.arange(0, flight_time_seconds + 1, 60)
    
    # Solve the differential equations
    solution = solve_ivp(
        fun=lambda t, y: three_body_equations(t, y, M_EARTH, M_MOON),
        t_span=t_span,
        y0=initial_state,
        t_eval=t_eval,
        method='RK45',
        rtol=1e-8,
        atol=1e-8
    )
    
    # Extract positions and velocities
    positions = solution.y[:3, :].T  # x, y, z
    velocities = solution.y[3:, :].T  # vx, vy, vz
    
    # Adjust times to include start_time_seconds
    times = solution.t + start_time_seconds
    
    return times, positions, velocities

def save_to_csv(times, positions, velocities, filename="trajectory_data.csv"):
    """
    Save trajectory data to a CSV file.
    
    Arguments:
        times : array
            Time points in seconds
        positions : array
            Position vectors at each time point (in kilometers)
        velocities : array
            Velocity vectors at each time point (in kilometers/second)
        filename : str
            Output filename
    """
    with open(filename, 'w', newline='') as csvfile:
        writer = csv.writer(csvfile)
        
        # Write header
        writer.writerow(['Time (s)', 'X (km)', 'Y (km)', 'Z (km)', 'VX (km/s)', 'VY (km/s)', 'VZ (km/s)'])
        
        # Write data
        for i in range(len(times)):
            writer.writerow([
                times[i],
                positions[i, 0],
                positions[i, 1],
                positions[i, 2],
                velocities[i, 0],
                velocities[i, 1],
                velocities[i, 2]
            ])

def plot_trajectory(positions, earth_pos=EARTH_POSITION, moon_pos=MOON_POSITION):
    """
    Plot the trajectory in 3D space.
    
    Arguments:
        positions : array
            Position vectors (in kilometers)
        earth_pos : array
            Earth position (in kilometers)
        moon_pos : array
            Moon position (in kilometers)
    """
    df1 = pd.read_csv("./data/hsdata_altered.csv")
    df2 = pd.read_csv("./data/bonusdata_altered.csv")
    fig = plt.figure(figsize=(10, 8))
    ax = fig.add_subplot(111, projection='3d')
    
    # Plot trajectory
    ax.plot(positions[:, 0], positions[:, 1], positions[:, 2], 'b-', label='Spacecraft Trajectory')
    ax.plot(df1['Rx(km)[J2000-EARTH]'], df1['Ry(km)[J2000-EARTH]'], df1['Rz(km)[J2000-EARTH]'], label='Trajectory1', color='blue')
    ax.plot(df2['Rx(km)[J2000-EARTH]'], df2['Ry(km)[J2000-EARTH]'], df2['Rz(km)[J2000-EARTH]'], label='Trajectory2', color='orange')
    
    # Plot Earth and Moon
    ax.scatter(earth_pos[0], earth_pos[1], earth_pos[2], color='blue', s=100, label='Earth')
    ax.scatter(moon_pos[0], moon_pos[1], moon_pos[2], color='gray', s=50, label='Moon')
    
    # Plot start and end points
    ax.scatter(positions[0, 0], positions[0, 1], positions[0, 2], color='green', s=50, label='Start')
    ax.scatter(positions[-1, 0], positions[-1, 1], positions[-1, 2], color='red', s=50, label='End')
    
    # Set labels and title
    ax.set_xlabel('X (km)')
    ax.set_ylabel('Y (km)')
    ax.set_zlabel('Z (km)')
    ax.set_title('Spacecraft Trajectory in Cislunar Space')
    
    # Add legend
    ax.legend()
    
    # # Set equal aspect ratio
    # max_range = np.max([
    #     np.max(positions[:, 0]) - np.min(positions[:, 0]),
    #     np.max(positions[:, 1]) - np.min(positions[:, 1]),
    #     np.max(positions[:, 2]) - np.min(positions[:, 2])
    # ])
    # mid_x = (np.max(positions[:, 0]) + np.min(positions[:, 0])) / 2
    # mid_y = (np.max(positions[:, 1]) + np.min(positions[:, 1])) / 2
    # mid_z = (np.max(positions[:, 2]) + np.min(positions[:, 2])) / 2
    # ax.set_xlim(mid_x - max_range/2, mid_x + max_range/2)
    # ax.set_ylim(mid_y - max_range/2, mid_y + max_range/2)
    # ax.set_zlim(mid_z - max_range/2, mid_z + max_range/2)
    
    plt.tight_layout()
    plt.savefig('trajectory_plot.png')
    plt.show()

def main(start_pos, end_pos, start_vel, end_vel, flight_time_seconds, start_time_minutes, plot = False):
    
    print("Calculating trajectory...")
    
    # Calculate trajectory
    times, positions, velocities = calculate_trajectory(
        start_pos, end_pos, start_vel, end_vel, flight_time_seconds, start_time_minutes
    )
    
    # Save results to CSV
    output_file = "trajectory.csv"
    save_to_csv(times, positions, velocities, output_file)
    
    print(f"Trajectory data saved to {output_file}")
    
    if plot:
        plot_trajectory(positions)

if __name__ == "__main__":
    main([-33842.83999,-32815.57848,-22591.45079], [-133457.4827,-56705.97272,-58563.79913], [2.262750165,0.631038263,0.202543076], [1.71849505,0.53810259,0.272918458], 900, 1243.650291, plot = True)
