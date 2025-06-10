# Ping Plugin for InfoPanel

A plugin for InfoPanel to display the average ping to a list of servers.

## Installation and Setup
Follow these steps to get the ping plugin working with InfoPanel:

1. **Download the plugin**:
   - Download the latest release \*.zip file (`PingPlugin-vX.X.X.zip`) from the [GitHub Releases page](https://github.com/markuskonojacki/InfoPanel.Ping/releases).

2. **Import the Plugin into InfoPanel**:
   - Open the InfoPanel app.
   - Navigate to the **Plugins** page.
   - Click **Import Plugin Archive**, then select the downloaded ZIP file.
   - InfoPanel will extract and install the plugin.

3. **Configure the Plugin**:
   - On the Plugins page, click **Open Plugins Folder** to locate the plugin files.
   - Close InfoPanel.
   - Open `InfoPanel.Ping.dll.ini` in a text editor (e.g., Notepad).
   - Add the desired number of IPs or server addresses as a comma seperated list.
   - Save and close the file.

## Configuration example
Define the IP or server list without spaces and comma separated.
- **`InfoPanel.Ping.dll.ini`**:
  ```ini
  [Ping Plugin]
  Servers=1.1.1.1,4.2.2.2,9.9.9.9
  RefreshTimer=10
