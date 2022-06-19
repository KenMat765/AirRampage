using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SkillDeckList : MonoBehaviour
{
    [SerializeField] RectTransform glowRect;
    [SerializeField] Image infoBoardImg;
    [SerializeField] Image cirkit_img;

    Text type_text, name_text, explain_text;

    Image glow_img;

    public const int num_in_page = 3 * 4;
    RectTransform[] icon_rects = new RectTransform[num_in_page];
    Image[] icon_imgs = new Image[num_in_page];
    Image[] skill_imgs = new Image[num_in_page];

    Image equip_btn_img;

    int total_page_count = 3;
    int current_page_num = 0;
    int? current_icon_index = null;
    public int? current_skill_id { get; private set; } = null;



    void Start()
    {
        infoBoardImg.color = Color.white;
        cirkit_img.color = Color.gray;

        type_text = infoBoardImg.transform.Find("Type").GetComponent<Text>();
        name_text = infoBoardImg.transform.Find("Name").GetComponent<Text>();
        explain_text = infoBoardImg.transform.Find("Explain").GetComponent<Text>();
        type_text.text = ""; name_text.text = ""; explain_text.text = "";

        glow_img = glowRect.GetComponent<Image>();
        glow_img.color = Color.clear;

        for (int order = 0; order < num_in_page; order++)
        {
            GameObject icon_obj = transform.Find("Icon" + order).gameObject;
            icon_rects[order] = icon_obj.GetComponent<RectTransform>();
            icon_imgs[order] = icon_obj.GetComponent<Image>();
            icon_imgs[order].raycastTarget = false;
            skill_imgs[order] = icon_obj.GetComponentsInChildrenWithoutSelf<Image>()[0];
        }

        equip_btn_img = infoBoardImg.transform.Find("Equip").GetComponent<Image>();
        equip_btn_img.color = Color.grey;
        equip_btn_img.raycastTarget = false;
    }



    // skill_id = page * num_in_page + icon_index
    public void OnSelectIcon(int order)
    {
        if (current_icon_index == order) ResetInfoBoard();
        else
        {
            current_icon_index = order;
            current_skill_id = current_page_num * num_in_page + order;

            glowRect.position = icon_rects[order].position;
            glow_img.color = new Color(1, 1, 1, 0.5f);

            equip_btn_img.raycastTarget = true;

            SkillData data = SkillDatabase.I.SearchSkillById((int)current_skill_id);

            cirkit_img.fillAmount = 0;
            cirkit_img.color = data.GetColor();

            type_text.color = new Color(1, 1, 1, 0);
            name_text.color = new Color(1, 1, 1, 0);
            explain_text.color = new Color(1, 1, 1, 0);

            switch (data.GetSkillType())
            {
                case SkillType.attack:
                    type_text.text = "攻撃系";
                    break;

                case SkillType.heal:
                    type_text.text = "回復系";
                    break;

                case SkillType.assist:
                    type_text.text = "補助系";
                    break;

                case SkillType.disturb:
                    type_text.text = "妨害系";
                    break;

                default:
                    type_text.text = "Null";
                    break;
            }
            name_text.text = data.GetNameJp();
            explain_text.text = data.GetInfomation();

            DOTween.CompleteAll();
            const float d1 = 0.15f;
            const float d2 = 0.15f;
            cirkit_img.DOFillAmount(1, d1)
                .OnComplete(() =>
                {
                    type_text.DOFade(1, d2);
                    name_text.DOFade(1, d2);
                    explain_text.DOFade(1, d2);
                    infoBoardImg.DOColor(data.GetColor(), d2);
                    equip_btn_img.DOColor(data.GetColor(), d2);
                });
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
    }

    public void OnExit()
    {
        for (int order = 0; order < num_in_page; order++) icon_imgs[order].raycastTarget = false;
        ResetInfoBoard();
        current_page_num = 0;
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
                if (!PlayerInfo.I.unlock[data.GetId()])
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
        equip_btn_img.DOColor(Color.grey, d);
        equip_btn_img.raycastTarget = false;
        cirkit_img.DOColor(Color.gray, d);
        type_text.text = "";
        name_text.text = "";
        explain_text.text = "";
        glow_img.color = Color.clear;
        current_icon_index = null;
        current_skill_id = null;
    }
}