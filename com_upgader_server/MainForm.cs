using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;

namespace WindowsFormsApp1
{
    public partial class MainForm : Form
    {

        public MainForm()
        {
            InitializeComponent();
        }

        SerialPort com_port = new SerialPort("COM5", 115200, Parity.None, 8, StopBits.One);
        FileStream file;
        Queue<byte> data_recived = new Queue<byte>();

        private void Button1_Click(object sender, EventArgs e)
        {
            com_port.Open();
            com_port.DataReceived += DataReceived;
            btnStart.Enabled = false;

        }

        private Packet GenerateSendPacket(Packet packet, byte[] send_data)
        {
            Packet send_packet = new Packet();
            send_packet.header.length = Constans.DATA_SIZE + Constans.HEADER_SIZE;
            send_packet.header.bootver = packet.header.bootver;
            send_packet.header.sn = packet.header.sn;
            send_packet.header.soft = packet.header.soft;
            send_packet.header.offset = packet.header.offset;
            send_packet.header.data_size = (ushort)send_data.Length;
            var t = new byte[Constans.DATA_SIZE];
            for (int i = 0; i < send_data.Length; i++)
            {
                t[i] = send_data[i];
            }
            send_packet.area = t;
            var crc = send_packet.crc = CRC.CRC16Packet(send_packet);

            if (send_data.Length < Constans.DATA_SIZE)
            {
                t[send_data.Length] = (byte)(crc % 256);
                t[send_data.Length + 1] = (byte)(crc >> 8);
            }
            
            return send_packet;
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var read_data = new byte[com_port.BytesToRead];
            com_port.Read(read_data, 0, read_data.Length);
            read_data.ToList().ForEach((i) => data_recived.Enqueue(i));
            Invoke(new Action(() =>
            {
                tbStatus.AppendText(string.Format("\r\n Got {0} bytes", read_data.Length));
            }));

            var packet = ProcessPacket();
            if (packet == null) return;

            


            byte[] c = new byte[packet.header.data_size];
            file.Seek(packet.header.offset, SeekOrigin.Begin);
            int read_size = file.Read(c, 0, c.Length);
            byte[] send_data  = c.Take(read_size).ToArray();
            Packet send_packet = GenerateSendPacket(packet, send_data);
            Invoke(new Action(() =>
            {
                tbStatus.AppendText(string.Format("\r\n offset: {0}", packet.header.offset));
                pbCount.PerformStep();
            }));

            var f = send_packet.ToByteArray();          
            com_port.Write(f, 0, f.Length);
            
        }

        private Packet ProcessPacket()
        {
            if(data_recived.Count < Constans.HEADER_SIZE)
            {
                return null;
            }
            Data data = new Data();
            data.data = Enumerable.Range(0, Constans.HEADER_SIZE).Select(i => data_recived.Dequeue()).ToArray();
            data.size = data.data.Length;
            int t = 0;
            Packet packet = new Packet();
            packet.header.length = (ushort)((uint)data.data[t + 1] << 8 | data.data[t]);
            t += 2;
            packet.header.bootver = (ushort)((uint)data.data[t + 1] << 8 | data.data[t]);
            t += 2;
            packet.header.sn = new byte[15]; 
            for (int i = 0; i < 15; i++)
            {
                packet.header.sn[i] = data.data[t + i];
            }
            t += 15;
            packet.header.soft = (ushort)((uint)data.data[t + 1] << 8 | data.data[t]);
            t += 2;
            packet.header.offset = (uint)data.data[t + 3] << 24 | (uint)data.data[t + 2] << 16 | (uint)data.data[t + 1] << 8 | data.data[t];
            t += 4;
            packet.header.data_size = (ushort)((uint)data.data[t + 1] << 8 | data.data[t]);
            t += 2;
            packet.area = new byte[Constans.DATA_SIZE];
            for (int i = t; i < packet.header.length - 2; i++)
            {
                packet.area[i] = data.data[i + t];
            }
            t = data.size - 2;
            packet.crc = (ushort)((uint)data.data[t + 1] << 8 | data.data[t]);
            var crc = CRC.CRC16Packet(packet);
            bool check = (crc == packet.crc);
            return packet;
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            if(openFileDialog.ShowDialog() == DialogResult.OK)
            {
                file = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read);
                pbCount.Maximum = (int)file.Length;
                pbCount.Step = Constans.DATA_SIZE;
            }
        }
    }
}
