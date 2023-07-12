﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TS_Server.Client;
using TS_Server.DataTools;
using TS_Server.Server;

namespace TS_Server.PacketHandlers
{
    class ChatHandler
    {
        private byte[] msg;
        public ChatHandler(TSClient client, byte[] data)
        {
            if (!specialMsg(client, data))
            {
                switch (data[1])
                {
                    case 1: // All
                        {
                            msg = new byte[data.Length - 2];
                            Array.Copy(data,2,msg,0,msg.Length);
                            byte[] packet = FillMessageData(client, (byte)ChatMsg.CHAT_MSG_ALL, msg);
                            client.getChar().replyToAll(packet, false);
                        }
                        break;
                    case 2: // Light
                        {
                            msg = new byte[data.Length - 2];
                            Array.Copy(data, 2, msg, 0, msg.Length); byte[] packet = FillMessageData(client, (byte)ChatMsg.CHAT_MSG_LIGHT, msg);
                            client.getChar().replyToMap(packet, false);
                        }
                        break;
                    case 3: // Whisper
                        {
                            UInt32 targetID = PacketReader.read32(data, 2);
                            TSCharacter target = TSServer.getInstance().getPlayerById((int)targetID).getChar();
                            if (target != null)
                            {
                                msg = new byte[data.Length - 6];
                                Array.Copy(data, 6, msg, 0, msg.Length); 
                                byte[] packet = FillMessageData(client, (byte)ChatMsg.CHAT_MSG_WHISPER, msg);
                                target.reply(packet);
                            }
                        }
                        break;
                    case 4:
                        {
                            // Chat With GM
                        }
                        break;
                    case 5: // Team
                        {
                            msg = new byte[data.Length - 2];
                            Array.Copy(data, 2, msg, 0, msg.Length); 
                            byte[] packet = FillMessageData(client, (byte)ChatMsg.CHAT_MSG_PARTY, msg);
                            client.getChar().replyToTeam(packet);
                        }
                        break;
                    case 6: // Army
                        break;
                    case 7: // Ally
                        break;
                }
            }
        }

