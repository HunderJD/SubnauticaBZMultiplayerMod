using ClientSubnautica.MultiplayerManager.ReceiveData;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace ServerSubnautica
{
    internal class HandleClient
    {
        int id;
        string username;
        
        string data;

        TcpClient client;
        NetworkStream stream;
        ClientMethod clientAction = new ClientMethod();
        public HandleClient(int id, string username)
        {
            this.id = id;
            this.username = username;
            lock (Server._lock) this.client = Server.list_clients[id];
            this.stream = this.client.GetStream();
            initialize();
        } 
        public void initialize()
        {
            int bufferSize = 1024;

            byte[] dataLength = BitConverter.GetBytes(Server.mapBytes.Length);

            stream.Write(dataLength, 0, 4);

            int bytesSent = 0;
            int bytesLeft = Server.mapBytes.Length;

            while (bytesLeft > 0)
            {
                int curDataSize = Math.Min(bufferSize, bytesLeft);

                stream.Write(Server.mapBytes, bytesSent, curDataSize);

                bytesSent += curDataSize;
                bytesLeft -= curDataSize;
            }

            string session = Server.gameInfo["session"].ToString();
            string changeSet = Server.gameInfo["changeSet"].ToString();
            string gameMode = Server.gameInfo["gameMode"].ToString();
            string storyVersion = Server.gameInfo["storyVersion"].ToString();

            byte[] test2 = Encoding.ASCII.GetBytes(session + "$" + changeSet + "$" + gameMode + "$" + storyVersion);


            stream.Write(test2, 0, test2.Length);

            byte[] buffer2 = new byte[1024];
            stream.Read(buffer2, 0, buffer2.Length);
            clientAction.broadcast(NetworkCMD.getIdCMD("NewId") + ":" + this.id + "/END/", this.id);
            string ids = "";
            lock (Server._lock)
            {
                foreach (var item in Server.list_clients)
                {
                    if (item.Key != this.id)
                    {
                        ids += item.Key + ";";
                    }
                }
            }
            if (ids.Length > 1)
            {
                clientAction.specialBroadcast(NetworkCMD.getIdCMD("AllId") + ":" + ids + "/END/", this.id);
                lock (Server._lock)
                {
                    Server.list_clients.First().Value.GetStream().Write(Encoding.ASCII.GetBytes(NetworkCMD.getIdCMD("GetTimePassed") + "/END/"));
                }
            }
        }

        public void start()
        {
            loop();
            endConnection();
        }

        public void loop()
        {
            while (true)
            {
                int cont = 1;
                byte[] buffer = new byte[1024];
                //Array.Clear(buffer, 0, buffer.Length);
                int byte_count;

                byte_count = this.stream.Read(buffer, 0, buffer.Length);

                data = Encoding.ASCII.GetString(buffer, 0, byte_count);

                if (!data.Contains("/END/"))
                    continue;
                string[] commands = data.Split(new string[] { "/END/" }, StringSplitOptions.None);

                foreach (var command in commands)
                {
                    if (command.Length <= 1)
                        continue;
                    try
                    {
                        string idCMD = command.Split(':')[0];
                        if (idCMD == NetworkCMD.getIdCMD("Disconnected"))
                        {
                            cont = 0;
                            break;
                        }

                        var tempList = command.Substring(command.IndexOf(":") + 1).Split(';').ToList();

                        if (idCMD != NetworkCMD.getIdCMD("Disconnected"))
                            tempList.Insert(0, id.ToString());
                        string[] param = tempList.ToArray();

                        //Redirecting data received to right method
                        clientAction.redirectCall(param, idCMD);
                    }
                    catch (Exception) { }
                }
                if (cont == 0)
                    break;
            }
        }

        public void endConnection()
        {
            List<(string, Vector3, Quaternion)> player_data = new List<(string, Vector3, Quaternion)>();
            Vector3 pos = new Vector3();
            Quaternion rot = new Quaternion();

            if (data.Contains("/END/"))
            {
                try
                {
                    string[] parts = data.Split(new string[] { "/END/" }, StringSplitOptions.RemoveEmptyEntries);
                    string[] idParts = parts[0].Split(':');

                    if (idParts[0] == NetworkCMD.getIdCMD("Disconnected"))
                    {
                        string[] t = idParts[1].Split(";");

                        float posX = float.Parse(t[0]);
                        float posY = float.Parse(t[1]);
                        float posZ = float.Parse(t[2]);
                        pos = new Vector3(posX, posY, posZ);

                        float rotX = float.Parse(t[3]);
                        float rotY = float.Parse(t[4]);
                        float rotZ = float.Parse(t[5]);
                        float rotW = float.Parse(t[6]);
                        rot = new Quaternion(rotX, rotY, rotZ, rotW);
                    }
                }
                catch{ }
            }

            player_data.Add((username, pos, rot));
            Server.OnDeconnexion(Server.linkPlayer_Client[id].ToString(), player_data); ///save to .json file 
            player_data.RemoveRange(0, player_data.Count);


            lock (Server._lock) Server.list_clients.Remove(id);
            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
            Console.WriteLine($"{username} left the server");

            clientAction.redirectCall(new string[] {username}, NetworkCMD.getIdCMD("Disconnected")); //message d'envoie de déconnesxion d'un joueur, rien ne saffiche et cest voulu
        }
    }
}
