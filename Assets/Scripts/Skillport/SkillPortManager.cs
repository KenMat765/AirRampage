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
    Image cirkit_img;

    const float wait_time = 1.2f;
    const float enter_exit_duration = 0.1f;
    const float change_interval = 0.8f;
    const float cirkit_duration = 0.2f;

    [SerializeField] SkillDeck skillDeck;
    [SerializeField] SkillDeckList skillDeckList;

    enum Page { menu, deck, gear, deck_list, gear_list }
    Page current_page = Page.menu;



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

        cirkit_img = list_rect.Find("Cirkit").GetComponent<Image>();
        cirkit_img.fillAmount = 0;
        cirkit_img.color = Color.grey;

        Utilities.DelayCall(this, wait_time, () =>
        {
            EnterButtons();
            EnterReturn_Glass();
        });
    }



    // ↓ 階層遷移を伴う処理 ↓ ////////////////////////////////////////////////////////////////////////////////////////////////
    public void OnSelectDeckBtn()
    {
        ExitButtons();
        Utilities.DelayCall(this, change_interval, EnterDeck);
    }

    public void OnSelectGearBtn()
    {
        ExitButtons();
        Utilities.DelayCall(this, change_interval, EnterGear);
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
                    SceneManager2.I.LoadSceneAsync2(GameScenes.menu, FadeType.bottom);
                });
                break;

            case Page.deck:
                skillDeck.OnExit();
                ExitDeck();
                SaveManager.SaveData<PlayerInfo>(PlayerInfo.I);
                Utilities.DelayCall(this, change_interval, EnterButtons);
                break;

            case Page.gear:
                ExitGear();
                Utilities.DelayCall(this, change_interval, EnterButtons);
                break;

            case Page.deck_list:
                skillDeckList.OnExit();
                ExitInfoList();
                Utilities.DelayCall(this, change_interval, EnterDeck);
                break;

            case Page.gear_list:
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

    void EnterGear()
    {
        current_page = Page.gear;
    }

    void ExitGear()
    {
        return_btn.interactable = false;
    }

    void EnterInfoList()
    {
        current_page = Page.deck_list;
        info_rect.DOAnchorPosX(-480, enter_exit_duration);
        list_rect.DOAnchorPosX(432, enter_exit_duration)
            .OnComplete(() => cirkit_img.DOFillAmount(1, cirkit_duration)
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
        cirkit_img.DOFillAmount(0, cirkit_duration)
            .OnComplete(() =>
            {
                info_rect.LetOutRect(Direction.left, 350, enter_exit_duration);
                list_rect.LetOutRect(Direction.right, 500, enter_exit_duration);
            });
    }
}