using ClientSubnautica.MultiplayerManager.ReceiveData;
using Newtonsoft.Json.Linq;
using ServerSubnautica;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;


class Server
{
    public static readonly object _lock = new object();
    public static readonly Dictionary<int, TcpClient> list_clients = new Dictionary<int, TcpClient>();
    public static readonly Dictionary<string, List<(string, Vector3)>> player_data = new Dictionary<string, List<(string, Vector3)>>();
///                                    id            name     pos 
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


        ///on crée un  fichier .json et on y ajoute le joeur avec ses data
        location = AppDomain.CurrentDomain.BaseDirectory;
        
        modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        // Loading the user configs.
        configFile = ServerFile(Path.Combine(modFolder, "playerData.json")); ///fichié crée on va donc le modifié


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
                    lock (_lock) player_data.Add("ID NON VALIDE", new List<(string, Vector3)> { (idParts[2], new Vector3(0, 0, 0)) });  //sert just a lancer la boucle 'foreach'
                    foreach (string Newid in player_data.Keys)
                    {
                        if (Newid != idParts[1])
                        {
                            lock (_lock) player_data.Add(idParts[1], new List<(string, Vector3)> { (username, new Vector3(0, 0, 0)) });
                            AddPlayerToServerFile(player_data[idParts[1]], Path.Combine(modFolder, "playerData.json"));
                            Console.WriteLine("New ID saved");
                            break;
                        }
                    }
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

            Thread receiveThread = new Thread(new HandleClient(count).start);
            receiveThread.Start();
            count++;
            Thread.Sleep(5);
        }
    }


    public static JObject ServerFile(string path)
    {
        if (File.Exists(path))
        {
            return JObject.Parse(File.ReadAllText(path));
        }
        else if (path.EndsWith("playerData.json"))
        {
            File.WriteAllText(path,
@"{
    ""INFO"": ""this is the file where the data of all the players are stored please do not touch it""
}");
            return JObject.Parse(File.ReadAllText(path));
        }
        else throw new Exception("The file you're trying to access does not exist, and has no default value.");
    }    ///ce joue OBLIGATOIREMENT, cest lui qui sotcke les données de chaque joueurs connecté


    public static JObject AddPlayerToServerFile(List<(string, Vector3)> playerData, string path)
    {
        Console.WriteLine(path);
        return JObject.Parse(File.ReadAllText(path));
        /*if (File.Exists(path))
        {
            Console.WriteLine("EXISTE");
            return JObject.Parse(File.ReadAllText(path));
        }
        else throw new Exception("The file you're trying to access does not exist, and has no default value.");
        //File.WriteAllText(path, Environment.NewLine + @" ""NEW LINE"": ""this is a new line :)""");
        //return JObject.Parse(File.ReadAllText(path));*/
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