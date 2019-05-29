using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace WindowsFormsApp1
{
    class Constans
    {
        public const ushort DATA_SIZE = 2016;
        public const ushort HEADER_SIZE = 29;
    }
    class Data
    {
        public byte[] data;
        public int size;
    }

    class Packet
    {
        public PacketHeader header;
        public byte[] area;
        public ushort crc;
        public byte[] CrcByteArray()
        {
            byte[] arr = new byte[Constans.HEADER_SIZE + header.data_size - 2];
            int size = Constans.HEADER_SIZE - 2;
            byte[] header_bytes = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(header, ptr, true);
            Marshal.Copy(ptr, header_bytes, 0, size);
            Marshal.FreeHGlobal(ptr);
            for (int i = 0; i < header_bytes.Length; i++)
            {
                arr[i] = header_bytes[i];
            }

            for (int i = 0; i < header.data_size; i++)
            {
                arr[i + Constans.HEADER_SIZE - 2] = area[i];
            }

            return arr;
        }
        public byte[] ToByteArray()
        {
            byte[] arr = new byte[Constans.HEADER_SIZE + area.Length];
            int size = Constans.HEADER_SIZE - 2;
            byte[] header_bytes = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(header, ptr, true);
            Marshal.Copy(ptr, header_bytes, 0, size);
            Marshal.FreeHGlobal(ptr);
            for (int i = 0; i < header_bytes.Length; i++)
            {
                arr[i] = header_bytes[i];
            }

            for (int i = 0; i < area.Length; i++)
            {
                arr[i + Constans.HEADER_SIZE - 2] = area[i];
            }

            arr[arr.Length - 2] = (byte)(crc % 256); 
            arr[arr.Length - 1] = (byte)(crc >> 8); 

            return arr;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct PacketHeader
    {
        public ushort length;
        public ushort bootver;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public byte[] sn;
        public ushort soft;
        public uint offset;
        public ushort data_size;
    }
}
