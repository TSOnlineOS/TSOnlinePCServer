﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TS_Server.DataTools;
using TS_Server.Client;

namespace TS_Server.Server
{
    public class TSWorld
    {
        private static TSWorld instance = null;
        public TSServer server;
        public Dictionary<ushort, TSMap> listMap;

        public TSWorld(TSServer s)
        {
            server = s;
            instance = this;
            listMap = new Dictionary<ushort, TSMap>();
        }

        public static TSWorld getInstance()
        {
            return instance;
        }

        public TSMap initMap(ushort mapid)
        {
            TSMap m = new TSMap(this, mapid);
            listMap.Add(mapid,m);
            return m;
        }

        public void warp(TSClient client, ushort warpid)
        {
            client.reply(new PacketCreator(new byte[] { 20, 0x07 }).send());
            client.reply(new PacketCreator(new byte[] { 0x29, 0x0E }).send());

            ushort start = client.map.mapid;
            if (WarpData.warpList.ContainsKey(start))
            {
                if (WarpData.warpList[start].ContainsKey(warpid))
                {
                    ushort[] dest = WarpData.warpList[start][warpid];

                    if (!listMap.ContainsKey(dest[0]))
                    {
                        listMap.Add(dest[0], new TSMap(this, dest[0]));
                    }

                    client.getChar().mapID = dest[0];
                    client.getChar().mapX = dest[1];
                    client.getChar().mapY = dest[2];
                    //client.map.removePlayer(client.accID);

                    listMap[dest[0]].addPlayerWarp(client, dest[1], dest[2]);
                    return;
                }
                else
                {
                    Console.WriteLine("Warp data helper : warpid " + warpid + " not found");
                    EveData.loadCoor(start, 12000);
                }
            }
            else
            {
                Console.WriteLine("Warp data helper : mapid " + start + " warpid " + warpid + " not found");
                EveData.loadCoor(start, 12000);
            }
            client.AllowMove();
        }
    }
}
