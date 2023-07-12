using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using TS_Server.Server;

namespace TS_Server.Client
{
    public class TSClient
    {
        private Socket socket;
        private String clientID;
        private TSCharacter chr;
        public bool creating, online;
        public BattleAbstract battle;
        public TSMap map;
        public uint accID;
        public byte[] name_temp;
        public TSWorld world;
        public ushort warpPrepare;

        public TSClient(Socket s, String id)
        {
            socket = s;
            clientID = id;
            creating = false;
            online = false;
        }

        public void createChar(byte[] data)
        {
            chr = new TSCharacter(this);

            chr.initChar(data, name_temp);
            chr.loginChar();
            world = TSServer.getInstance().getWorld();
        }

        public int checkLogin(uint acc_id, string password)
        {
            //check exist, online, create char(เช็คตัวละครออนไลน์)
            int ret = 3;
            //var c = new TSMysqlConnection();

            //MySqlDataReader data = c.selectQuery("SELECT password, loggedin FROM account WHERE id = " + acc_id);
            /*
            if (!data.Read())
                ret = 1;
            else if (data.GetString(0) != password)
                ret = 1;
            else if (data.GetBoolean(1))
                ret = 2;
            else
            {
                var c2 = new TSMysqlConnection();
                MySqlDataReader data2 = c2.selectQuery("SELECT accountid FROM chars WHERE accountid = " + acc_id);

                if (!data2.Read())
                {
                    accID = acc_id;
                    ret = 3;
                }
                data2.Close();
                c2.connection.Close();
            }
            */
            //data.Close();
            //c.connection.Close();

            //if (ret == 0)
            //{
                accID = acc_id;
                chr = new TSCharacter(this);
            //}

            return 0;
        }

        public bool isTeamLeader()
        {
            return false;
        }
        public bool isJoinedTeam()
        {
            return false;
        }

        public bool isOnline()
        {
            return online;
        }

        public TSCharacter getChar()
        {
            return chr;
        }

        public Socket getSocket()
        {
            return socket;
        }

        public String getClientID()
        {
            return clientID;
        }

        public void reply(byte[] data)
        {
            try
            {
                socket.Send(data);
            }
            catch (Exception e)
            {
                Console.WriteLine("Socket down, client " + clientID + " disconnect");
                disconnect();
            }
        }

        public void savetoDB()
        {
            /*
            var c = new TSMysqlConnection();
            c.connection.Open();
            chr.saveCharDB(c.connection);
            for (int i = 0; i < 4; i++)
                if (chr.pet[i] != null)
                    chr.pet[i].savePetDB(c.connection, false);
            c.connection.Close();
            */
        }

        public void continueMoving()
        {
            RequestComplete();
            AllowMove();
        }
        public void RequestComplete()
        {
            reply(new PacketCreator(0x14, 8).send());
        }
        public void AllowMove()
        {
            reply(new PacketCreator(5, 4).send());
            reply(new PacketCreator(0x0F, 0x0A).send());
        }
        public void ClickkNpc(byte[] data, TSClient client)
        {
            PacketCreator p = new PacketCreator(0x14, 1);
            p.addByte(0); p.add16(0); p.addByte(0); p.addByte(1);
            p.add16(ushort.Parse((PacketReader.read16(data, 1) + 2).ToString()));
            p.add16(0); p.add16(0); p.add16(0);
            p.add16(10666);//you are hero :))
            client.reply(p.send());
        }
        public void UImportant()
        {
            // Important
            reply(new PacketCreator(new byte[] { 0x18, 0x07, 0x03, 0x04 }).send());

            PacketCreator p = new PacketCreator(0x29);
            p.add8(0x05); p.add8(0x01); p.add8(0x01);
            p.add32(0); p.add32(0); p.add32(0); p.add32(0); p.add32(0);
            p.add32(0); p.add32(0); p.add32(0); p.add32(0); p.add32(0);
            p.add32(0x02000000); p.add32(0x00000001); p.add32(0); p.add32(0);
            p.add32(0); p.add32(0); p.add32(0); p.add32(0);
            p.add32(0); p.add32(0); p.add32(0); p.add32(0);
            p.add32(0x00000103); p.add32(0); p.add32(0); p.add32(0);
            p.add32(0); p.add32(0); p.add32(0); p.add32(0);
            p.add32(0); p.add32(0); p.add32(0); p.add32(0x00010400);
            p.add32(0); p.add32(0); p.add32(0); p.add32(0);
            p.add32(0); p.add32(0); p.add32(0); p.add32(0);
            p.add32(0); p.add32(0); p.add32(0x01050000); p.add32(0);
            p.add32(0); p.add32(0); p.add32(0); p.add32(0);
            p.add32(0); p.add32(0); p.add32(0); p.add32(0);
            p.add32(0); p.add16(0); p.add8(0);
            reply(p.send());

            //UpdateMap2Npc();

            p = new PacketCreator(0x0B);
            p.add32(0xF24B0204); p.add32(0x00000001); p.add8(0);
            reply(p.send());
        }
        public void U0602()
        {
            reply(new PacketCreator(0x06, 0x06).send());
        }
        public void U1406()
        {
            reply(new PacketCreator(0x14, 6).send());
        }

        public void disconnect()
        {
            if (battle != null)
            {
                battle.outBattle(this);
            }
            if (online)
            {
                savetoDB();

                // Disappear
                var p = new PacketCreator(0x0D, 0x04);
                p.add32((uint)accID);
                chr.replyToMap(p.send(), false);
                p = new PacketCreator(0x01, 0x01);
                p.add32((uint)accID);
                chr.replyToMap(p.send(), false);

                map.listPlayers.Remove(accID);
                //map.removePlayer(accID);
                TSServer.getInstance().removePlayer(accID);
                online = false;
            }
        }
    }
}
