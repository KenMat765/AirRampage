using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class SkillDeckList : MonoBehaviour
{
    [SerializeField] RectTransform glowRect;
    [SerializeField] Image infoBoardImg;
    [SerializeField] Image cirkit_img;

    TextMeshProUGUI explain_text, name_text, type_text, level_text;

    GameObject coin_obj;
    TextMeshProUGUI coinHaveText, coinNeedText;

    Image glow_img, equip_btn_img, level_meter_img;
    Button equip_btn;
    TextMeshProUGUI equip_btn_text;

    public const int num_in_page = 3 * 4;
    RectTransform[] icon_rects = new RectTransform[num_in_page];
    Image[] icon_imgs = new Image[num_in_page];
    Image[] skill_imgs = new Image[num_in_page];

    TextMeshProUGUI[] feature_texts = new TextMeshProUGUI[4];

    int total_page_count = 3;
    int current_page_num = 0;
    int? current_icon_index = null;
    public int? current_skill_id { get; private set; } = null;

    public bool is_deck { get; set; }


    void Start()
    {
        infoBoardImg.color = Color.white;
        cirkit_img.color = Color.gray;

        type_text = infoBoardImg.transform.Find("Type").GetComponent<TextMeshProUGUI>();
        name_text = infoBoardImg.transform.Find("Name").GetComponent<TextMeshProUGUI>();
        explain_text = infoBoardImg.transform.Find("Explanation").GetComponent<TextMeshProUGUI>();
        level_text = infoBoardImg.transform.Find("Level_Meter/Level_Number").GetComponent<TextMeshProUGUI>();
        type_text.text = ""; name_text.text = ""; explain_text.text = ""; level_text.text = "";

        coin_obj = infoBoardImg.transform.Find("Coin").gameObject;
        coinHaveText = coin_obj.transform.Find("CoinHave").GetComponent<TextMeshProUGUI>();
        coinNeedText = coin_obj.transform.Find("CoinNeed").GetComponent<TextMeshProUGUI>();
        coin_obj.SetActive(false);
        coinHaveText.text = PlayerInfo.I.coins.ToString();
        coinNeedText.text = "";

        glow_img = glowRect.GetComponent<Image>();
        glow_img.color = Color.clear;
        Transform equip_trans = infoBoardImg.transform.Find("Equip");
        equip_btn_img = equip_trans.GetComponent<Image>();
        equip_btn = equip_trans.GetComponent<Button>();
        equip_btn_text = equip_trans.GetComponentInChildren<TextMeshProUGUI>();
        equip_btn_img.color = Color.white;
        equip_btn.interactable = false;
        equip_btn_text.text = "";
        level_meter_img = infoBoardImg.transform.Find("Level_Meter").GetComponent<Image>();
        level_meter_img.fillAmount = 0;

        for (int k = 0; k < 4; k++)
        {
            Transform feature_trans = infoBoardImg.transform.Find($"Features/Feature{k + 1}");
            feature_texts[k] = feature_trans.Find("Text").GetComponent<TextMeshProUGUI>();
            feature_texts[k].color = Color.clear;
        }

        for (int order = 0; order < num_in_page; order++)
        {
            GameObject icon_obj = transform.Find("Icon" + order).gameObject;
            icon_rects[order] = icon_obj.GetComponent<RectTransform>();
            icon_imgs[order] = icon_obj.GetComponent<Image>();
            icon_imgs[order].raycastTarget = false;
            skill_imgs[order] = icon_obj.GetComponentsInChildrenWithoutSelf<Image>()[0];
        }

        RefreshIcons(0);
    }



    // skill_id = page * num_in_page + icon_index
    public void OnSelectIcon(int order)
    {
        if (current_icon_index == order)
        {
            ResetInfoBoard();
        }
        else
        {
            current_icon_index = order;
            current_skill_id = current_page_num * num_in_page + order;

            glowRect.position = icon_rects[order].position;
            glow_img.color = new Color(1, 1, 1, 0.5f);

            RefreshInfoBoard((int)current_skill_id);
        }
    }

    public void GoToNextList(int direction)
    {
        int next_page_num = ((current_page_num + direction) % total_page_count + total_page_count) % total_page_count;
        RefreshIcons(next_page_num);
        ResetInfoBoard();
        current_page_num = next_page_num;
    }


    public void OnEnter()
    {
        RefreshIcons(current_page_num);
        if (is_deck) // Set skill to deck
        {
            coinHaveText.text = PlayerInfo.I.coins.ToString();
            coin_obj.SetActive(false);
            equip_btn_text.text = "Set";
        }
        else // Upgrade skill
        {
            coin_obj.SetActive(true);
            equip_btn_text.text = "Upgrade";
        }
    }

    public void OnExit()
    {
        for (int order = 0; order < num_in_page; order++) icon_imgs[order].raycastTarget = false;
        ResetInfoBoard();
        equip_btn_text.text = "";
        current_page_num = 0;
    }


    public void RefreshInfoBoard(int skill_id)
    {
        SkillData data = SkillDatabase.I.SearchSkillById((int)current_skill_id);
        int current_skill_level = PlayerInfo.I.skl_level[(int)current_skill_id];

        cirkit_img.fillAmount = 0;
        cirkit_img.color = data.GetColor();

        type_text.color = new Color(1, 1, 1, 0);
        name_text.color = new Color(1, 1, 1, 0);
        explain_text.color = new Color(1, 1, 1, 0);
        coinNeedText.color = new Color(1, 1, 1, 0);
        level_text.color = new Color(1, 1, 1, 0);
        level_meter_img.fillAmount = 0;

        switch (data.GetSkillType())
        {
            case SkillType.attack: type_text.text = "Attack"; break;
            case SkillType.heal: type_text.text = "Heal"; break;
            case SkillType.assist: type_text.text = "Assist"; break;
            case SkillType.disturb: type_text.text = "Disturb"; break;
            default: type_text.text = "Null"; break;
        }
        name_text.text = data.GetName();
        level_text.text = current_skill_level.ToString();

        string[] features;
        if (is_deck)
        {
            equip_btn.interactable = true;
            features = data.GetFeatures();
            explain_text.text = data.GetInfomation();
        }
        else
        {
            LevelData l_data = SkillLevelDatabase.I.SearchSkillById((int)current_skill_id).GetLevelData(current_skill_level);
            features = l_data.EnhanceDetails;
            if (current_skill_level < 5) // Under max level
            {
                coin_obj.SetActive(true);
                int coin_have = PlayerInfo.I.coins;
                int coin_need = GameInfo.upgrade_coin[current_skill_level - 1];
                if (coin_have < coin_need) // Not enough coins
                {
                    equip_btn.interactable = false;
                    coinHaveText.color = Color.red;
                }
                else // Enough coins
                {
                    equip_btn.interactable = true;
                    coinHaveText.color = Color.white;
                }
                coinHaveText.text = coin_have.ToString();
                coinNeedText.text = coin_need.ToString();
                explain_text.text = "";
            }
            else // Reached max level
            {
                equip_btn.interactable = false;
                coin_obj.SetActive(false);
                explain_text.text = "Maximum Level Reached";
            }
        }
        for (int k = 0; k < 4; k++)
        {
            feature_texts[k].color = Color.clear;
            feature_texts[k].text = features[k];
        }

        DOTween.CompleteAll();
        const float d1 = 0.15f;
        const float d2 = 0.15f;
        cirkit_img.DOFillAmount(1, d1)
            .OnComplete(() =>
            {
                type_text.DOFade(1, d2);
                name_text.DOFade(1, d2);
                explain_text.DOFade(1, d2);
                coinNeedText.DOFade(1, d2);
                level_text.DOFade(1, d2);
                infoBoardImg.DOColor(data.GetColor(), d2);
                equip_btn_img.DOColor(data.GetColor(), d2);
                level_meter_img.DOFillAmount(current_skill_level / 5.0f, d2);
                for (int k = 0; k < 4; k++)
                {
                    if (feature_texts[k].text == "") continue;
                    feature_texts[k].DOColor(Color.white, d2);
                }
            });
    }

    void RefreshIcons(int page_num)
    {
        for (int order = 0; order < num_in_page; order++)
        {
            SkillData data = SkillDatabase.I.SearchSkillByPageOrder(page_num, order);
            if (data == null)
            {
                icon_imgs[order].color = Color.clear;
                skill_imgs[order].color = Color.clear;
                icon_imgs[order].raycastTarget = false;
            }
            else
            {
                if (!PlayerInfo.I.skl_unlock[data.GetId()])
                {
                    icon_imgs[order].color = new Color(1, 1, 1, 0.5f);
                    skill_imgs[order].color = Color.clear;
                    icon_imgs[order].raycastTarget = false;
                }
                else
                {
                    icon_imgs[order].color = data.GetColor();
                    skill_imgs[order].color = new Color(1, 1, 1, 0.45f);
                    skill_imgs[order].sprite = data.GetSprite();
                    icon_imgs[order].raycastTarget = true;
                }
            }
        }
    }

    void ResetInfoBoard()
    {
        DOTween.CompleteAll();
        const float d = 0.15f;
        infoBoardImg.DOColor(Color.white, d);
        equip_btn_img.DOColor(Color.white, d);
        equip_btn.interactable = false;
        cirkit_img.DOColor(Color.gray, d);
        type_text.text = "";
        name_text.text = "";
        explain_text.text = "";
        coinNeedText.text = "";
        level_text.text = "";
        level_meter_img.DOFillAmount(0, d);
        glow_img.color = Color.clear;
        current_icon_index = null;
        current_skill_id = null;
        for (int k = 0; k < 4; k++)
        {
            feature_texts[k].color = Color.clear;
        }
        coin_obj.SetActive(false);
    }
}