using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Text;
using Unity.Netcode;
using Cysharp.Threading.Tasks;

public class OnlineLobbyUI : MonoBehaviour
{
    RectTransform menuRect, returnRect;
    Button returnButton, confirmButton;
    Text confirmButtonText, titleText;
    TMP_InputField nameInputField, inputField1, inputField2;
    GameObject hostClientObject, localRelayObject, nameObject, passwordObject;

    const float tweenDuration = 0.1f, tweenInterval = 0.8f;

    enum Page { hostClient, localRelay, name, password }
    Page page;
    bool pageChanged = false;
    void SetPage(Page next)
    {
        page = next;
        pageChanged = true;
    }

    bool selectedHost, selectedLocal;



    async void Start()
    {
        await RelayAllocation.SignInPlayerAsync();

        menuRect = transform.Find("Menu").GetComponent<RectTransform>();
        returnRect = transform.Find("Return").GetComponent<RectTransform>();
        returnButton = returnRect.GetComponent<Button>();
        menuRect.DOScaleY(0, 0);
        returnRect.DOAnchorPosX(-100, 0);
        returnButton.onClick.AddListener(ExitLobby);
        returnButton.interactable = false;

        hostClientObject = menuRect.Find("HostClient").gameObject;
        localRelayObject = menuRect.Find("LocalRelay").gameObject;
        nameObject = menuRect.Find("Name").gameObject;
        passwordObject = menuRect.Find("Password").gameObject;
        hostClientObject.SetActive(true);
        localRelayObject.SetActive(false);
        nameObject.SetActive(false);
        passwordObject.SetActive(false);

        titleText = menuRect.Find("Title").GetComponent<Text>();
        titleText.DOFade(0, 0);

        nameInputField = nameObject.transform.Find("InputField").GetComponent<TMP_InputField>();
        inputField1 = passwordObject.transform.Find("InputField1").GetComponent<TMP_InputField>();
        inputField2 = passwordObject.transform.Find("InputField2").GetComponent<TMP_InputField>();

        var confirmButtonObject = passwordObject.transform.Find("ConfirmButton");
        confirmButton = confirmButtonObject.GetComponent<Button>();
        confirmButtonText = confirmButtonObject.GetComponentInChildren<Text>();
        confirmButton.interactable = false;
        confirmButtonText.color = new Color(1, 0.96f, 1, 0.3f);

        DOVirtual.DelayedCall(0.5f, () =>
        {
            menuRect.DOScaleY(1, tweenDuration)
                .OnComplete(() =>
                {
                    hostClientObject.transform.DOScaleX(1, tweenDuration);
                    titleText.DOFade(0.7f, tweenDuration);
                    page = Page.hostClient;
                });
            returnRect.DOAnchorPosX(100, tweenDuration)
                .OnComplete(() => returnButton.interactable = true);
        }).Play();
    }

    void Update()
    {
        if (!pageChanged) return;

        Sequence sequence = DOTween.Sequence();
        switch (page)
        {
            case Page.hostClient:
                sequence.Append(titleText.DOFade(0, tweenDuration).OnComplete(() => titleText.text = "Multi"));
                sequence.Join(localRelayObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => localRelayObject.SetActive(false)));

                sequence.Join(hostClientObject.transform.DOScaleX(0, 0).OnComplete(() => { hostClientObject.SetActive(true); returnButton.interactable = false; }));
                sequence.AppendInterval(tweenInterval);
                sequence.Append(hostClientObject.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));

                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(ExitLobby);
                break;

