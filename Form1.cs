/*
 * PC Performance Monitor
 * A real-time system monitoring application that tracks CPU, GPU, RAM, and disk metrics
 * Author: Alex Wagner
 * Created: 10/20/2024
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Management;
using LibreHardwareMonitor.Hardware;

namespace PCMonitor
{
    public partial class Form1 : Form
    {
        // Performance counters and UI controls
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private PerformanceCounter diskReadCounter;
        private PerformanceCounter diskWriteCounter;
        private NumericUpDown refreshRateControl;
        private ProgressBar cpuProgressBar;
        private ProgressBar ramProgressBar;
        private ProgressBar gpuTempProgressBar;
        private ProgressBar gpuUsageProgressBar;
        private CheckBox alwaysOnTopCheckBox;
        private ulong totalMemory;
        private LibreHardwareMonitor.Hardware.Computer computer;
        private Label lblCPUTemp;
        private Label lblGPUTemp;
        private Label lblGPUUsage;
        private System.Windows.Forms.Timer updateTimer;

        // Initialize form and set background color
        public Form1()
        {
            InitializeComponent();
            this.BackColor = Color.LightGray;
        }

        // Initialize performance counters for system metrics
        private void InitializeCounters()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
        }

        // Set up UI elements and initialize monitoring systems
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Size = new Size(400, 400);  // Increased height to fit new controls
            int padding = 10;
            int labelWidth = 180;
            int progressBarWidth = 150;
            int controlHeight = 20;

            // Title
            Label titleLabel = new Label
            {
                Text = "PC Performance Monitor",
                Font = new Font("Arial", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(padding, padding)
            };
            this.Controls.Add(titleLabel);

            // CPU
            lblCPU.Location = new Point(padding, titleLabel.Bottom + padding);
            lblCPU.Size = new Size(labelWidth, controlHeight);
            cpuProgressBar = new ProgressBar
            {
                Location = new Point(lblCPU.Right + padding, lblCPU.Top),
                Size = new Size(progressBarWidth, controlHeight)
            };
            this.Controls.Add(cpuProgressBar);

            // RAM
            lblRAM.Location = new Point(padding, lblCPU.Bottom + padding);
            lblRAM.Size = new Size(labelWidth, controlHeight);
            ramProgressBar = new ProgressBar
            {
                Location = new Point(lblRAM.Right + padding, lblRAM.Top),
                Size = new Size(progressBarWidth, controlHeight)
            };
            this.Controls.Add(ramProgressBar);

            // Disk Read
            lblDiskRead.Location = new Point(padding, lblRAM.Bottom + padding);
            lblDiskRead.Size = new Size(labelWidth, controlHeight);

            // Disk Write
            lblDiskWrite.Location = new Point(padding, lblDiskRead.Bottom + padding);
            lblDiskWrite.Size = new Size(labelWidth, controlHeight);

            // CPU Temperature Label
            lblCPUTemp = new Label
            {
                Location = new Point(padding, lblDiskWrite.Bottom + padding),
                Size = new Size(labelWidth, controlHeight),
                Font = new Font("Arial", 10),
                ForeColor = Color.DarkBlue
            };
            this.Controls.Add(lblCPUTemp);

            // GPU Temperature
            lblGPUTemp = new Label
            {
                Location = new Point(padding, lblCPUTemp.Bottom + padding),
                Size = new Size(labelWidth, controlHeight),
                Font = new Font("Arial", 10),
                ForeColor = Color.DarkBlue
            };
            this.Controls.Add(lblGPUTemp);

            gpuTempProgressBar = new ProgressBar
            {
                Location = new Point(lblGPUTemp.Right + padding, lblGPUTemp.Top),
                Size = new Size(progressBarWidth, controlHeight),
                Maximum = 100
            };
            this.Controls.Add(gpuTempProgressBar);

            // GPU Usage
            lblGPUUsage = new Label
            {
                Location = new Point(padding, lblGPUTemp.Bottom + padding),
                Size = new Size(labelWidth, controlHeight),
                Font = new Font("Arial", 10),
                ForeColor = Color.DarkBlue
            };
            this.Controls.Add(lblGPUUsage);

            gpuUsageProgressBar = new ProgressBar
            {
                Location = new Point(lblGPUUsage.Right + padding, lblGPUUsage.Top),
                Size = new Size(progressBarWidth, controlHeight),
                Maximum = 100
            };
            this.Controls.Add(gpuUsageProgressBar);

            // Refresh Rate
            refreshRateControl = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 60,
                Value = 1,
                Location = new Point(padding, lblGPUUsage.Bottom + padding),
                Size = new Size(60, controlHeight)
            };
            this.Controls.Add(refreshRateControl);

            Label refreshLabel = new Label
            {
                Text = "Refresh rate (seconds):",
                AutoSize = true,
                Location = new Point(refreshRateControl.Right + padding, refreshRateControl.Top + 2)
            };
            this.Controls.Add(refreshLabel);

            // Always on Top
            alwaysOnTopCheckBox = new CheckBox
            {
                Text = "Always on Top",
                AutoSize = true,
                Location = new Point(padding, refreshRateControl.Bottom + padding)
            };
            alwaysOnTopCheckBox.CheckedChanged += AlwaysOnTopCheckBox_CheckedChanged;
            this.Controls.Add(alwaysOnTopCheckBox);

            // Set fonts and colors
            Font labelFont = new Font("Arial", 10);
            Color labelColor = Color.DarkBlue;
            foreach (Control control in this.Controls)
            {
                if (control is Label label && label != titleLabel)
                {
                    label.Font = labelFont;
                    label.ForeColor = labelColor;
                }
            }

            GetTotalMemory();
            InitializeCounters();

            // Initialize LibreHardwareMonitor
            computer = new LibreHardwareMonitor.Hardware.Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true
            };
            computer.Open();

            // Force an initial update
            foreach (var hardware in computer.Hardware)
            {
                hardware.Update();
            }

            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = (int)(refreshRateControl.Value * 1000);
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            refreshRateControl.ValueChanged += refreshRateControl_ValueChanged;
        }

        // Retrieve total system RAM using WMI
        private void GetTotalMemory()
        {
            try
            {
                ObjectQuery wql = new ObjectQuery("SELECT * FROM Win32_ComputerSystem");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(wql);
                ManagementObjectCollection results = searcher.Get();

                foreach (ManagementObject result in results)
                {
                    totalMemory = Convert.ToUInt64(result["TotalPhysicalMemory"]);
                    break; // Only need first result
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error getting total memory: " + ex.Message);
                totalMemory = 1; // Prevent division by zero
            }
        }

        // Update all performance metrics and UI elements
        private void UpdatePerformanceData()
        {
            // Get current performance metrics
            float cpuUsage = cpuCounter.NextValue();
            float availableMemory = ramCounter.NextValue();
            float diskReadSpeed = diskReadCounter.NextValue() / 1024 / 1024; // Convert to MB/s
            float diskWriteSpeed = diskWriteCounter.NextValue() / 1024 / 1024; // Convert to MB/s
            float cpuTemp = GetCPUTemperature();
            var (gpuTemp, gpuUsage) = GetGPUInfo();

            // Update UI labels
            lblCPU.Text = $"CPU Usage: {cpuUsage:F1}%";
            lblRAM.Text = $"Available Memory: {availableMemory:F0} MB";
            lblDiskRead.Text = $"Disk Read: {diskReadSpeed:F2} MB/s";
            lblDiskWrite.Text = $"Disk Write: {diskWriteSpeed:F2} MB/s";
            lblCPUTemp.Text = cpuTemp > 0
                ? $"CPU Temperature: {cpuTemp:F1}°C"
                : "CPU Temperature: Not available";
            lblGPUTemp.Text = gpuTemp > 0
                ? $"GPU Temperature: {gpuTemp:F1}°C"
                : "GPU Temperature: Not available";
            lblGPUUsage.Text = $"GPU Usage: {gpuUsage:F1}%";

            // Update progress bars
            cpuProgressBar.Value = (int)cpuUsage;
            ramProgressBar.Value = (int)((totalMemory - (ulong)availableMemory * 1024 * 1024) / (float)totalMemory * 100);
            gpuTempProgressBar.Value = (int)gpuTemp;
            gpuUsageProgressBar.Value = (int)gpuUsage;
        }

        // Retrieve CPU temperature using LibreHardwareMonitor
        private float GetCPUTemperature()
        {
            try
            {
                var cpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
                if (cpu != null)
                {
                    cpu.Update();
                    foreach (var sensor in cpu.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature &&
                            (sensor.Name.Contains("Package") || sensor.Name.Contains("Core #1")))
                        {
                            return sensor.Value ?? 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting CPU temperature: {ex.Message}");
            }
            return 0;
        }

        // Retrieve GPU temperature and usage information
        private (float temperature, float usage) GetGPUInfo()
        {
            float temperature = 0;
            float usage = 0;

            try
            {
                var gpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia);
                if (gpu != null)
                {
                    gpu.Update();
                    foreach (var sensor in gpu.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("GPU Core"))
                        {
                            temperature = sensor.Value ?? 0;
                        }
                        else if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("GPU Core"))
                        {
                            usage = sensor.Value ?? 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting GPU info: {ex.Message}");
            }

            return (temperature, usage);
        }

        // Toggle always-on-top functionality
        private void AlwaysOnTopCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = alwaysOnTopCheckBox.Checked;
        }

        // Clean up resources on form close
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            computer.Close();
        }

        // Timer event handler for updating display
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdatePerformanceData();
        }

        // Handle refresh rate changes
        private void refreshRateControl_ValueChanged(object sender, EventArgs e)
        {
            updateTimer.Interval = (int)(refreshRateControl.Value * 1000);
        }
    }
}
