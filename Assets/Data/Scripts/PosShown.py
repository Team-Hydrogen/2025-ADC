import numpy as np
import matplotlib.pyplot as plt
import pandas as pd
import time

df = pd.read_csv("bonusdata.csv")
df2 = pd.read_csv("hsdata.csv")
print(df.head())

# apply normalization techniques 
for column in df.columns[1:4]: 
    df[column] = (df[column] - df[column].min()) / (df[column].max() - df[column].min()) 

for column in df2.columns[1:4]: 
    df2[column] = (df2[column] - df2[column].min()) / (df2[column].max() - df2[column].min()) 

print(df2.head())

fig, axs = plt.subplots(2, subplot_kw=dict(projection='3d'))
# ax = fig.add_subplot(projection='3d')
# ax2 = fig.add_subplot(projection='3d')
# axs[0].plot(df["Rx(km)[J2000-EARTH]"], df["Ry(km)[J2000-EARTH]"], df["Rz(km)[J2000-EARTH]"])
axs[0].plot(df["Rx(km)[J2000-EARTH]"], df["Ry(km)[J2000-EARTH]"], df["Rz(km)[J2000-EARTH]"])
axs[1].plot(df["Vx(km/s)[J2000-EARTH]"], df["Vy(km/s)[J2000-EARTH]"], df["Vz(km/s)[J2000-EARTH]"])


plt.show()



# def next_frame(iteration):
#     # print([df.iloc[iteration]["Rx(km)[J2000-EARTH]"], df.iloc[iteration]["Ry(km)[J2000-EARTH]"], df.iloc[iteration]["Rz(km)[J2000-EARTH]"]])
#     return [df.iloc[iteration]["Rx(km)[J2000-EARTH]"], df.iloc[iteration]["Ry(km)[J2000-EARTH]"], df.iloc[iteration]["Rz(km)[J2000-EARTH]"]]

# fig = plt.figure()
# ax = p3.Axes3D(fig)
# ani = animation.FuncAnimation(fig, next_frame, len(df.index),
#                                     interval=50, blit=False, repeat=True)
# Writer = animation.writers['ffmpeg']
# writer = Writer(fps=30, bitrate=1800)
# ani.save('3d-scatted-animated.mp4', writer=writer)

# plt.show()