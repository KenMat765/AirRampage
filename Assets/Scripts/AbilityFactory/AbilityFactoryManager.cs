using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Linq;
using System;

public class AbilityFactoryManager : MonoBehaviour
{
    [SerializeField] ParticleSystem lightning;
    [SerializeField] Image glassImg;
    [SerializeField] RectTransform returnRect;
    [SerializeField] RectTransform generateRect;
    [SerializeField] RectTransform status, result;
    [SerializeField] Color textBlue, textRed;

    Button return_button;
    Button generateButton;

    // Status
    TextMeshProUGUI coinHaveText, coinNeedText;
    Image circleFill;
    TextMeshProUGUI unlockedNumText, allNumText;

    // Result
    TextMeshProUGUI resultTitleText;
    TextMeshProUGUI abilityNameText;
    TextMeshProUGUI weightText;
    TextMeshProUGUI explanationText;

    const int featureCount = 4;
    const float yScale = 1.05f;
    const float wait_time = 1.2f;
    const float enter_exit_duration = 0.1f;

    bool is_status = true;

    void Start()
    {
        return_button = returnRect.GetComponent<Button>();
        generateButton = generateRect.GetComponent<Button>();
        return_button.interactable = false;
        generateButton.interactable = false;

        // Status
        coinHaveText = status.Find("Coin/CoinHave").GetComponent<TextMeshProUGUI>();
        coinNeedText = status.Find("Coin/CoinNeed").GetComponent<TextMeshProUGUI>();
        circleFill = status.Find("CircleBack/CircleFill").GetComponent<Image>();
        unlockedNumText = status.Find("CircleBack/UnlockedNum").GetComponent<TextMeshProUGUI>();
        allNumText = status.Find("CircleBack/AllNum").GetComponent<TextMeshProUGUI>();

        // Result
        resultTitleText = result.Find("Title").GetComponent<TextMeshProUGUI>();
        abilityNameText = result.Find("Name").GetComponent<TextMeshProUGUI>();
        weightText = result.Find("Weight/WeightText").GetComponent<TextMeshProUGUI>();
        explanationText = result.Find("ExplanationFrame/Explanation").GetComponent<TextMeshProUGUI>();

        // Show status and hide result & glass
        status.gameObject.SetActive(true);
        status.DOScaleY(yScale, 0);
        result.gameObject.SetActive(true);
        result.DOScaleY(0, 0);
        glassImg.gameObject.SetActive(true);
        glassImg.DOFade(0, 0);

        // Show status UI
        bool can_generate = ShowStatus();
        generateButton.interactable = can_generate;

        // Enter UIs
        ExitUI(0);
        Utilities.DelayCall(this, wait_time, () =>
        {
            EnterUI(enter_exit_duration);
        });
    }

    void EnterUI(float duration)
    {
        generateRect.DOAnchorPosX(500, duration);
        returnRect.DOAnchorPosX(120, duration)
            .OnComplete(() =>
            {
                return_button.interactable = true;
            });
    }

    void ExitUI(float duration)
    {
        return_button.interactable = false;
        generateRect.LetOutRect(Direction.left, 500, duration);
        returnRect.LetOutRect(Direction.left, 100, duration);
    }

    public void Return()
    {
        ExitUI(enter_exit_duration);
        Utilities.DelayCall(this, wait_time, () =>
        {
            SceneManager2.I.LoadSceneAsync2(GameScenes.MENU, FadeType.bottom);
        });
    }

    public void OnPressedButton()
    {
        if (is_status)
        {
            UnlockAbility();
        }
        else
        {
            ReturnToStatus();
        }
    }

    ///<summary> Updates status UIs </summary>
    ///<returns> Whether skill can be generated or not </returns>
    bool ShowStatus()
    {
        int unlock_count = PlayerInfo.I.abi_unlock.Count(n => n);
        int all_count = AbilityDatabase.I.ability_count;
        unlockedNumText.text = unlock_count.ToString();
        allNumText.text = all_count.ToString();
        circleFill.fillAmount = unlock_count / (float)all_count;

        int coins_have = PlayerInfo.I.coins;
        int coins_need = GameInfo.a_generate_coin;
        coinHaveText.text = coins_have.ToString();
        coinNeedText.text = coins_need.ToString();

        if (unlock_count >= all_count)
        {
            unlockedNumText.color = textRed;
            coinHaveText.color = textBlue;
            return false;
        }
        if (coins_have < coins_need)
        {
            unlockedNumText.color = textBlue;
            coinHaveText.color = textRed;
            return false;
        }
        unlockedNumText.color = textBlue;
        coinHaveText.color = textBlue;
        return true;
    }

    ///<summary> Updates result UIs </summary>
    ///<param name="abilityId"> Skill ID generated </param>
    void ShowResult(int abilityId)
    {
        Ability abilityData = AbilityDatabase.I.GetAbilityById(abilityId);
        bool unlocked = PlayerInfo.I.abi_unlock[abilityId];
        if (unlocked) // Already unlocked
        {
            resultTitleText.text = "";
        }
        else // Newly unlocked
        {
            // resultTitleText.text = "new ability" + Environment.NewLine + "unlocked";
            resultTitleText.text = "unlocked";
        }
        abilityNameText.text = abilityData.Name;
        weightText.text = abilityData.Weight.ToString();
        explanationText.text = abilityData.Explanation;
    }

    void UnlockAbility()
    {
        return_button.interactable = false;

        // Unlock random skill
        int ability_count = AbilityDatabase.I.ability_count;
        int abilityId = UnityEngine.Random.Range(0, ability_count);

        // Update UI. (Do this before updating PlayerInfo)
        ShowResult(abilityId);
        generateButton.interactable = false;

        // Update PlayerInfo
        PlayerInfo.I.abi_unlock[abilityId] = true;
        PlayerInfo.I.coins -= GameInfo.a_generate_coin;
        SaveManager.SaveData<PlayerInfo>(PlayerInfo.I);

        // Animation
        Sequence seq = DOTween.Sequence();
        const float d1 = 0.1f;
        const float d2 = 0.6f;
        const float anim_duration = 3f;
        seq.Append(status.DOScaleY(0, d1));
        seq.Join(glassImg.DOFade(1, d2))
            .OnPlay(() => lightning.Play());
        seq.AppendInterval(anim_duration);
        seq.AppendCallback(() => lightning.Stop());
        seq.Append(result.DOScaleY(yScale, d1));
        seq.Join(glassImg.DOFade(0, d2));
        seq.OnComplete(() =>
        {
            is_status = false;
            generateButton.interactable = true;
        });
        seq.Play();
    }

    void ReturnToStatus()
    {
        bool can_generate = ShowStatus();
        generateButton.interactable = false;

        // Animation
        Sequence seq = DOTween.Sequence();
        const float d1 = 0.05f;
        seq.Append(result.DOScaleY(0, d1));
        seq.Append(status.DOScaleY(yScale, d1));
        seq.OnComplete(() =>
        {
            is_status = true;
            return_button.interactable = true;
            generateButton.interactable = can_generate;
        });
        seq.Play();
    }
}
