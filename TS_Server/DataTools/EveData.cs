using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace TS_Server.DataTools
{
    //[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //public struct TalkInfo
    //{
    //    public ushort id;
    //    public byte length;
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 254)]
    //    public byte[] dialog;
    //}


    public static class EveData
    {
        public static Dictionary<ushort,Tuple<int,int>>offsets = new Dictionary<ushort,Tuple<int,int>>();        

        // Reads directly from stream to structure
        public static T ReadFromItems<T>(Stream fs, int off)
        {
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
            fs.Read(buffer, off, Marshal.SizeOf(typeof(T)));
            GCHandle Handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            T RetVal = (T)Marshal.PtrToStructure(Handle.AddrOfPinnedObject(), typeof(T));
            Handle.Free();

            return RetVal;
        }

        public static bool loadHeaders()
        {
            try
            {
                using (FileStream fs = new FileStream("eve.Emg", FileMode.Open, FileAccess.Read))
                {                  
                    int nb_headers = 0;
                    fs.Seek(2, 0);
                    int pos = 2;
                    int endHeader = 1000000;
                    byte[] buffer = new byte[0x20];

                    while (fs.Position < endHeader)
                    {
                        fs.Read(buffer, 0, buffer.Length);
                        pos += buffer.Length;
                        ushort mapid = UInt16.Parse(Encoding.Default.GetString(buffer, 1, 5));
                        int off = BitConverter.ToInt32(buffer, 0x18);
                        int len = BitConverter.ToInt32(buffer, 0x1c);
                        offsets[mapid] = new Tuple<int,int>(off, len);
                        nb_headers++;
                        if (nb_headers == 1) endHeader = off;
                    }

                    fs.Close();
                    fs.Dispose();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

        }

        public static ushort read16(byte[] data, int off)
        {
            return (ushort)(data[off] + (data[off + 1] << 8));
        }

        public static uint read32(byte[] data, int off)
        {
            return (uint)(data[off] + (data[off + 1] << 8) + (data[off + 2] << 16) + (data[off + 3] << 24));
        }

        public static Tuple<ushort, ushort> loadCoor(ushort mapid, ushort destid)
        {
            if (!offsets.ContainsKey(mapid)) return null;

            byte[] data = new byte[offsets[mapid].Item2];
            try
            {
                using (FileStream fs = new FileStream("eve.Emg", FileMode.Open, FileAccess.Read))
                {
                    fs.Seek(offsets[mapid].Item1, 0);
                    fs.Read(data, 0, data.Length);                    
                    fs.Close();
                    fs.Dispose();                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

            //Console.WriteLine((data[0] + (data[1] << 8)));

            int pos = 0x67;
            int nb_npc = data[pos];
            //Console.WriteLine(nb_npc);
            pos += 4;
            //NPC, later
            for (int i = 0; i < nb_npc; i++)
            {
                ushort clickID = (ushort)(data[pos] + (data[pos + 1] << 8));
                pos += 2;
                ushort npcID = (ushort)(data[pos] + (data[pos + 1] << 8));
                //Console.WriteLine(npcID); 
                pos += 2;
                ushort nb1 = (ushort)(data[pos] + (data[pos + 1] << 8));
                pos += (nb1 + 2);
                byte nb2 = data[pos];
                pos += (nb2 + 1);
                byte nb_f = data[pos];
                pos += 9;
                pos += (8*nb_f);

                pos += 31;
                int posX = BitConverter.ToInt32(data, pos);
                pos += 4;
                int posY = BitConverter.ToInt32(data, pos);
                pos += 4;
                pos += 41;            
            }

            ushort nb_entry_exit = read16(data, pos); pos += 2;
            for (int i = 0; i < nb_entry_exit; i++)
            {
                pos += 3;
                uint posX = read32(data, pos);
                pos += 4;
                uint posY = read32(data, pos);
                pos += 6;
                //Console.WriteLine(posX + " " + posY); 
            }

            ushort nb_unk1 = read16(data, pos);
            pos += 2;
            for (int i = 0; i < nb_unk1; i++)
            {
                pos += 2;
                ushort nb = read16(data, pos);
                pos += 2;
                pos += nb;                
                pos += 21;
                //Console.WriteLine(posX + " " + posY); 
            }

            ushort nb_unk2 = read16(data, pos);
            //Console.WriteLine(nb_unk2); 
            pos += 2;
            for (int i = 0; i < nb_unk2; i++)
            {
                pos += 2;
                ushort nb = read16(data, pos);
                pos += 2;
                pos += nb;
                pos += 17;
                //Console.WriteLine(posX + " " + posY); 
            }

            ushort nb_dialog = read16(data, pos);
            pos += 2;
            for (int i = 0; i < nb_dialog; i++)
            {
                pos += 4;
                byte nb_d = data[pos];
                pos += 4;
                pos += 5 * nb_d;
                //Console.WriteLine(nb_d); 
            }

            ushort nb_warp = read16(data, pos);
            pos += 2;
            uint X = 0, Y = 0;
            int count = 0;
            for (int i = 0; i < nb_warp; i++)
            {
                ushort warp_id = read16(data, pos);
                pos += 2;
                ushort dest_map = read16(data, pos);
                pos += 4;
                uint posX = read32(data, pos) * 20 - 10;
                pos += 4;
                uint posY = read32(data, pos) * 20 - 10;
                pos += 4;
                pos += 0x19;

                if (dest_map == destid)
                {
                    X = posX; Y = posY;
                    count++;
                }
                Console.WriteLine(mapid + " " + warp_id + " " + dest_map + " " + posX + " " + posY);
            }
            if (count == 1)
                return new Tuple<ushort, ushort>((ushort)X, (ushort)Y);
            return null;
        }
    }
}
