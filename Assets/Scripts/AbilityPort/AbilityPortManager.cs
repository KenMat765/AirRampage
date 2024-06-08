using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class AbilityPortManager : Singleton<AbilityPortManager>
{
    protected override bool dont_destroy_on_load { get; set; } = false;

    int current_page = 0;
    int max_page { get { return Mathf.CeilToInt(AbilityDatabase.I.ability_count / (float)pallet_num); } }
    int current_pallet_index = -1;
    Ability current_ability = null;

    // Weight & Status
    int current_weight = 0;
    int hp = 100;
    int atk = 100;
    int def = 100;
    int spd = 100;

    // Glass & ReturnButton
    [SerializeField] Image glass_img;
    [SerializeField] RectTransform return_rect;
    Button return_btn;
    const float wait_time = 1.2f;
    const float enter_exit_duration = 0.1f;

    // Circuit
    Image circuit_img;
    const float circuit_duration = 0.2f;

    // InfoBoard
    [SerializeField] RectTransform info_rect;
    TextMeshProUGUI name_text, explain_text, equip_text, total_weight_text, hp_text, atk_text, def_text, spd_text, weight_text;
    Image total_weight_meter, hp_meter, atk_meter, def_meter, spd_meter;

    // ListBoard
    [SerializeField] RectTransform list_rect;
    struct PalletUI
    {
        public Image pallet, light;
        public TextMeshProUGUI name, weight;
        public Button button;
    }
    PalletUI[] palletUIs = new PalletUI[pallet_num];
    const int pallet_num = 6;
    [SerializeField] Color selected_color = new Color(0.04f, 1.0f, 0.0f);
    [SerializeField] Color blank_color = Color.gray;
    [SerializeField] Color locked_color = Color.gray;
    [SerializeField] Color unlocked_color = Color.white;
    [SerializeField] Color overweight_color = Color.red;


    void Start()
    {
        // Glass & ReturnButton
        glass_img.color = Color.clear;
        return_btn = return_rect.GetComponent<Button>();
        return_btn.interactable = false;
        return_rect.anchoredPosition = new Vector2(-100, -120);

        // Circuit
        circuit_img = list_rect.Find("Circuit").GetComponent<Image>();
        circuit_img.fillAmount = 0;
        circuit_img.color = Color.grey;

        // InfoBoard
        name_text = info_rect.Find("Name").GetComponent<TextMeshProUGUI>();
        explain_text = info_rect.Find("Explanation").GetComponent<TextMeshProUGUI>();
        equip_text = info_rect.Find("Equip").GetComponentInChildren<TextMeshProUGUI>();
        total_weight_meter = info_rect.Find("Weight_Meter").GetComponent<Image>();
        total_weight_text = info_rect.Find("Weight_Meter/Weight").GetComponent<TextMeshProUGUI>();
        hp_meter = info_rect.Find("Features/Feature1/Meter/MeterFill").GetComponent<Image>();
        atk_meter = info_rect.Find("Features/Feature2/Meter/MeterFill").GetComponent<Image>();
        def_meter = info_rect.Find("Features/Feature3/Meter/MeterFill").GetComponent<Image>();
        spd_meter = info_rect.Find("Features/Feature4/Meter/MeterFill").GetComponent<Image>();
        hp_text = info_rect.Find("Features/Feature1/Value").GetComponent<TextMeshProUGUI>();
        atk_text = info_rect.Find("Features/Feature2/Value").GetComponent<TextMeshProUGUI>();
        def_text = info_rect.Find("Features/Feature3/Value").GetComponent<TextMeshProUGUI>();
        spd_text = info_rect.Find("Features/Feature4/Value").GetComponent<TextMeshProUGUI>();
        weight_text = info_rect.Find("Weight").GetComponent<TextMeshProUGUI>();

        // Calculate weight & status
        for (int id = 0; id < AbilityDatabase.I.ability_count; id++)
        {
            if (!PlayerInfo.I.abi_unlock[id]) continue;
            if (!PlayerInfo.I.abi_equip[id]) continue;
            Ability ability = AbilityDatabase.I.GetAbilityById(id);
            UpdateStatusValue(ability, true);
        }
        UpdateStatusUI();

        // ListBoard
        for (int k = 0; k < pallet_num; k++)
        {
            Transform pallet_rect = list_rect.Find("Pallets").GetChild(k);
            palletUIs[k].pallet = pallet_rect.GetComponent<Image>();
            palletUIs[k].button = pallet_rect.GetComponent<Button>();
            palletUIs[k].light = pallet_rect.Find("Light").GetComponent<Image>();
            palletUIs[k].name = pallet_rect.Find("Name").GetComponent<TextMeshProUGUI>();
            palletUIs[k].weight = pallet_rect.Find("Weight").GetComponent<TextMeshProUGUI>();
        }

        // Enter UIs
        Utilities.DelayCall(this, wait_time, () =>
        {
            ResetInfoList();
            ShowListPage(current_page);
            EnterReturnGlass();
            EnterInfoList();
        });
    }


    public void Return()
    {
        ExitInfoList();
        ExitReturnGlass();
        SaveManager.SaveData<PlayerInfo>(PlayerInfo.I);
        Utilities.DelayCall(this, wait_time, () =>
        {
            SceneManager2.I.LoadSceneAsync2(GameScenes.MENU, FadeType.bottom);
        });
    }


    // ↓ UI移動系メソッド群 ↓ ////////////////////////////////////////////////////////////////////////////////////////////////
    void EnterReturnGlass()
    {
        glass_img.DOFade(1, enter_exit_duration);
        return_rect.DOAnchorPosX(120, enter_exit_duration);
    }

    void ExitReturnGlass()
    {
        glass_img.DOFade(0, enter_exit_duration);
        return_rect.LetOutRect(Direction.left, 100, enter_exit_duration);
    }

    void EnterInfoList()
    {
        info_rect.DOAnchorPosX(-470, enter_exit_duration);
        list_rect.DOAnchorPosX(432, enter_exit_duration)
            .OnComplete(() => circuit_img.DOFillAmount(1, circuit_duration)
                .OnComplete(() =>
                {
                    return_btn.interactable = true;
                }));
    }

    void ExitInfoList()
    {
        return_btn.interactable = false;
        circuit_img.DOFillAmount(0, circuit_duration)
            .OnComplete(() =>
            {
                info_rect.LetOutRect(Direction.left, 350, enter_exit_duration);
                list_rect.LetOutRect(Direction.right, 500, enter_exit_duration);
            });
    }


    // ↓ List & Info Board系メソッド群 ↓ ////////////////////////////////////////////////////////////////////////////////////////////////
    public void OnSelectPallet(int index)
    {
        // When currently selected pallet was selected.
        if (index == current_pallet_index)
        {
            DOTween.CompleteAll();
            ResetInfoList();
        }

        // When new pallet was selected.
        else
        {
            // Reset color of currently selected pallet if it exists.
            if (current_pallet_index != -1)
            {
                palletUIs[current_pallet_index].pallet.color = unlocked_color;
            }

            // Update current pallet index.
            current_pallet_index = index;

            // Get ability
            int ability_id = current_page * pallet_num + index;
            if (ability_id >= AbilityDatabase.I.ability_count)
            {
                return;
            }
            bool is_equiped = PlayerInfo.I.abi_equip[ability_id];
            current_ability = AbilityDatabase.I.GetAbilityById(ability_id);

            // Reset circuit & info board.
            circuit_img.fillAmount = 0;
            circuit_img.color = Color.white;
            name_text.DOFade(0, 0);
            name_text.text = current_ability.Name;
            explain_text.DOFade(0, 0);
            explain_text.text = current_ability.Explanation;
            equip_text.DOFade(0, 0);
            equip_text.text = is_equiped ? "Remove" : "Set";
            weight_text.DOFade(0, 0);
            weight_text.text = current_ability.Weight.ToString();

            // Animate circuit & info board.
            DOTween.CompleteAll();
            const float d1 = 0.15f;
            const float d2 = 0.15f;
            palletUIs[index].pallet.DOColor(selected_color, d1);
            circuit_img.DOFillAmount(1, d1)
            .OnComplete(() =>
            {
                name_text.DOFade(1, d2);
                explain_text.DOFade(1, d2);
                equip_text.DOFade(1, d2);
                weight_text.DOFade(1, d2);
            });
        }
    }

    public void OnSelectEquip()
    {
        if (current_ability == null)
        {
            return;
        }

        int ability_id = AbilityDatabase.I.GetIdFromAbility(current_ability);
        bool is_equiped = PlayerInfo.I.abi_equip[ability_id];
        const float d = 0.15f;

        // Unequip
        if (is_equiped)
        {
            DOTween.CompleteAll();
            equip_text.alpha = 0;
            equip_text.text = "Set";
            equip_text.DOFade(1, d);
            palletUIs[current_pallet_index].light.DOFade(0, d);
            PlayerInfo.I.abi_equip[ability_id] = false;
        }

        // Equip
        else
        {
            DOTween.CompleteAll();
            equip_text.alpha = 0;
            equip_text.text = "Remove";
            equip_text.DOFade(1, d);
            palletUIs[current_pallet_index].light.DOFade(1, d);
            PlayerInfo.I.abi_equip[ability_id] = true;
        }

        UpdateStatusValue(current_ability, !is_equiped);
        UpdateStatusUI();
        ShowListPage(current_page);
    }

    public void OnSelectNextArrow(int direction)
    {
        int next_page;
        // Go to next page.
        if (direction >= 0)
        {
            next_page = current_page + 1;
            next_page %= max_page;
        }
        // Go to previous page.
        else
        {
            next_page = current_page - 1;
            if (next_page < 0) next_page += max_page;
        }
        DOTween.CompleteAll();
        ResetInfoList();
        ShowListPage(next_page);
    }

    void ShowListPage(int page)
    {
        if (page != current_page)
        {
            current_page = page;
        }

        int weight_remain = GameInfo.max_weight - current_weight;

        for (int pallet_index = 0; pallet_index < pallet_num; pallet_index++)
        {
            // Ability does not exist
            int ability_id = page * pallet_num + pallet_index;
            if (ability_id >= AbilityDatabase.I.ability_count)
            {
                palletUIs[pallet_index].pallet.color = blank_color;
                palletUIs[pallet_index].button.interactable = false;
                palletUIs[pallet_index].light.DOFade(0, 0);
                palletUIs[pallet_index].name.text = "";
                palletUIs[pallet_index].weight.text = "";
                continue;
            }

            // Ability unlocked
            bool unlocked = PlayerInfo.I.abi_unlock[ability_id];
            if (unlocked)
            {
                Ability ability = AbilityDatabase.I.GetAbilityById(ability_id);

                // Ability equiped
                bool is_equiped = PlayerInfo.I.abi_equip[ability_id];
                if (is_equiped)
                {
                    palletUIs[pallet_index].pallet.color = unlocked_color;
                    palletUIs[pallet_index].button.interactable = true;
                    palletUIs[pallet_index].light.DOFade(1, 0);
                    palletUIs[pallet_index].name.text = ability.Name;
                    palletUIs[pallet_index].weight.text = ability.Weight.ToString();
                }

                // Ability unequiped
                else
                {
                    // Ability overweight
                    if (ability.Weight > weight_remain)
                    {
                        palletUIs[pallet_index].pallet.color = overweight_color;
                        palletUIs[pallet_index].button.interactable = false;
                        palletUIs[pallet_index].light.DOFade(0, 0);
                        palletUIs[pallet_index].name.text = ability.Name;
                        palletUIs[pallet_index].weight.text = ability.Weight.ToString();
                    }

                    // Ability underweight
                    else
                    {
                        palletUIs[pallet_index].pallet.color = unlocked_color;
                        palletUIs[pallet_index].button.interactable = true;
                        palletUIs[pallet_index].light.DOFade(0, 0);
                        palletUIs[pallet_index].name.text = ability.Name;
                        palletUIs[pallet_index].weight.text = ability.Weight.ToString();
                    }
                }
            }

            // Ability locked
            else
            {
                palletUIs[pallet_index].pallet.color = locked_color;
                palletUIs[pallet_index].button.interactable = false;
                palletUIs[pallet_index].light.DOFade(0, 0);
                palletUIs[pallet_index].name.text = "???";
                palletUIs[pallet_index].weight.text = "??";
            }
        }
    }

    void ResetInfoList()
    {
        const float d = 0.1f;

        // Deselect pallet when selected
        if (current_pallet_index != -1)
        {
            palletUIs[current_pallet_index].pallet.color = unlocked_color;
            current_pallet_index = -1;
            current_ability = null;
        }

        // Reset UIs
        circuit_img.DOColor(Color.gray, d);
        name_text.DOFade(0, d);
        explain_text.DOFade(0, d);
        equip_text.DOFade(0, d);
        weight_text.DOFade(0, d);
    }

    void UpdateStatusValue(Ability ability, bool equip)
    {
        int sign = equip ? 1 : -1;
        current_weight += ability.Weight * sign;
        switch (ability.Name)
        {
            case "HP Boost": hp += 20 * sign; break;
            case "HP Boost - II": hp += 30 * sign; break;
            case "HP Boost - III": hp += 50 * sign; break;
            case "Berserker": atk += 20 * sign; break;
            case "Berserker - II": atk += 30 * sign; break;
            case "Berserker - III": atk += 50 * sign; break;
            case "Guardian": def += 20 * sign; break;
            case "Guardian - II": def += 30 * sign; break;
            case "Guardian - III": def += 50 * sign; break;
            case "Lightning": spd += 20 * sign; break;
            case "Lightning - II": spd += 30 * sign; break;
            case "Lightning - III": spd += 50 * sign; break;
        }
    }

    void UpdateStatusUI()
    {
        const float d = 0.15f;
        total_weight_meter.DOFillAmount((float)current_weight / GameInfo.max_weight, d);
        hp_meter.DOFillAmount((hp - 90.0f) / 10 / 11, d);
        atk_meter.DOFillAmount((atk - 90.0f) / 10 / 11, d);
        def_meter.DOFillAmount((def - 90.0f) / 10 / 11, d);
        spd_meter.DOFillAmount((spd - 90.0f) / 10 / 11, d);
        total_weight_text.text = current_weight.ToString();
        hp_text.text = hp.ToString();
        atk_text.text = atk.ToString();
        def_text.text = def.ToString();
        spd_text.text = spd.ToString();
    }
}
