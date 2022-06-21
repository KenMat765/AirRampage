using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SkillDeck : Utilities
{
    [SerializeField] Image[] numberImgs = new Image[4];

    [SerializeField] Image[] icon_imgs = new Image[GameInfo.max_skill_count];
    Image[] skill_imgs = new Image[GameInfo.max_skill_count];

    Image[] lines = new Image[GameInfo.max_skill_count];
    Image[] line2s = new Image[GameInfo.max_skill_count];
    Text[] skill_names = new Text[GameInfo.max_skill_count];
    Text[] change_texts = new Text[GameInfo.max_skill_count];
    Image[] change_btn_imgs = new Image[GameInfo.max_skill_count];
    Text[] remove_texts = new Text[GameInfo.max_skill_count];
    Image[] remove_btn_imgs = new Image[GameInfo.max_skill_count];
    Text[] set_texts = new Text[GameInfo.max_skill_count];
    Image[] set_btn_imgs = new Image[GameInfo.max_skill_count];

    public int current_deck_num { get; private set; } = 0;
    int? current_icon_index = null;
    public int selected_icon_index { get; private set; } = 0;

    Sequence fadein_seq;



    void Start()
    {
        for (int k = 0; k < icon_imgs.Length; k++)
        {
            icon_imgs[k] = icon_imgs[k].GetComponent<Image>();
            skill_imgs[k] = icon_imgs[k].transform.Find("Skill_Img").GetComponent<Image>();

            lines[k] = icon_imgs[k].transform.Find("Line").GetComponent<Image>();
            line2s[k] = icon_imgs[k].transform.Find("Line2").GetComponent<Image>();

            skill_names[k] = lines[k].transform.Find("SkillName").GetComponent<Text>();

            change_btn_imgs[k] = lines[k].transform.Find("Change_Btn").GetComponent<Image>();
            change_texts[k] = change_btn_imgs[k].transform.Find("Change").GetComponent<Text>();

            remove_btn_imgs[k] = lines[k].transform.Find("Remove_Btn").GetComponent<Image>();
            remove_texts[k] = remove_btn_imgs[k].transform.Find("Remove").GetComponent<Text>();

            set_btn_imgs[k] = line2s[k].transform.Find("Set_Btn").GetComponent<Image>();
            set_texts[k] = set_btn_imgs[k].transform.Find("Set").GetComponent<Text>();
        }

        OnExit();
        RefreshIcons(0);
    }



    public void OnSelectIcon(int icon_index)
    {
        int?[] skillIds = new int?[GameInfo.max_skill_count];
        PlayerInfo.I.SkillIdsGetter(current_deck_num, out skillIds);
        if (current_icon_index != icon_index)
        {
            if (current_icon_index != null)
            {
                FadeOutLeader();
            }

            if (skillIds[icon_index] != null)
            {
                FadeInLeader(icon_index, LeaderType.Line);
            }
            else
            {
                FadeInLeader(icon_index, LeaderType.Line2);
            }

            current_icon_index = icon_index;
            selected_icon_index = icon_index;
        }

        else
        {
            FadeOutLeader();
            current_icon_index = null;
        }
    }

    public void GoToNextDeck(int direction)
    {
        float number_duration = 0.1f;

        int next_deck_num = ((current_deck_num + direction) % GameInfo.deck_count + GameInfo.deck_count) % GameInfo.deck_count;

        numberImgs[current_deck_num].fillAmount = 0;
        numberImgs[next_deck_num].DOFillAmount(1, number_duration);

        RefreshIcons(next_deck_num);

        FadeOutLeader();

        current_icon_index = null;
        current_deck_num = next_deck_num;
    }

    public void OnSelectRemove()
    {
        PlayerInfo.I.SkillIdSetter(current_deck_num, selected_icon_index, null);
        RefreshIcons(current_deck_num);
        FadeOutLeader();
        current_icon_index = null;
    }



    public void OnEnter()
    {
        for (int k = 0; k < icon_imgs.Length; k++)
        {
            icon_imgs[k].raycastTarget = true;
        }
    }

    public void OnExit()
    {
        FadeOutLeader();

        for (int k = 0; k < icon_imgs.Length; k++)
        {
            icon_imgs[k].raycastTarget = false;
            change_btn_imgs[k].raycastTarget = false;
            remove_btn_imgs[k].raycastTarget = false;
            set_btn_imgs[k].raycastTarget = false;
        }

        current_icon_index = null;
    }



    // ↓ Deck内UI処理系メソッド群 ↓ ////////////////////////////////////////////////////////////////////////////////////////////////////

    enum LeaderType { Line, Line2 }

    void FadeInLeader(int icon_index, LeaderType leader_type)
    {
        float line_duration = 0.15f;
        float text_duration = 0.15f;

        fadein_seq = DOTween.Sequence();

        switch (leader_type)
        {
            case LeaderType.Line:
                fadein_seq.Append(DOTween.To(() => lines[icon_index].fillAmount, (value) => lines[icon_index].fillAmount = value, 1, line_duration));
                fadein_seq.Append(skill_names[icon_index].DOFade(1, text_duration));
                fadein_seq.Join(change_btn_imgs[icon_index].DOFade(1, text_duration));
                fadein_seq.Join(change_texts[icon_index].DOFade(1, text_duration));
                fadein_seq.Join(remove_btn_imgs[icon_index].DOFade(1, text_duration));
                fadein_seq.Join(remove_texts[icon_index].DOFade(1, text_duration));
                fadein_seq.Play();

                change_btn_imgs[icon_index].raycastTarget = true;
                remove_btn_imgs[icon_index].raycastTarget = true;

                break;

            case LeaderType.Line2:
                fadein_seq.Append(DOTween.To(() => line2s[icon_index].fillAmount, (value) => line2s[icon_index].fillAmount = value, 1, line_duration));
                fadein_seq.Append(set_texts[icon_index].DOFade(1, text_duration));
                fadein_seq.Join(set_btn_imgs[icon_index].DOFade(1, text_duration));
                fadein_seq.Play();

                set_btn_imgs[icon_index].raycastTarget = true;

                break;
        }
    }

    void FadeOutLeader()
    {
        float fadeout_duration = 0.1f;

        if (current_icon_index != null)
        {
            int casted_icon_index = (int)current_icon_index;

            fadein_seq.Kill(false);

            var seq = DOTween.Sequence();

            if (lines[casted_icon_index].fillAmount > 0)
            {
                seq.Join(skill_names[casted_icon_index].DOFade(0, fadeout_duration));
                seq.Join(change_btn_imgs[casted_icon_index].DOFade(0, fadeout_duration));
                seq.Join(change_texts[casted_icon_index].DOFade(0, fadeout_duration));
                seq.Join(remove_btn_imgs[casted_icon_index].DOFade(0, fadeout_duration));
                seq.Join(remove_texts[casted_icon_index].DOFade(0, fadeout_duration));
                seq.Append(DOTween.To(() => lines[casted_icon_index].fillAmount, (value) => lines[casted_icon_index].fillAmount = value, 0, fadeout_duration));
            }
            if (line2s[casted_icon_index].fillAmount > 0)
            {
                seq.Join(set_texts[casted_icon_index].DOFade(0, fadeout_duration));
                seq.Join(set_btn_imgs[casted_icon_index].DOFade(0, fadeout_duration));
                seq.Append(DOTween.To(() => line2s[casted_icon_index].fillAmount, (value) => line2s[casted_icon_index].fillAmount = value, 0, fadeout_duration));
            }

            seq.Play();

            change_btn_imgs[casted_icon_index].raycastTarget = false;
            remove_btn_imgs[casted_icon_index].raycastTarget = false;
            set_btn_imgs[casted_icon_index].raycastTarget = false;
        }
    }

    public void RefreshIcons(int deck_num)
    {
        int?[] skillIds = new int?[GameInfo.max_skill_count];
        PlayerInfo.I.SkillIdsGetter(deck_num, out skillIds);
        for (int k = 0; k < icon_imgs.Length; k++)
        {
            if (skillIds[k] == null)
            {
                icon_imgs[k].color = Color.white;
                skill_imgs[k].sprite = null;
                skill_imgs[k].color = Color.clear;
            }
            else
            {
                SkillData data = SkillDatabase.I.SearchSkillById((int)skillIds[k]);

                // いらない予定 (data == null はあり得ない)
                if (data == null)
                {
                    icon_imgs[k].color = Color.white;
                    skill_imgs[k].sprite = null;
                    skill_imgs[k].color = Color.clear;
                }
                else
                {
                    icon_imgs[k].color = data.GetColor();
                    skill_imgs[k].sprite = data.GetSprite();
                    skill_imgs[k].color = new Color(1, 1, 1, 0.45f);
                    skill_names[k].text = data.GetNameJp();
                }
            }
        }
    }
}