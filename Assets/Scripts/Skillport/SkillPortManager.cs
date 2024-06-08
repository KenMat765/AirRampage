using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SkillPortManager : Singleton<SkillPortManager>
{
    protected override bool dont_destroy_on_load { get; set; } = false;
    [SerializeField] Image glass_img;
    [SerializeField] RectTransform right_btn_rect;
    [SerializeField] RectTransform left_btn_rect;
    [SerializeField] RectTransform deck_rect;
    [SerializeField] RectTransform skilldeck_btn_rect;
    [SerializeField] RectTransform skillgear_btn_rect;
    [SerializeField] RectTransform return_rect;
    Button skilldeck_btn;
    Button skillgear_btn;
    Button return_btn;
    [SerializeField] RectTransform info_rect;
    [SerializeField] RectTransform list_rect;
    Image circuit_img;

    const float wait_time = 1.2f;
    const float enter_exit_duration = 0.1f;
    const float change_interval = 0.8f;
    const float circuit_duration = 0.2f;

    [SerializeField] SkillDeck skillDeck;
    [SerializeField] SkillDeckList skillDeckList;

    enum Page { menu, deck, deck_list, gear_list }
    Page current_page = Page.menu;

    bool is_deck = false;


    void Start()
    {
        glass_img.color = Color.clear;

        skilldeck_btn = skilldeck_btn_rect.GetComponent<Button>();
        skillgear_btn = skillgear_btn_rect.GetComponent<Button>();
        return_btn = return_rect.GetComponent<Button>();

        skilldeck_btn.interactable = false;
        skillgear_btn.interactable = false;
        return_btn.interactable = false;

        skilldeck_btn_rect.anchoredPosition = new Vector2(-300, 0);
        skillgear_btn_rect.anchoredPosition = new Vector2(300, 0);
        return_rect.anchoredPosition = new Vector2(-100, -120);

        circuit_img = list_rect.Find("Circuit").GetComponent<Image>();
        circuit_img.fillAmount = 0;
        circuit_img.color = Color.grey;

        Utilities.DelayCall(this, wait_time, () =>
        {
            EnterButtons();
            EnterReturn_Glass();
        });
    }



    // ↓ 階層遷移を伴う処理 ↓ ////////////////////////////////////////////////////////////////////////////////////////////////
    public void OnSelectDeckBtn()
    {
        is_deck = true;
        ExitButtons();
        Utilities.DelayCall(this, change_interval, EnterDeck);
    }

    public void OnSelectGearBtn()
    {
        is_deck = false;
        ExitButtons();
        Utilities.DelayCall(this, enter_exit_duration + change_interval, EnterInfoList);
    }

    public void Return()
    {
        switch (current_page)
        {
            case Page.menu:
                ExitButtons();
                ExitReturn_Glass();
                Utilities.DelayCall(this, wait_time, () =>
                {
                    SceneManager2.I.LoadSceneAsync2(GameScenes.MENU, FadeType.bottom);
                });
                break;

            case Page.deck:
                skillDeck.OnExit();
                ExitDeck();
                SaveManager.SaveData<PlayerInfo>(PlayerInfo.I);
                Utilities.DelayCall(this, change_interval, EnterButtons);
                break;

            case Page.deck_list:
                skillDeckList.OnExit();
                ExitInfoList();
                Utilities.DelayCall(this, change_interval, EnterDeck);
                break;

            case Page.gear_list:
                skillDeckList.OnExit();
                ExitInfoList();
                Utilities.DelayCall(this, change_interval, EnterButtons);
                break;
        }
    }

    public void OnSelectSetInDeck()
    {
        skillDeck.OnExit();
        ExitDeck();
        Utilities.DelayCall(this, enter_exit_duration + change_interval, EnterInfoList);
    }

    public void OnSelectEquip()
    {
        // Set selected skill to deck.
        if (is_deck)
        {
            int deck_num = skillDeck.current_deck_num;
            int?[] skillIds = new int?[GameInfo.max_skill_count];
            PlayerInfo.I.SkillIdsGetter(deck_num, out skillIds);
            for (int k = 0; k < GameInfo.max_skill_count; k++)
            {
                if (skillIds[k] == skillDeckList.current_skill_id)
                {
                    PlayerInfo.I.SkillIdSetter(deck_num, k, null);
                }
            }
            PlayerInfo.I.SkillIdSetter(deck_num, skillDeck.selected_icon_index, skillDeckList.current_skill_id);

            skillDeckList.OnExit();
            ExitInfoList();
            Utilities.DelayCall(this, change_interval, EnterDeck);
        }

        // Level up selected skill
        else
        {
            int skill_id = (int)skillDeckList.current_skill_id;
            UpgradeSkill(skill_id);
            skillDeckList.RefreshInfoBoard(skill_id);
        }
    }

    void UpgradeSkill(int skill_id)
    {
        // Level up skill
        int current_level = PlayerInfo.I.skl_level[skill_id];
        int new_level = current_level + 1;
        new_level = Mathf.Clamp(new_level, 1, 5);
        PlayerInfo.I.skl_level[skill_id] = new_level;
        // Reduce coins
        int cost = GameInfo.upgrade_coin[current_level - 1];
        PlayerInfo.I.coins -= cost;
        // Save PlayerInfo
        SaveManager.SaveData<PlayerInfo>(PlayerInfo.I);
    }



    // ↓ UI移動系メソッド群 ↓ ////////////////////////////////////////////////////////////////////////////////////////////////
    void EnterButtons()
    {
        current_page = Page.menu;
        skilldeck_btn_rect.DOAnchorPosX(600, enter_exit_duration);
        skillgear_btn_rect.DOAnchorPosX(-600, enter_exit_duration)
            .OnComplete(() =>
            {
                return_btn.interactable = true;
                skilldeck_btn.interactable = true;
                skillgear_btn.interactable = true;
            });
    }

    void ExitButtons()
    {
        return_btn.interactable = false;
        skilldeck_btn.interactable = false;
        skillgear_btn.interactable = false;
        skilldeck_btn_rect.LetOutRect(Direction.left, 300, enter_exit_duration);
        skillgear_btn_rect.LetOutRect(Direction.right, 300, enter_exit_duration);
    }

    void EnterReturn_Glass()
    {
        glass_img.DOFade(1, enter_exit_duration);
        return_rect.DOAnchorPosX(120, enter_exit_duration);
    }

    void ExitReturn_Glass()
    {
        glass_img.DOFade(0, enter_exit_duration);
        return_rect.LetOutRect(Direction.left, 100, enter_exit_duration);
    }

    void EnterDeck()
    {
        current_page = Page.deck;

        // Wait until deck is refreshed.
        skillDeck.RefreshIcons(skillDeck.current_deck_num);

        deck_rect.DOAnchorPosY(80, enter_exit_duration);
        right_btn_rect.DOAnchorPosX(-140, enter_exit_duration);
        left_btn_rect.DOAnchorPosX(140, enter_exit_duration)
            .OnComplete(() =>
            {
                return_btn.interactable = true;
                skillDeck.OnEnter();
            });
    }

    void ExitDeck()
    {
        return_btn.interactable = false;
        skillDeck.OnExit();
        deck_rect.LetOutRect(Direction.up, 200, enter_exit_duration);
        right_btn_rect.LetOutRect(Direction.right, 100, enter_exit_duration);
        left_btn_rect.LetOutRect(Direction.left, 100, enter_exit_duration);
    }

    void EnterInfoList()
    {
        if (is_deck)
            current_page = Page.deck_list;
        else
            current_page = Page.gear_list;
        skillDeckList.is_deck = is_deck;
        info_rect.DOAnchorPosX(-470, enter_exit_duration);
        list_rect.DOAnchorPosX(432, enter_exit_duration)
            .OnComplete(() => circuit_img.DOFillAmount(1, circuit_duration)
                .OnComplete(() =>
                {
                    return_btn.interactable = true;
                    skillDeckList.OnEnter();
                }));
    }

    void ExitInfoList()
    {
        return_btn.interactable = false;
        skillDeckList.OnExit();
        circuit_img.DOFillAmount(0, circuit_duration)
            .OnComplete(() =>
            {
                info_rect.LetOutRect(Direction.left, 350, enter_exit_duration);
                list_rect.LetOutRect(Direction.right, 500, enter_exit_duration);
            });
    }
}