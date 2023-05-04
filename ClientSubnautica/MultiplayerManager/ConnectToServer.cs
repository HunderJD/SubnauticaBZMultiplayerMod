﻿using ClientSubnautica.MultiplayerManager.SendData;
using System;
using System.Net;
using System.Net.Sockets;

namespace ClientSubnautica.MultiplayerManager
{
    class ConnectToServer
    {
        //Connect to server
        public static TcpClient start(string ip)
        {
            string[] ipArray = ip.Split(':');

            IPAddress ipDest = IPAddress.Parse(ipArray[0]);
            int port = int.Parse(ipArray[1]);
            TcpClient client = new TcpClient();

            client.Connect(ipDest, port);

            ///envoie de l'id + username 
            SendMyID.start(client);
            return client;
        }
    }
}
