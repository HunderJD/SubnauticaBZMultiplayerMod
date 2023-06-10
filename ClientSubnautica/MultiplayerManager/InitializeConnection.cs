using ClientSubnautica.ClientManager;
using ClientSubnautica.MultiplayerManager.ReceiveData;
using ClientSubnautica.MultiplayerManager.SendData;
using ClientSubnautica.StartMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UWE;
using static RadicalLibrary.Spline;

namespace ClientSubnautica.MultiplayerManager
{
    internal class InitializeConnection
    {
        public static TcpClient client = new TcpClient();
        public static bool threadStarted = false;
        public static string outDirectoryPath;
        public byte[] data;

        public void start(string ip)    //Ici égale a 127.0.0.1:5000
        {
            //Thread sender

            client = ConnectToServer.start(ip);

            bool isconnected = client.Connected;
            NetworkStream ns = client.GetStream();
            byte[] receivedBytes = new byte[1024];

            ErrorMessage.AddMessage("Downloading map... 0%");
            data = downloadMap(ns);
            ErrorMessage.AddMessage(data.Length.ToString());

            ErrorMessage.AddMessage("Downloading map... 100%");

            outDirectoryPath = importMap(data);

            ErrorMessage.AddMessage("Map downloaded !");

            byte[] receivedBytes2 = new byte[1024];
            int byte_count;

            byte_count = ns.Read(receivedBytes2, 0, receivedBytes2.Length);

            string message2 = Encoding.ASCII.GetString(receivedBytes2, 0, byte_count);
            string[] arr = message2.Split('$');

            if (arr != null)
            {
                GameModePresetId gameMode = GameModePresetId.Survival;
                switch (arr[2])
                {
                    /* 
                    case "1":
                        gameMode = GameModePresetId.Freedom;
                        break;
                     
                    case "2":
                        gameMode = GameModePresetId.Hardcore;
                        break;
                    */
                    case "4":
                        gameMode = GameModePresetId.Creative;
                        break;
                }

                ErrorMessage.AddMessage("Loading map ...");

                //ce gros pavé CHARGE LA MAP mdr
                CoroutineHost.StartCoroutine(LoadMap.loadMap(uGUI_MainMenu.main, outDirectoryPath, arr[0], arr[1], gameMode, new GameOptions(),arr[3], returnValue =>
                    {


                        byte[] test = Encoding.ASCII.GetBytes("ok");

                        ns.Write(test, 0, test.Length);

                        //Thread receiver
                        Thread threadReceiver = new Thread(o => ReceiveDataFromServer.start((TcpClient)o));
                        threadReceiver.Start(client);

                        threadStarted = true;
                    }));
            }
        }

        public static byte[] downloadMap(NetworkStream ns)
        {
            byte[] fileSizeBytes = new byte[4];
            int bytes = ns.Read(fileSizeBytes, 0, 4);
            
            int dataLength = BitConverter.ToInt32(fileSizeBytes, 0);

            int bytesLeft = dataLength;
            byte[] data = new byte[dataLength];

            int bufferSize = 1024;
            int bytesRead = 0;

            while (bytesLeft > 0)
            {
                int curDataSize = System.Math.Min(bufferSize, bytesLeft);
                if (client.Available < curDataSize)
                    curDataSize = client.Available; //This saved me

                bytes = ns.Read(data, bytesRead, curDataSize);

                bytesRead += curDataSize;
                bytesLeft -= curDataSize;
            }
            return data;
        }   //Donwload les DATA de la map

        public static string importMap(byte[] data) //crée une copie de la map
        {
            string[] outPath = { MainPatcher.location, "SNAppData", "SavedGames", "MultiplayerSave" };
            if (PlatformServicesEpic.IsPresent())
            {
                outPath[0] = Application.persistentDataPath;
                outPath[1] = "SubnauticaZero";
            }
            string outDirectoryPath = System.IO.Path.Combine(outPath);
            if (Directory.Exists(outDirectoryPath))
                Directory.Delete(outDirectoryPath, true);

            Directory.CreateDirectory(outDirectoryPath);
            string[] outPath2 = { outDirectoryPath, "world.zip" };
            string outZipPath = System.IO.Path.Combine(outPath2);
            File.WriteAllBytes(outZipPath, data);
            ZipFile.ExtractToDirectory(outZipPath, outDirectoryPath);
            File.Delete(outZipPath);

            return outDirectoryPath;
        }   //transforme ces data en fichié de jeu que le client peut lire

        private static string getPath() //j'essaye d'obtenir le "path" de la map "MultiplayerSave"
        {
            string[] outPath = {MainPatcher.location, "SNAppData", "SavedGames", "MultiplayerSave" };
            if (PlatformServicesEpic.IsPresent())
            {
                outPath[0] = Application.persistentDataPath;
                outPath[1] = "SubnauticaZero";
            }
            string outDirectoryPath = System.IO.Path.Combine(outPath);


            return outDirectoryPath;//renvoie le fichier de ta partie (partie multijoueur) sur lequel tu joue
        }

        public static void deleteMap(NetworkStream ns) 
        {
            string path = getPath();    // equal to the path of THE save file (finish by \\MultiplayerSave)
            if (Directory.Exists(path))
            {
                string tempPath = System.IO.Path.GetDirectoryName(path) + "\\World.zip";


                if (File.Exists(tempPath))//il ne le supprime pas
                    File.Delete(tempPath);

                ZipFile.CreateFromDirectory(path, tempPath);   


                byte[] saveData = File.ReadAllBytes(tempPath); 

                if (saveData.Length > 0)
                {
                    //send it
                    //delete it



                    /*                    int bufferSize = 1024;
                                        byte[] dataLength = BitConverter.GetBytes(saveData.Length);

                                        ns.Write(dataLength, 0, 4);

                                        int bytesSent = 0;
                                        int bytesLeft = saveData.Length;

                                        while (bytesLeft > 0)       ///Send map to player(s)
                                        {
                                            int curDataSize = System.Math.Min(bufferSize, bytesLeft);

                                            ns.Write(saveData, bytesSent, curDataSize);

                                            bytesSent += curDataSize;
                                            bytesLeft -= curDataSize;
                                        }*/

                    //delete after read and send information 

                    //delete map from CLIENT
                    Directory.Delete(path, true);
                    File.Delete(tempPath);
                }
            }
        }
    }
}
