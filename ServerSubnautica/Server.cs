using ClientSubnautica.MultiplayerManager.ReceiveData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServerSubnautica;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Xml.Linq;

class Server
{
    public static readonly object _lock = new object();
    public static readonly Dictionary<int, TcpClient> list_clients = new Dictionary<int, TcpClient>();
    public static readonly Dictionary<int, string> linkPlayer_Client = new Dictionary<int, string>();
    public static readonly Dictionary<string, List<(string, Vector3, Quaternion)>> player_data = new Dictionary<string, List<(string, Vector3, Quaternion)>>();
///                                    id            name     pos       rot
    public static byte[] mapBytes;
    public static string mapName;
    public static JObject configParams;
    public static JObject gameInfo;
    private static string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    public static string mapPath;
    public static string gameInfoPath;


    //Save player data into server file
    public static string location;
    public static string modFolder;
    public static JObject configFile;
    public static string path;

    static void Main(string[] args)
    {
        Server server = new Server();
        configParams = server.loadParam(configPath);
        

        mapName = configParams["MapFolderName"].ToString();
        gameInfoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, mapName, "gameinfo.json");
        mapPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, mapName + ".zip");
        if (!zipFile(mapName))
        {
            Console.WriteLine("Can't compress world");
            Console.WriteLine("Press a key...");
            Console.ReadKey();
            Environment.Exit(1);
        }
        gameInfo = server.loadParam(gameInfoPath);

        mapBytes = getFileBytes(mapPath);

        File.Delete(mapPath);

        string ipAddress = configParams["ipAddress"].ToString();
        int port = int.Parse(configParams["port"].ToString());
        int count = 1;
        IPAddress host = IPAddress.Parse(ipAddress);
        TcpListener ServerSocket = new TcpListener(host, port);
        ServerSocket.Start();
        Console.WriteLine("Listening on "+ ipAddress + ":"+port);


        location = AppDomain.CurrentDomain.BaseDirectory;
        modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);        /// acces way only
        path = Path.Combine(modFolder, "playerData.json");

        configFile = ServerFile(path);

        RememberPlayer(path);

        ///CE JOUE EN BOUCLE UNE FOIS LA MAP CHARGÉ + HOST DU SERVEUR
        while (true)
        {
            TcpClient client = ServerSocket.AcceptTcpClient();

            string playerId = "";
            string username = "PLAYER"; ///Defaults
            int bufferSize = 1024;
            byte[] buffer = new byte[bufferSize];

            NetworkStream ns2 = client.GetStream();
            int bytesRead = ns2.Read(buffer, 0, bufferSize);    ///Lire les données entrantes sur le socket
            string receivedMsg = Encoding.ASCII.GetString(buffer, 0, bytesRead);    ///Convertir les données lues en une chaîne de caractères

            if (!receivedMsg.Contains("/END/"))
                continue;
            try
            {
                string[] parts = receivedMsg.Split(new string[] { "/END/" }, StringSplitOptions.RemoveEmptyEntries);
                string[] idParts = parts[0].Split(':');
                if (idParts[0] == NetworkCMD.getIdCMD("PlayerId"))  ///On voit si le message est le bon     13131313:HunderJ
                {
                    bool exist = false;
                   
                    foreach (string Newid in player_data.Keys)
                    {
                        Console.WriteLine(Newid);
                        if (Newid == idParts[1])
                        {
                            exist = true;
                            break;
                        }
                        Console.WriteLine(exist);
                    }
                    
                    if (!exist) ///only if a new id join the server
                    {
                        Console.WriteLine($"new player has arrived as : {idParts[1]}");
                        player_data.Add(idParts[1], new List<(string, Vector3, Quaternion)> { (idParts[2], new Vector3(0, 0, 0), Quaternion.Identity) });   ///default data
                        AddPlayerToServerFile(player_data[idParts[1]], idParts[1], path);
                    }

                    playerId = idParts[1];
                    username = idParts[2];
                    lock (_lock) linkPlayer_Client.Add(count, playerId);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }

            lock (_lock) list_clients.Add(count, client);
            Console.WriteLine($"{username} join the server");

            Thread receiveThread = new Thread(new HandleClient(count, username).start);
            receiveThread.Start();
            count++;
            Thread.Sleep(5);
        }
    }


    public static JObject ServerFile(string path)  ///ce joue OBLIGATOIREMENT, cest lui qui sotcke les données de chaque joueurs connecté
    {
        if (File.Exists(path))
        {
            return new JObject();       ///File already exist => do nothing
        }
        else if (path.EndsWith("playerData.json"))
        {
            var newFile = File.Create(path);
            newFile.Close();        
            return new JObject();   ///return a new (empty) file.json
        }
        else throw new Exception("The file you're trying to access does not exist, and has no default value.");
    }

    public static JObject AddPlayerToServerFile(List<(string, Vector3, Quaternion)> playerData, string id, string path)    ///ajouter de cette facon ->   ID:playerData
    {
        if (File.Exists(path) && path.EndsWith("playerData.json"))
        {
            bool Exist = false;
            foreach (string testID in File.ReadLines(path))
            {
                string simpleID = testID.Split(':')[0];
                if(simpleID == id)
                {
                    Exist = true;
                    break;
                }
            }

            if(!Exist) //on ajoute la nouvelle id
            {                
                var playerDataJson = new JObject();
                playerDataJson["id"] = id;
                var data = new JArray();
                data.Add(playerData[0].Item1);
                data.Add(new JArray(playerData[0].Item2.X, playerData[0].Item2.Y, playerData[0].Item2.Z));
                var rotation = new JObject();
                rotation["X"] = playerData[0].Item3.X;
                rotation["Y"] = playerData[0].Item3.Y;
                rotation["Z"] = playerData[0].Item3.Z;
                rotation["W"] = playerData[0].Item3.W;
                data.Add(rotation);
                playerDataJson["data"] = data;

                string json = playerDataJson.ToString(Formatting.None);
                File.AppendAllText(path, Environment.NewLine + json);


            }
            return new JObject();
        }
        else throw new Exception("The file you're trying to access does not exist, and has no default value.");
    }

    public static void OnDeconnexion(string id, List<(string, Vector3, Quaternion)> playerData)  ///when player leave -> save his NEW username/pos/rot, etc etc 
    {
        if (File.Exists(path) && path.EndsWith("playerData.json"))
        {

            string[] lines = File.ReadAllLines(path);

            foreach (string line in lines)
            {
                if (line.Length > 0)
                {
                    JObject obj = JObject.Parse(line);
                    string playerId = (string)obj["id"];

                    if (playerId == id) // si l'id correspond à celui que vous cherchez
                    {
                        JToken data = obj["data"];
                        ///change value : 
                        data[0] = playerData[0].Item1;  ///username
                        data[1][0] = playerData[0].Item2.X; ///posX
                        data[1][1] = playerData[0].Item2.Y; ///posY
                        data[1][2] = playerData[0].Item2.Z; ///posZ
                        data[2]["X"] = playerData[0].Item3.X;   ///rotX 
                        data[2]["Y"] = playerData[0].Item3.Y;   ///rotY
                        data[2]["Z"] = playerData[0].Item3.Z;   ///rotZ
                        data[2]["W"] = playerData[0].Item3.W;   ///rotW

                        int lineIndex = Array.IndexOf(lines, line);
                        lines[lineIndex] = $"{{\"id\":\"{id}\",\"data\":[\"{data[0]}\",[{string.Join(",", ((JArray)data[1]).Select(x => ((float)x).ToString("0.0")))}],{{\"X\":{(float)data[2]["X"]},\"Y\":{(float)data[2]["Y"]},\"Z\":{(float)data[2]["Z"]},\"W\":{(float)data[2]["W"]}]}}}}";
                        File.WriteAllLines(path, lines);
                        break;
                    }
                    else
                        Console.WriteLine("no id assigned");
                }
            }
        }
        else throw new Exception("The file you're trying to access does not exist, and has no default value.");
    }

    public static void RememberPlayer(string path)  ///connecter le fichier 'playerData.json' au dictionnaire 'player_data'
    {
        Console.WriteLine("call");
        if (File.Exists(path) && path.EndsWith("playerData.json"))
        {
            string id = "";
            string username = "";
            Vector3 pos = new Vector3();
            Quaternion rot = Quaternion.Identity;

            foreach (string line in File.ReadAllLines(path))
            {
                if (string.IsNullOrEmpty(line)) continue;

                JObject json = JObject.Parse(line);

                id = json["id"].ToString(); ///read ID

                JArray data = (JArray)json["data"];

                username = data[0].ToString(); ///read username

                JArray posArray = (JArray)data[1];///Read position
                pos = new Vector3((float)posArray[0], (float)posArray[1], (float)posArray[2]); 

                JObject rotObj = (JObject)data[2];///Read rotation 
                rot = new Quaternion((float)rotObj["X"], (float)rotObj["Y"], (float)rotObj["Z"], (float)rotObj["W"]);

                Console.WriteLine("player added");
                player_data.Add(id, new List<(string, Vector3, Quaternion)> { (username, pos, rot) });
                Console.WriteLine(id);
            }
        }
    }

    public JObject loadParam(string path)
    {
        return JObject.Parse(File.ReadAllText(path));
    }

    public static bool zipFile(string folderName)
    {
        try
        {
            string[] paths = { AppDomain.CurrentDomain.BaseDirectory, folderName };
            string fullPath = Path.Combine(paths);

            string[] outPath = { AppDomain.CurrentDomain.BaseDirectory, folderName + ".zip" };
            string outFullPath = Path.Combine(outPath);
            string startPath = fullPath;
            string zipPath = outFullPath;

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(startPath, zipPath);
            return true;
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    public static byte[] getFileBytes(string path)
    {
        return File.ReadAllBytes(path);
    }
}