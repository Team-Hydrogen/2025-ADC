import matplotlib.pyplot as plt
import pandas as pd

df1 = pd.read_csv("./data/starting_data/hsdata.csv")
df2 = pd.read_csv("./data/starting_data/bonusdata.csv")
#MISSION ELAPSED TIME (min),Rx(km)[J2000-EARTH],Ry(km)[J2000-EARTH],Rz(km)[J2000-EARTH]
#plot both in 3d
fig = plt.figure()
ax = fig.add_subplot(111, projection='3d')
ax.plot(df1['Rx(km)[J2000-EARTH]'], df1['Ry(km)[J2000-EARTH]'], df1['Rz(km)[J2000-EARTH]'], label='Trajectory1', color='blue')
ax.plot(df2['Rx(km)[J2000-EARTH]'], df2['Ry(km)[J2000-EARTH]'], df2['Rz(km)[J2000-EARTH]'], label='Trajectory2', color='orange')
ax.set_xlabel('x (km)')
ax.set_ylabel('y (km)')
ax.set_zlabel('z (km)')
plt.legend()
plt.show()