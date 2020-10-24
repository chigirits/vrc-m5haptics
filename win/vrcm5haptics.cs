using System;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace vrcm5haptics
{

    class MainForm : Form
    {
        const int baudRate = 115200;
        static SerialPort serial = null;

        const int realMinLevel = 64;
        const int realMaxLevel = 512;
        const int levelDivision = 16;
        const double curveExp = 1.0;
        ComboBox portSelector;
        Button connectButton;
        TrackBar levelTrackBar;

        public MainForm()
        {
            Text = "VRC-M5Haptics";

            var x = 10;
            var y = 10;
            var h = 21;
            var p = h + 10;

            portSelector = new ComboBox()
            {
                Location = new System.Drawing.Point(x, y),
                Name = "COMPort",
                Size = new System.Drawing.Size(90, h),
                TabIndex = 0,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            var ports = SerialPort.GetPortNames().OrderBy(n => n);
            portSelector.Items.AddRange(ports.ToArray());
            var n = portSelector.Items.Count;
            if (0 < n) portSelector.SelectedIndex = n - 1;
            Controls.Add(portSelector);

            connectButton = new Button()
            {
                Text = "接続",
                Location = new Point(x+100, y),
                Size = new Size(90, h),
            };
            connectButton.Click += new EventHandler(ConnectButtonClicked);
            Controls.Add(connectButton);
            y += p;

            levelTrackBar = new TrackBar()
            {
                Location = new Point(x, y),
                Size = new Size(256, h),
                Maximum = levelDivision,
                TickFrequency = levelDivision / 4,
                LargeChange = levelDivision / 4,
                SmallChange = 1,
                Enabled = false,
                TabStop = false,
            };
            levelTrackBar.Scroll += new System.EventHandler(LevelTrackBarScrolled);
            Controls.Add(levelTrackBar);
            y += p;
        }

        void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var data = ((SerialPort)sender).ReadExisting();
            Console.WriteLine($"Data Received: {data}");
        }

        void ConnectButtonClicked(object sender, EventArgs e)
        {
            if (serial == null)
            {
                var port = portSelector.Text;
                Console.WriteLine($"Opening serial port: {port}");
                serial = new SerialPort()
                {
                    PortName = port,
                    BaudRate = baudRate,
                    ReadTimeout = 500,
                    WriteTimeout = 500,
                };
                serial.DataReceived += new SerialDataReceivedEventHandler(SerialDataReceived);
                try
                {
                    serial.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                if (!serial.IsOpen)
                {
                    Console.WriteLine($"Port open failure: {port}");
                    serial = null;
                    MessageBox.Show(
                        $"シリアルポート({port})に接続できませんでした。Bluetooth接続状態を確認してください。",
                        "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                Console.WriteLine($"Port opened successfully: {port}");
                levelTrackBar.Enabled = true;
                levelTrackBar.TabStop = true;
                connectButton.Text = "切断";
            }
            else
            {
                serial.Close();
                serial = null;
                levelTrackBar.Enabled = false;
                levelTrackBar.TabStop = false;
                connectButton.Text = "接続";
            }
        }

        void LevelTrackBarScrolled(object sender, EventArgs e)
        {
            if (serial == null) return;
            var t = (double)levelTrackBar.Value / (double)levelDivision;
            var t2 = Math.Pow(t, curveExp);
            var v2 = (int)Math.Round(t2 * (double)(realMaxLevel-realMinLevel));
            if (0 < v2) v2 += realMinLevel;
            var msg = $"{v2}";
            Console.WriteLine(msg);
            serial.WriteLine(msg);
        }
    }

    static class Program
    {
        public static void Main(string[] args) {
            var form = new MainForm();
            Application.Run(form);
        }
    }
}
