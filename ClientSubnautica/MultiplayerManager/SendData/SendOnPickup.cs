﻿using ClientSubnautica.ClientManager;
using ClientSubnautica.MultiplayerManager;
using ClientSubnautica.MultiplayerManager.ReceiveData;
using ClientSubnautica.StartMod;
using System;
using System.Net.Sockets;
using System.Text;

namespace ClientSubnautica
{
    public class SendOnPickup
    {      
        public static void send(Pickupable pickupable)
        {
            NetworkStream ns2 = InitializeConnection.client.GetStream();

            byte[] msgresponse = Encoding.ASCII.GetBytes("");
            Array.Clear(msgresponse, 0, msgresponse.Length);
            msgresponse = Encoding.ASCII.GetBytes(NetworkCMD.getIdCMD("PickupItem") + ":" + pickupable.gameObject.GetComponent<UniqueGuid>().guid + "/END/");
            // Position envoyé !
            ns2.Write(msgresponse, 0, msgresponse.Length);
        }
    }
}
