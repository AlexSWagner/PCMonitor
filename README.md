# PC Performance Monitor

A Windows desktop application that provides real-time monitoring of system performance metrics.

## Features

- CPU Usage and Temperature monitoring
- GPU Usage and Temperature monitoring (NVIDIA)
- RAM Usage tracking
- Disk Read/Write Speed monitoring
- Configurable refresh rate
- Always-on-top option
- Progress bar visualizations for easy reading

## Requirements

- Windows Operating System
- .NET Framework
- Administrator privileges (required for hardware monitoring)
- NVIDIA GPU for GPU monitoring features

## Libraries Used

- LibreHardwareMonitor - For hardware monitoring
- System.Management - For system information
- Windows Forms - For the user interface

## Installation

1. Download the latest release
2. Run the application as Administrator
3. Monitor your system's performance in real-time

## Usage

- Launch the application
- The interface will display current:
  - CPU Usage and Temperature
  - GPU Usage and Temperature
  - Available RAM
  - Disk Read/Write Speeds
- Use the refresh rate control to adjust update frequency
- Toggle "Always on Top" to keep the window visible

## Development

This project was developed using:
- Visual Studio 2022
- C# Windows Forms
- .NET Framework

## Future Updates

Planned features:
- Network usage monitoring
- System uptime display
- Data logging capabilities
- Customizable alert thresholds

## Author

Alex Wagner
