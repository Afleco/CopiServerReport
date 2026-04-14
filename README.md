# CopiServerReport (CSR) 🛠️📊

![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)
![C#](https://img.shields.io/badge/C%23-Language-239120?logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-WinForms-512BD4?logo=dotnet&logoColor=white)
![Windows](https://img.shields.io/badge/Windows-Server-0078D6?logo=windows&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-yellow.svg)

**CopiServerReport (CSR)** is a Windows Forms application designed to streamline server maintenance and health auditing for IT professionals. It centralizes deep hardware diagnostics, Windows Update management, and automated PDF reporting into a single interface.

The core design principle of CSR is **safety-first execution**. It provides technicians with a comprehensive system analysis to ensure stability before applying critical updates or executing maintenance tasks in production environments.

## Features

### 🔄 Windows Update Management
* Integrates directly with the native Windows Update Agent (WUA) API.
* **Risk Assessment:** Analyzes update metadata to identify major rollups or updates requiring a system reboot prior to installation, reducing the risk of unexpected downtime.
* Implements built-in cooldowns to prevent WSUS timeout errors (`0x80240438`) during intensive patching processes.

### 🔍 System & Hardware Diagnostics
* **Advanced S.M.A.R.T. Telemetry:** Integrates `LibreHardwareMonitor` to read accurate health percentages, temperatures, and power-on hours for NVMe, SATA, and USB drives.
* **Driver Auditing:** Scans the registry (`Win32_PnPEntity`) to identify devices with error codes and retrieves missing driver details.
* **Security Detection:** Identifies kernel-level EDRs (e.g., CrowdStrike, SentinelOne) and standard antivirus services, bypassing basic Windows Security Center limitations.
* Maps active network interfaces, mapped network drive capacity, RAM usage, and active user sessions.

### 🏢 DF-Server Auditing Module
Includes a dedicated module for auditing the DF-Server document management system:
* Automatically detects the installed software version by querying the Windows Registry, with a fallback mechanism to read `.exe` metadata if registry keys are missing or corrupt.
* Monitors digital certificate expiration dates.
* Tracks available DF-Signature credits.

### 📄 Automated PDF Reporting
* Generates structured, branded PDF audit reports using `QuestPDF`.
* **Report Customization:** Allows technicians to selectively omit acknowledged driver errors or pending updates from the final document, ensuring the client receives an accurate and relevant health summary.
  
## ⚙️ Technical Details
* **Language/Framework:** C# (.NET WinForms)
* **Architecture:** Strictly decoupled logic. All resource-intensive operations (WMI queries, downloads, file cleanup) are executed asynchronously via `Task.Run()` on MTA background threads to ensure UI responsiveness.
* **Dependencies:**
  * `QuestPDF` (Reporting engine)
  * `LibreHardwareMonitorLib` (Hardware telemetry)

## Installation & Usage 🧑🏻‍💻

1. Clone the repository:
   ```bash
   git clone https://github.com/Afleco/CopiServerReport.git
2. Open the solution in Visual Studio and restore the required NuGet packages.

3. Run as Administrator: The application requires elevated privileges to query kernel-level SMART data, interact with the Windows Update API, and manage global temporary files.

## Contributing 🫱🏻‍🫲🏻 
This tool was developed to address specific workflows in server maintenance. Contributions, bug reports, and pull requests to improve hardware detection or expand functionality are welcome.