            case Page.localRelay:
                sequence.Append(titleText.DOFade(0, tweenDuration).OnComplete(() => titleText.text = "Connection"));
                sequence.Join(hostClientObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => hostClientObject.SetActive(false)));
                sequence.Join(nameObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => nameObject.SetActive(false)));

                sequence.Join(localRelayObject.transform.DOScaleX(0, 0).OnComplete(() => { localRelayObject.SetActive(true); returnButton.interactable = false; }));
                sequence.AppendInterval(tweenInterval);
                sequence.Append(localRelayObject.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));

                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(() => SetPage(Page.hostClient));
                break;

            case Page.name:
                sequence.Append(titleText.DOFade(0, tweenDuration).OnComplete(() => titleText.text = "Name"));
                sequence.Join(localRelayObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => localRelayObject.SetActive(false)));
                sequence.Join(passwordObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => passwordObject.SetActive(false)));

                sequence.Join(nameObject.transform.DOScaleX(0, 0).OnComplete(() => { nameObject.SetActive(true); returnButton.interactable = false; }));
                sequence.AppendInterval(tweenInterval);
                sequence.Append(nameObject.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));

                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(() => SetPage(Page.localRelay));
                break;

            case Page.password:
                sequence.Append(titleText.DOFade(0, tweenDuration)
                .OnComplete(() =>
                {
                    if (selectedHost) titleText.text = "Host";
                    else titleText.text = "Client";
                }));
                sequence.Join(nameObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => nameObject.SetActive(false)));

                sequence.Join(passwordObject.transform.DOScaleX(0, 0).OnComplete(() => { passwordObject.SetActive(true); returnButton.interactable = false; }));
                sequence.AppendInterval(tweenInterval);
                sequence.Append(passwordObject.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));

                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(() => SetPage(Page.name));
                break;
        }
        sequence.Join(titleText.DOFade(0.7f, tweenDuration));
        sequence.Play();

        pageChanged = false;
    }



    public void Host()
    {
        selectedHost = true;
        SortieLobbyUI.selectedHost = selectedHost;
        confirmButtonText.text = "Create Lobby";
        SetPage(Page.localRelay);
    }

    public void Client()
    {
        selectedHost = false;
        SortieLobbyUI.selectedHost = selectedHost;
        confirmButtonText.text = "Enter Lobby";
        SetPage(Page.localRelay);
    }

    public void LocalNetwork()
    {
        selectedLocal = true;
        SetPage(Page.name);
    }

    public void RelayServer()
    {
        selectedLocal = false;
        SetPage(Page.name);
    }

    public void NameInput()
    {
        SetPage(Page.password);
    }

    public void Confirm()
    {
        if (selectedHost)
        {
            // GameNetPortal.I.SetPassword(passwordInputField.text);
            string skillCode;
            int?[] skillIds, skillLevels;
            PlayerInfo.SkillIdGetter(0, out skillIds);
            PlayerInfo.SkillLevelGetter(0, out skillLevels);
            LobbyParticipantData.SkillCodeEncoder(skillIds, skillLevels, out skillCode);
            // string payloadJSON = JsonUtility.ToJson(new ConnectionPayload(passwordInputField.text, nameInputField.text, skillCode));
            string payloadJSON = JsonUtility.ToJson(new ConnectionPayload(nameInputField.text, skillCode));
            byte[] payloadBytes = Encoding.ASCII.GetBytes(payloadJSON);
            NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;
            if (selectedLocal)
            {
                RelayAllocation.SignOutPlayer();
                GameNetPortal.I.StartHost();
            }
            else
            {
                RelayAllocation.AllocateRelayAndConfigureTransportAsHost(this, GameInfo.max_player_count - 1);
            }
        }
        else
        {
            string skillCode;
            int?[] skillIds, skillLevels;
            PlayerInfo.SkillIdGetter(0, out skillIds);
            PlayerInfo.SkillLevelGetter(0, out skillLevels);
            LobbyParticipantData.SkillCodeEncoder(skillIds, skillLevels, out skillCode);
            // var payloadJSON = JsonUtility.ToJson(new ConnectionPayload(passwordInputField.text, nameInputField.text, skillCode));
            var payloadJSON = JsonUtility.ToJson(new ConnectionPayload(nameInputField.text, skillCode));
            byte[] payloadBytes = Encoding.ASCII.GetBytes(payloadJSON);
            NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;
            if (selectedLocal)
            {
                RelayAllocation.SignOutPlayer();
                GameNetPortal.I.StartClient();
            }
            else
            {
                RelayAllocation.ConfigureTransportAsClient(this, inputField1.text);
            }
        }
    }

    void ExitLobby()
    {
        RelayAllocation.SignOutPlayer();
        returnButton.interactable = false;
        returnRect.DOAnchorPosX(-100, tweenDuration);
        hostClientObject.transform.DOScaleX(0, tweenDuration);
        DOVirtual.DelayedCall(tweenDuration, () =>
        {
            menuRect.DOScaleY(0, tweenDuration);
            SceneManager2.I.LoadSceneAsync2(GameScenes.menu, FadeType.gradually, FadeType.left);
        }).Play();
    }

    public void OnValueChangedInputField()
    {
        if (inputField1.text == "" || inputField2.text == "")
        {
            confirmButton.interactable = false;
            confirmButtonText.color = new Color(0, 0.96f, 1, 0.3f);
        }
        else
        {
            confirmButton.interactable = true;
            confirmButtonText.color = new Color(0, 0.96f, 1, 0.7f);
        }
    }
}
