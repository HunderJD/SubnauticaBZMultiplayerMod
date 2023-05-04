using ClientSubnautica.MultiplayerManager;
using HarmonyLib;
using System;
using System.Diagnostics.Tracing;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ClientSubnautica
{
    class AddMenuButton
    {
        [HarmonyPatch(typeof(MainMenuRightSide), "OpenGroup")]
        public class Patches
        {
            [HarmonyPostfix]
            static void Postfix(string target, MainMenuRightSide __instance)
            {
                if (GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu") == null)
                {
                    if (target == "SavedGames")
                    {
                        GameObject GOserverAdress = GameObject.Instantiate(GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/Home/EmailBox"));
                        GOserverAdress.name = "MultiplayerMenu";
                        GOserverAdress.transform.parent = GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/").transform;

                        GOserverAdress.transform.position = new Vector3(GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/").transform.position.x, 0.103f, (float)(GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/").transform.position.z + 0.33f));
                        GOserverAdress.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                        GOserverAdress.transform.transform.rotation = (GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/Home/EmailBox").transform.rotation);

                        GameObject GONickNameMenu = GameObject.Instantiate(GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/Home/EmailBox"));
                        GONickNameMenu.name = "NicknameMenu";
                        GONickNameMenu.transform.parent = GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/").transform;

                        GONickNameMenu.transform.position = new Vector3(GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/").transform.position.x, -0.025f, (float)(GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/").transform.position.z + 0.33f));
                        GONickNameMenu.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                        GONickNameMenu.transform.transform.rotation = (GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/Home/EmailBox").transform.rotation);

                        GameObject.Destroy(GOserverAdress.FindChild("HeaderText").GetComponent<TranslationLiveUpdate>());
                        GameObject.Destroy(GONickNameMenu.FindChild("HeaderText").GetComponent<TranslationLiveUpdate>());

                        GameObject.Destroy(GOserverAdress.transform.Find("SubscriptionSuccess/Text").GetComponent<TranslationLiveUpdate>());
                        GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/SubscriptionSuccess/Text").GetComponent<TextMeshProUGUI>().text = "Server found !";

                        GameObject.Destroy(GOserverAdress.transform.Find("SubscriptionInProgress/Text").GetComponent<TranslationLiveUpdate>());
                        GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/SubscriptionInProgress/Text").GetComponent<TextMeshProUGUI>().text = "Searching server...";

                        GameObject.Destroy(GOserverAdress.transform.Find("SubscriptionError/Text").GetComponent<TranslationLiveUpdate>());
                        GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/SubscriptionError/Text").GetComponent<TextMeshProUGUI>().text = "Server not found";

                        GOserverAdress.FindChild("Subscribe").GetComponent<Button>().onClick.AddListener(() =>
                        {
                            //GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/SubscriptionInProgress").SetActive(true);
                            InitializeConnection test = new InitializeConnection();
                            try
                            {
                                test.start(GOserverAdress.FindChild("InputField").GetComponent<TMP_InputField>().text);
                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/SubscriptionInProgress").SetActive(false);

                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/SubscriptionSuccess").SetActive(true);
                            }
                            catch (SocketException e)
                            {
                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/SubscriptionInProgress").SetActive(false);

                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/SubscriptionError").SetActive(true);
                            }
                            catch (FormatException e)
                            {
                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/SubscriptionInProgress").SetActive(false);

                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/SubscriptionError/Text").GetComponent<TextMeshProUGUI>().text = "Invalid address ip";
                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/SubscriptionError").SetActive(true);
                            }
                            catch (IndexOutOfRangeException e)
                            {
                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/SubscriptionInProgress").SetActive(false);

                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/SubscriptionError/Text").GetComponent<TextMeshProUGUI>().text = "No port specified or wrong ip address format";
                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/SubscriptionError").SetActive(true);
                            }
                            catch (Exception e)
                            {
                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/SubscriptionInProgress").SetActive(false);

                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/SubscriptionError/Text").GetComponent<TextMeshProUGUI>().text = "Uknown error occured.";
                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/SubscriptionError").SetActive(true);
                            }
                        });

                        GONickNameMenu.FindChild("Subscribe").GetComponent<Button>().onClick.AddListener(() =>
                        {
                            GameObject myNickNameBox = GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/NicknameMenu/InputField");

                            if (myNickNameBox.GetComponent<TMP_InputField>().text == "")
                            {
                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/NicknameMenu/SubscriptionInProgress").SetActive(false);

                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/NicknameMenu/SubscriptionError/Text").GetComponent<TextMeshProUGUI>().text = "incorrect username (no caracter)";
                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/NicknameMenu/SubscriptionError").SetActive(true);
                            }
                            else if (myNickNameBox.GetComponent<TMP_InputField>().text.Length <= 0)
                            {
                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/NicknameMenu/SubscriptionInProgress").SetActive(false);

                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/NicknameMenu/SubscriptionError/Text").GetComponent<TextMeshProUGUI>().text = "incorrect username (wrong lenght)";
                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/NicknameMenu/SubscriptionError").SetActive(true);
                            }
                            else
                            {
                                var pseudo = myNickNameBox.GetComponent<TMP_InputField>().text; ///On assigne le nouveaux pseudo a une VAR
                                MainPatcher.ChangeNickName(pseudo);
                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/NicknameMenu/SubscriptionInProgress").SetActive(false);
                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/NicknameMenu/SubscriptionSuccess/Text").GetComponent<TextMeshProUGUI>().text = "new pseudo : " + pseudo;

                                GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/NicknameMenu/SubscriptionSuccess").SetActive(true);
                                ///Le pseudo ne se supprime pas 
                            }


                        });

                        GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/HeaderText").GetComponent<TextMeshProUGUI>().text = "Join a server";
                        GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/InputField/Placeholder").GetComponent<TextMeshProUGUI>().text = "Enter the ip adress of the server...";
                        GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/InputField").GetComponent<TMP_InputField>().text = "127.0.0.1:5000";

                        GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/NicknameMenu/HeaderText").GetComponent<TextMeshProUGUI>().text = "My username";
                        GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/NicknameMenu/InputField/Placeholder").GetComponent<TextMeshProUGUI>().text = "Enter your new nickname";
                        GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/NicknameMenu/InputField").GetComponent<TMP_InputField>().text = MainPatcher.username;

                        GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu/ViewPastUpdates/").SetActive(false);
                        GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/NicknameMenu/ViewPastUpdates/").SetActive(false);

                        uGUI_InputField playerNameInputField = GOserverAdress.GetComponent<uGUI_InputField>();

                        SceneManager.MoveGameObjectToScene(GOserverAdress, SceneManager.GetSceneByName("XMenu"));

                        GOserverAdress.SetActive(true);
                        GONickNameMenu.SetActive(true);
                    }
                }
                else
                {
                    GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/MultiplayerMenu").SetActive(target == "SavedGames");
                    GameObject.Find("Menu canvas/Panel/MainMenu/RightSide/NicknameMenu").SetActive(target == "SavedGames");
                }
            }
        }
    }
}