        public bool specialMsg(TSClient client, byte[] data)
        {
            ushort id;
            ushort amount;
            byte[] arr = new byte[data.Length - 2];
            Array.Copy(data, 2, arr, 0, data.Length - 2);
            string msg = Encoding.Default.GetString(arr);

            try
            {
                if (String.Compare(msg, 0, "/addpet ", 0, 8, true) == 0)
                {
                    id = UInt16.Parse(msg.Substring(8));
                    client.getChar().addPet(id, 0);
                    return true;
                }
                else if (String.Compare(msg, 0, "/opensto", 0, 8, true) == 0)
                {
                    PacketCreator storage = new PacketCreator(0x1D, 06);
                    client.reply(storage.send());
                    return true;
                }
                else if (String.Compare(msg, 0, "/additem ", 0, 9, true) == 0)
                {
                    if (msg.IndexOf(" ", 9) > -1)
                    {
                        msg = msg.Substring(9);
                        string[] msg_array = msg.Split(' ');
                        id = UInt16.Parse(msg_array[0]);
                        amount = UInt16.Parse(msg_array[1]);
                        client.getChar().inventory.addItem(id, amount, true);
                    }
                    else
                    {
                        id = UInt16.Parse(msg.Substring(9));
                        client.getChar().inventory.addItem(id, 1, true);
                    }
                    return true;
                }
                else if (String.Compare(msg, 0, "/sleep", 0, 6, true) == 0)
                {
                    if (client.getChar().party != null)
                    {
                        TSParty party = client.getChar().party;
                        foreach (TSCharacter c in party.member)
                        {
                            c.sleep();
                        }
                    }
                    else
                    {
                        client.getChar().sleep();
                    }
                    return true;
                }
                else if (String.Compare(msg, 0, "/reborn", 0, 7, true) == 0)
                {
                    client.getChar().rebornChar(1, 0);
                    return true;
                }
                else if (String.Compare(msg, 0, "/rebirth ", 0, 9, true) == 0)
                {
                    byte job = Byte.Parse(msg.Substring(9));
                    client.getChar().rebornChar(2, job);
                    return true;
                }
                else if (String.Compare(msg, 0, "/rbpet", 0, 6, true) == 0)
                {
                    if (client.getChar().checkPetReborn(1))
                        client.reply(new PacketCreator(0x2c, 2).send());
                    return true;
                }
                else if (String.Compare(msg, 0, "/rb2pet", 0, 7, true) == 0)
                {
                    if (client.getChar().checkPetReborn(2))
                        client.reply(new PacketCreator(0x2c, 3).send());
                    return true;
                }
                else if (String.Compare(msg, 0, "/addball ", 0, 9, true) == 0)
                {
                    if (client.getChar().rb == 2)
                    {
                        client.getChar().ball_point += Byte.Parse(msg.Substring(9));
                        client.getChar().sendBallList();
                    }
                    return true;
                }
                else if (String.Compare(msg, 0, "/level ", 0, 7, true) == 0) // can handle "/level 200" for testing ;)
                {
                    client.getChar().level = Byte.Parse(msg.Substring(7));
                    client.getChar().refreshChr();
                    client.getChar().refresh(client.getChar().level, 0x23);
                    return true;
                }
                else if (String.Compare(msg, 0, "/addexp ", 0, 8, true) == 0)
                {
                    client.getChar().setExp(Int32.Parse(msg.Substring(8)));
                    return true;
                }
                else if (String.Compare(msg, 0, "/setcharelement ", 0, 15, true) == 0)
                {
                    client.getChar().setCharElement(Byte.Parse(msg.Substring(16)));
                    return true;
                }
                else if (String.Compare(msg, 0, "/setoutfit ", 0, 10, true) == 0)
                {
                    ushort outfitId = ushort.Parse(msg.Substring(11));
                    if (outfitId == 0)
                    {
                        //random outfit, just for fun
                        ushort maxRan = (ushort)(NpcData.npcList.Values.Count - 1);
                        outfitId = NpcData.npcList[RandomGen.getUShort(0, maxRan)].id;
                    }
                    client.getChar().outfitId = outfitId;
                    client.getChar().showOutfit();
                    return true;
                }
                else if (String.Compare(msg, 0, "/battle ", 0, 7, true) == 0)
                {
                    ushort battleid = ushort.Parse(msg.Substring(8));
                    new TSBattleNPC(client, 3, BattleData.battleList[battleid].getGround(), BattleData.battleList[battleid].getNpcId());
                    return true;
                }
                else if (String.Compare(msg, 0, "/motd ", 0, 5, true) == 0)
                {
                    string message = msg.Substring(5);
                    SendSysMessage(client, message, false);
                    return true;
                }
                else if (String.Compare(msg, 0, "/gm ", 0, 3, true) == 0)
                {
                    string message = msg.Substring(3);
                    SendGmMessage(client, message, false);
                    return true;
                }
                else
                {
                    //Console.WriteLine(msg);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return false;
        }

        public byte[] FillMessageData(TSClient client, byte type, byte[] message)
        {
            var p = new PacketCreator(0x02);
            p.add8(type);

            if (type == (byte)ChatMsg.CHAT_MSG_SYSTEM)
                p.add32((UInt32)0x0000);
            else
                p.add32((UInt32)client.accID);
            p.addBytes(message);

            return p.send();
        }
        public void SendSysMessage(TSClient client, string msg, bool self = false)
        {
            byte[] packet = FillMessageData(client, (byte)ChatMsg.CHAT_MSG_SYSTEM,Encoding.Default.GetBytes(msg));
            client.getChar().replyToAll(packet, self);
        }
        public void SendGmMessage(TSClient client, string msg, bool self = false)
        {
            byte[] packet = FillMessageData(client, (byte)ChatMsg.CHAT_MSG_GM, Encoding.Default.GetBytes(msg));
            client.getChar().replyToAll(packet, self);
        }

        enum ChatMsg
        {
            CHAT_MSG_NULL = 0x00,
            CHAT_MSG_ALL = 0x01,
            CHAT_MSG_LIGHT = 0x02,
            CHAT_MSG_WHISPER = 0x03,
            CHAT_MSG_GM = 0x04,
            CHAT_MSG_PARTY = 0x05,
            CHAT_MSG_ARMY = 0x06,
            CHAT_MSG_ALLY = 0x07,
            CHAT_MSG_UNK_0 = 0x08,
            CHAT_MSG_UNK_1 = 0x09,
            CHAT_MSG_UNK_2 = 0x0A,
            CHAT_MSG_SYSTEM = 0x0B
        };
    }
}
