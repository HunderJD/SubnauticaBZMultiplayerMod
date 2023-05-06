using HarmonyLib;
using QModManager.API.ModLoading;
using System;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ClientSubnautica
{
    [QModCore]
    public static class MainPatcher
    {
        public static string location;
        public static string modFolder;
        public static string id;
        public static string username;
        public static JObject configFile;


        [QModPatch]
        public static void Patch()
        {
            location = AppDomain.CurrentDomain.BaseDirectory;
            modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // Loading the user configs.
            configFile = LoadParam(Path.Combine(modFolder, "player.json"));
            string playerID = configFile["playerID"].ToString();
            id = configFile["playerID"].ToString();
            username = configFile["nickname"].ToString();

            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            string text = "dam_" + executingAssembly.GetName().Name;
            new Harmony(text).PatchAll(executingAssembly);
        }

        /// ON CRÉE une seul fois le fichier, avec une ID et un PSEUDO prédéfinis (PLAYER + ID)
        /// quand le joueur veut modifié son pseudo il passe par 'AddMenuButton.cs' qui appelle la fonction 'ChangeNickName()' et qui change JUSTE le nickname par le contenue.text 

        public static JObject LoadParam(string path)
        {
            if (File.Exists(path))
            {
                return JObject.Parse(File.ReadAllText(path));
            }
            else if (path.EndsWith("player.json"))
            {
                var id = GenerateID();
                File.WriteAllText(path,
@"{
    ""WARNING"": ""DO NOT CHANGE OR DELETE THE ID OR YOU WILL LOSE ALL YOUR PROGRESSIONS ON EVERY SERVEUR (You can modify your pseudo trougth the main menu)"",
    ""playerID"": """ + id + @""",
    ""nickname"": """ + username + id + @"""
}");
                return JObject.Parse(File.ReadAllText(path));
            }
            else throw new Exception("The file you're trying to access does not exist, and has no default value.");
        }

        public static void ChangeNickName(string NewPseudo)
        {
            ///JObject configFile = CONFIGFILE !!!!

            // Mettre à jour la propriété "nickname" avec la nouvelle valeur
            configFile["nickname"] = NewPseudo;
            username = NewPseudo;

            // Enregistrer les modifications dans le fichier
            File.WriteAllText(Path.Combine(modFolder, "player.json"), configFile.ToString());
        }

        public static string GenerateID()
        {
            var tid = Process.GetCurrentProcess().Id.ToString() + ((int)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds).ToString();
            return tid;
        }
    }
}