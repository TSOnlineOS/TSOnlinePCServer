﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TS_Server.Client;

namespace TS_Server.Server
{
    public class TSMap
    {
        public TSWorld world;
        public ushort mapid;
        public Dictionary<uint, TSClient> listPlayers;

        public TSMap(TSWorld w, ushort id)
        {
            world = w;
            mapid = id;
            listPlayers = new Dictionary<uint, TSClient>();
        }

        public void addPlayerWarp(TSClient client, ushort x, ushort y)
        {
            //warp done, reply to client
            var p = new PacketCreator(0x0c);
            p.add32(client.accID);
            p.add16(mapid);
            p.add16(x);
            p.add16(y);
            // Group
            p.add8((byte)(client.getChar().isTeamLeader() ? 4 : (!client.getChar().isJoinedTeam() ? 0 : 1)));
            p.add8(0); // Orient
            client.reply(p.send());

            client.AllowMove();

            //client.reply(new PacketCreator(0x17, 4).send());
            client.map.removePlayer(client.accID);
            this.addPlayer(client);
            client.map = this;
        }

        public void addPlayer(TSClient client)
        {
            listPlayers.Add(client.accID, client);
            client.map = this;

            //packets for client
            foreach (TSClient c in listPlayers.Values)
            {
                if (c.accID != client.accID)
                {
                    // packets For players in the same map
                    client.reply(c.getChar().sendLookForOther());
                    c.reply(client.getChar().sendLookForOther()); //nice line :))

                    // Update team visible or pet list from other
                    c.getChar().sendUpdateTeam();
                    client.getChar().sendUpdateTeam();
                }
            }
            client.getChar().sendUpdateTeam();
        }

        public void removePlayer(uint id)
        {
            TSClient client = getPlayerById(id);

            var p = new PacketCreator(0x0C);
            p.add32(client.accID);
            p.add16(mapid);
            p.add16(client.getChar().mapX);
            p.add16(client.getChar().mapY);
            p.add16(0x0100);

            listPlayers.Remove(id);
            BroadCast(client, p.send(), false);
        }

        public TSClient getPlayerById(uint id)
        {
            if (listPlayers.ContainsKey(id))
                return listPlayers[id];
            return null;
        }

        public void movePlayer(TSClient client, ushort x, ushort y)
        {
            var p = new PacketCreator(0x06);
            p.add8(0x01);
            p.add32(client.accID);
            p.add8(client.getChar().orient); // Orientation
            p.add16(x);
            p.add16(y);
            byte[] packet = p.send();

            TSCharacter chr = client.getChar();
            chr.replyToMap(packet, true);
            client.getChar().mapX = x;
            client.getChar().mapY = y;

            if (chr.party != null)
            {
                foreach (TSCharacter c in chr.party.member)
                {
                    if (c != chr)
                    {
                        c.mapX = x;
                        c.mapY = y;
                    }
                }
            }

        }

        public void announceBattle(TSClient client)
        {
            // Update Smoke
            var p = new PacketCreator(0x0B);
            p.add8(0x04); p.add8(0x02);
            p.add32(client.accID); p.add16(0); // Guess
            p.add8(client.battle.battle_type); // Guessing

            BroadCast(client, p.send(), false);
        }

        public void BroadCast(TSClient client, byte[] data, bool self)
        {
            if (self)
            {
                client.reply(data);
            }
            foreach (TSClient c in listPlayers.Values)
            {
                if (c.accID != client.accID)
                {
                    c.reply(data);
                }
            }
        }
    }
}
