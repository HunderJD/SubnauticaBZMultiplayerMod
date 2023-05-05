using ClientSubnautica.MultiplayerManager.ReceiveData;
using Newtonsoft.Json.Linq;
using ServerSubnautica;
using System;
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


class Server
{
    public static readonly object _lock = new object();
    public static readonly Dictionary<int, TcpClient> list_clients = new Dictionary<int, TcpClient>();
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
        modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);        /// <- acces way only
        configFile = ServerFile(Path.Combine(modFolder, "playerData.json"));

        RememberPlayer(Path.Combine(modFolder, "playerData.json"));


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

                if (idParts[0] == NetworkCMD.getIdCMD("PlayerId"))  ///On voit si le message est le bon
                {
                    player_data.Add("ID NON VALIDE", new List<(string, Vector3, Quaternion)> { (idParts[2], new Vector3(0, 0, 0), Quaternion.Identity)});  //sert just a lancer la boucle 'foreach'
                    foreach (string Newid in player_data.Keys)
                    {
                        if (Newid != idParts[1])        //a refaire
                        {
                            Console.WriteLine($"new player has arrived : {idParts[1]}");
                            player_data.Add(idParts[1], new List<(string, Vector3, Quaternion)> {(idParts[2], new Vector3(0, 0, 0), Quaternion.Identity) });
                            AddPlayerToServerFile(player_data[idParts[1]], idParts[1], Path.Combine(modFolder, "playerData.json"));
                            break;
                        }
                    }
                    player_data.Remove("ID NON VALIDE");

                    playerId = idParts[1];
                    username = idParts[2];
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }

            lock (_lock) list_clients.Add(count, client);
            Console.WriteLine($"{username} join the server, id:{playerId}");

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
                string json = $@"{id}:{playerData[0]}";
                File.AppendAllText(path, Environment.NewLine + json);
            }
            return new JObject();
        }
        else throw new Exception("The file you're trying to access does not exist, and has no default value.");
    }


    public static void RememberPlayer(string path) ///connéecte le fichier 'playerData.json' au dictionnaire 'player_data'
    {
        if (File.Exists(path) && path.EndsWith("playerData.json"))
        {
            string id = "";
            string username = "";
            Vector3 pos = new Vector3();
            Quaternion rot = Quaternion.Identity;

            foreach (string idByLine in File.ReadAllLines(path))
            {
                if (idByLine.Length > 0)   ///skip the first empty line
                {
                    id = idByLine.Split(':')[0];
                    username = idByLine.Split(':')[1].Split('(')[1].Split(',')[0];
                    string posSTR = idByLine.Split('<', '>')[1];
                    string rotSTR = idByLine.Split('{', '}')[1];

                    string[] posComponents = posSTR.Split(' ');
                    pos = new Vector3(float.Parse(posComponents[0]), float.Parse(posComponents[1]), float.Parse(posComponents[2]));

                    string[] rotComponents = rotSTR.Split(' ');
                    rot = new Quaternion(float.Parse(rotComponents[0].Split(':')[1]), float.Parse(rotComponents[1].Split(':')[1]), float.Parse(rotComponents[2].Split(':')[1]), float.Parse(rotComponents[3].Split(':')[1]));

                    Console.WriteLine("player sync");
                    player_data.Add(id, new List<(string, Vector3, Quaternion)> { (username, pos, rot) });
                }
            }
            
            Console.WriteLine(player_data.Count);
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