# This file is only used when running a scene in Unity to visualize the PPI images
import websocket
import json
import numpy as np
import matplotlib.pyplot as plt
from matplotlib.animation import FuncAnimation
import threading
import argparse
import time

fig, ax = plt.subplots(figsize=(10, 10), subplot_kw=dict(projection='polar'))
im = None
cbar = None
scatter = None

radarID = None
reconnect_delay = 5  # Delay in seconds before attempting to reconnect

color = False
clip = None


def create_ppi_plot(data, azimuth, range_bins, ships, radar_range):
    global im, cbar, scatter, color

    if clip:
        mean = np.mean(data)
        std = np.std(data)
        data = np.clip(data, 0, min(2000, mean + clip * std))

    if color:
        data = np.where(data != 0, 1, data)
    vmin = data.min()
    vmax = data.max()

    # Convert polar coordinates to cartesian
    theta = np.radians(azimuth)
    r, theta = np.meshgrid(range_bins, theta)

    # Plot the data
    if im is None:
        im = ax.pcolormesh(theta, r, data, cmap='magma', vmin=vmin, vmax=vmax)

        # Customize the plot
        ax.set_theta_zero_location("N")
        ax.set_theta_direction(-1)
        ax.set_rlabel_position(0)
        ax.set_title("PPI Plot")

        # Add a colorbar
        cbar = plt.colorbar(im, ax=ax)
        cbar.set_label('Intensity')
    else:
        im.set_array(data.ravel())
        im.set_clim(vmin=vmin, vmax=vmax)

    # Plot ship points
    if ships:
        ship_thetas = np.radians([ship['Azimuth'] for ship in ships])
        ship_distances = [ship['Distance'] /
                          int(radar_range) * data.shape[1] for ship in ships]

        if scatter is None:
            scatter = ax.scatter(ship_thetas, ship_distances,
                                 c='cyan', s=10, zorder=5)
        else:
            scatter.set_offsets(np.column_stack((ship_thetas, ship_distances)))

    return im, scatter


latest_data = None
latest_ships = None
data_lock = threading.Lock()
radar_range = None


def on_message(ws, message):
    global latest_data, latest_ships, radar_range
    data = json.loads(message)
    ppi = data.get('PPI', 'NA')
    ships = data.get('ships', [])
    r_range = data.get('range', 5000)
    if ppi == "NA":
        return
    ppi = np.array(ppi)
    print(np.unravel_index(ppi.argmax(), ppi.shape))

    with data_lock:
        latest_data = ppi
        latest_ships = ships
        radar_range = r_range


def on_error(ws, error):
    print(f"Error: {error}")


def on_close(ws, close_status_code, close_msg):
    print("Connection closed")


def on_open(ws):
    print("Connection opened")


def run_websocket():
    while True:
        try:
            ws = websocket.WebSocketApp(f"ws://localhost:8080/radar{radarID}",
                                        on_message=on_message,
                                        on_error=on_error,
                                        on_close=on_close,
                                        on_open=on_open)
            ws.run_forever()
        except Exception as e:
            print(f"WebSocket error: {e}")

        print(f"Connection lost. Reconnecting in {reconnect_delay} seconds...")
        time.sleep(reconnect_delay)


def update_plot(frame):
    global latest_data, latest_ships
    with data_lock:
        if latest_data is not None:
            num_azimuth, num_range = latest_data.shape
            azimuth = np.linspace(0, 360, num_azimuth)
            range_bins = np.linspace(0, num_range, num_range)

            return create_ppi_plot(latest_data, azimuth, range_bins, latest_ships, radar_range)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()

    # Add arguments
    parser.add_argument('-r', type=int, default=0, help='Radar ID')
    parser.add_argument('-c', '--color', action='store_true',
                        help='Enable color output')
    parser.add_argument('--clip', type=int, default=0,
                        help='Clip standard deviations')
    args = parser.parse_args()

    if isinstance(args.r, int):
        radarID = args.r
        print(radarID)
    else:
        print("Invalid Radar ID")

    if isinstance(args.clip, int) and args.clip != 0:
        clip = args.clip

    if args.color:
        color = True

    # Start WebSocket connection in a separate thread
    websocket_thread = threading.Thread(target=run_websocket)
    websocket_thread.daemon = True
    websocket_thread.start()

    # Set up the animation
    ani = FuncAnimation(fig, update_plot, interval=100, blit=False)
    plt.show()
