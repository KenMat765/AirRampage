﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Netcode;
using Cysharp.Threading.Tasks;

public class uGUIMannager : Singleton<uGUIMannager>
{
    protected override bool dont_destroy_on_load { get; set; } = false;


    [SerializeField] RectTransform zone, status, controllStick, blastAndSkills, radar, comboRepo, timeAndScores, result, destroyRepo, killedRepo, call;

    [SerializeField] Image screen_color;

    FighterInfo playerInfo;

    #region Zone
    ZoneController playerZoneCtrl;
    Image zone_back;
    TextMeshProUGUI zone_text;
    public bool animating_zone { get; private set; } = false;
    Tween blink_zone_meter;
    #endregion

    #region Status
    GameObject HP_Player;
    Image[] speed_dots, power_dots, defence_dots;
    int speed_grade_cashe = 0, power_grade_cashe = 0, defence_grade_cashe = 0;
    Image cp_meter, zone_meter;
    #endregion

    #region Controll Stick
    RectTransform stick;
    public static bool onStick { get; private set; }
    public static Vector2 norm_diffPos { get; private set; }
    #endregion

    #region Normal Blast
    public static bool onBlast { get; private set; }
    public static Vector2 normBlastDiffPos { get; private set; }
    [SerializeField] float blast_diff_pos_max_mag = 200;
    #endregion

    #region Skills
    SkillController playerSkillCtrl;
    Image[] skill_fills = new Image[GameInfo.MAX_SKILL_COUNT];
    Button[] skill_btns = new Button[GameInfo.MAX_SKILL_COUNT];
    Image[] skill_imgs = new Image[GameInfo.MAX_SKILL_COUNT];
    #endregion

    #region Combo
    TextMeshProUGUI combo, cp_bonus;
    [SerializeField] TMP_ColorGradient gradv_green, gradv_yellow, gradv_red, gradv_blue, gradv_gray;
    bool combo_seqPlaying, combo_displayed;
    public float default_combo_disp_timer { get; set; }
    float combo_disp_timer;
    List<Sequence> combo_sequences = new List<Sequence>();
    Sequence combo_currSeq;
    #endregion

    #region Time and Scores
    TextMeshProUGUI timer;
    TextMeshProUGUI redScoreText, blueScoreText;
    float redScore, blueScore;
    [SerializeField] float SCORE_UPDATE_SPEED = 200;
    #endregion

    #region Result
    Button confirmButton;
    TextMeshProUGUI result_redScore, result_blueScore;
    TextMeshProUGUI[] fighter_names;
    TextMeshProUGUI[] fighter_scores;
    float[] scores_float;
    TextMeshProUGUI redTitle, blueTitle;
    TextMeshProUGUI occupation;
    #endregion

    #region Lock On
    [SerializeField] Image lockOn;
    AudioSource lockOnSound;
    GameObject lastTarget;
    #endregion

    #region Destroy Report
    TextMeshProUGUI destroyerTex, destroyedTex;
    Image arrow, skill_icon;
    bool destroy_seqPlaying;
    List<Sequence> destroy_sequences = new List<Sequence>();
    Sequence destroy_currSeq;
    #endregion

    #region Killed Report
    TextMeshProUGUI killed, reviveIn, reviveCount;
    #endregion

    #region Participant HPs
    RectTransform[] HP_rects;
    Image[] HPs, HPs_white;
    #endregion

    #region Call
    TextMeshProUGUI mission, rule, finish;
    #endregion

    #region PostProcess
    [SerializeField] Volume postprocess;
    Bloom bloom;
    Vignette viginette;
    #endregion

    public void UISetup()
    {
        playerInfo = ParticipantManager.I.fighterInfos[ParticipantManager.I.myFighterNo];

        #region PostProcess
        postprocess.profile.TryGet(out bloom);
        bloom.active = PlayerInfo.I.postprocess;
        postprocess.profile.TryGet(out viginette);
        viginette.intensity.value = 0;
        viginette.active = true;
        #endregion

        #region Zone
        playerZoneCtrl = playerInfo.fighter.GetComponent<ZoneController>();
        zone_back = zone.Find("Zone_Back").GetComponent<Image>();
        zone_text = zone.Find("Zone_Text").GetComponent<TextMeshProUGUI>();
        zone_back.FadeColor(0);
        zone_text.FadeColor(0);
        zone_text.rectTransform.DOAnchorPosX(-CanvasManager.canvas_width / 2 - zone_text.rectTransform.rect.width, 0);
        #endregion

        #region Status
        HP_Player = status.Find("HP_Player").gameObject;

        const int dot_count = 3;
        speed_dots = new Image[dot_count];
        power_dots = new Image[dot_count];
        defence_dots = new Image[dot_count];
        for (int k = 0; k < dot_count; k++)
        {
            speed_dots[k] = status.Find("Speed_Dots/Dot" + k).GetComponent<Image>();
            power_dots[k] = status.Find("Power_Dots/Dot" + k).GetComponent<Image>();
            defence_dots[k] = status.Find("Defence_Dots/Dot" + k).GetComponent<Image>();
            speed_dots[k].color = Color.clear;
            power_dots[k].color = Color.clear;
            defence_dots[k].color = Color.clear;
        }

        cp_meter = status.Find("CP_Meter").GetComponent<Image>();
        zone_meter = status.Find("Zone_Meter").GetComponent<Image>();
        cp_meter.fillAmount = playerZoneCtrl.Cp / playerZoneCtrl.CpToEnterZone;
        zone_meter.fillAmount = 0;
        #endregion

        #region Controll Stick
        stick = controllStick.Find("Stick").GetComponent<RectTransform>();
        #endregion

        #region Skills
        playerSkillCtrl = playerInfo.body.GetComponent<SkillController>();
        for (int k = 0; k < GameInfo.MAX_SKILL_COUNT; k++)
        {
            Transform skill_transform = blastAndSkills.Find("SkillButton" + k);
            skill_fills[k] = skill_transform.Find("Fill").GetComponent<Image>();
            skill_btns[k] = skill_transform.Find("Button").GetComponent<Button>();
            skill_imgs[k] = skill_transform.Find("Skill_Img").GetComponent<Image>();
        }
        SkillButtonSetup();
        #endregion

        #region Combo
        combo = comboRepo.Find("Combo").GetComponent<TextMeshProUGUI>();
        cp_bonus = comboRepo.Find("CP_Bonus").GetComponent<TextMeshProUGUI>();
        comboRepo.DOAnchorPosX(300, 0);
        combo_disp_timer = default_combo_disp_timer;
        #endregion

        #region Time and Scores
        timer = timeAndScores.Find("Timer").GetComponent<TextMeshProUGUI>();
        redScoreText = timeAndScores.Find("ScoreRed").GetComponent<TextMeshProUGUI>();
        blueScoreText = timeAndScores.Find("ScoreBlue").GetComponent<TextMeshProUGUI>();
        redScoreText.text = ScoreManager.I.GetScore(Team.RED).ToString();
        blueScoreText.text = ScoreManager.I.GetScore(Team.BLUE).ToString();
        #endregion

        #region Result
        confirmButton = result.Find("ConfirmButton").GetComponent<Button>();
        result_redScore = result.Find("RedScore").GetComponent<TextMeshProUGUI>();
        result_blueScore = result.Find("BlueScore").GetComponent<TextMeshProUGUI>();
        fighter_names = result.Find("Names").GetComponentsInChildren<TextMeshProUGUI>();
        for (int k = 0; k < GameInfo.MAX_PLAYER_COUNT; k++) fighter_names[k].text = BattleInfo.battleDatas[k].name;
        switch (BattleInfo.rule)
        {
            case Rule.BATTLE_ROYAL:
                Transform result_royal = result.Find("Result_Royal");
                fighter_scores = result_royal.Find("Scores").GetComponentsInChildren<TextMeshProUGUI>();
                foreach (TextMeshProUGUI score in fighter_scores) score.text = "";
                break;

                // case Rule.TERMINALCONQUEST:
                //     Transform result_terminal = result.Find("Result_Terminal");
                //     result_redScore = result_terminal.Find("RedScore").GetComponent<TextMeshProUGUI>();
                //     result_blueScore = result_terminal.Find("BlueScore").GetComponent<TextMeshProUGUI>();
                //     redTitle = result.Find("RedTitle").GetComponent<TextMeshProUGUI>();
                //     blueTitle = result.Find("BlueTitle").GetComponent<TextMeshProUGUI>();
                //     occupation = result_terminal.Find("Occupation").GetComponent<TextMeshProUGUI>();
                //     occupation.color = Color.clear;
                //     break;

                // case Rule.CRYSTALHUNTER:
                //     Transform result_crystal = result.Find("Result_Crystal");
                //     result_redScore = result_crystal.Find("RedScore").GetComponent<TextMeshProUGUI>();
                //     result_blueScore = result_crystal.Find("BlueScore").GetComponent<TextMeshProUGUI>();
                //     redTitle = result.Find("RedTitle").GetComponent<TextMeshProUGUI>();
                //     blueTitle = result.Find("BlueTitle").GetComponent<TextMeshProUGUI>();
                //     occupation = result_crystal.Find("Occupation").GetComponent<TextMeshProUGUI>();
                //     occupation.color = Color.clear;
                //     break;
        }
        result.DOScaleX(0, 0);
        confirmButton.interactable = false;
        result_redScore.text = "";
        result_blueScore.text = "";
        #endregion

        #region Lock On
        lockOnSound = lockOn.GetComponent<AudioSource>();
        #endregion

        #region Destroy Report
        destroyerTex = destroyRepo.Find("Destroyer").GetComponent<TextMeshProUGUI>();
        destroyedTex = destroyRepo.Find("Destroyed").GetComponent<TextMeshProUGUI>();
        arrow = destroyRepo.Find("Arrow").GetComponent<Image>();
        skill_icon = destroyRepo.Find("SkillIcon").GetComponent<Image>();
        destroyedTex.text = "";
        destroyedTex.text = "";
        #endregion

        #region Killed Report
        killed = killedRepo.Find("Killed").GetComponent<TextMeshProUGUI>();
        reviveIn = killedRepo.Find("Revive_In").GetComponent<TextMeshProUGUI>();
        reviveCount = killedRepo.Find("Revive_Count").GetComponent<TextMeshProUGUI>();
        #endregion

        #region Participant HPs
        List<GameObject> HP_objects = GameObject.FindGameObjectsWithTag("HP").ToList();
        HP_objects.Insert(ParticipantManager.I.myFighterNo, HP_Player);
        HP_rects = HP_objects.Select(h => h.GetComponent<RectTransform>()).ToArray();
        HPs = HP_objects.Select(h => h.transform.Find("HP").GetComponent<Image>()).ToArray();
        HPs_white = HP_objects.Select(h => h.transform.Find("HP_white").GetComponent<Image>()).ToArray();
        Image[] team_icons = HP_objects.Select(h => h.transform.Find("Team").GetComponent<Image>()).ToArray();
        TextMeshProUGUI[] name_icons = HP_objects.Select(h => h.transform.Find("Name").GetComponent<TextMeshProUGUI>()).ToArray();
        for (int no = 0; no < GameInfo.MAX_PLAYER_COUNT; no++)
        {
            string name = ParticipantManager.I.fighterInfos[no].fighterCondition.fighterName.Value.ToString();
            name_icons[no].text = name;
            Team team = ParticipantManager.I.fighterInfos[no].fighterCondition.fighterTeam.Value;
            switch (team)
            {
                case Team.RED: team_icons[no].color = Color.red; break;
                case Team.BLUE: team_icons[no].color = Color.blue; break;
                default: team_icons[no].color = Color.gray; break;
            }
        }
        #endregion

        #region Call
        mission = call.Find("Mission").GetComponent<TextMeshProUGUI>();
        rule = call.Find("Rule").GetComponent<TextMeshProUGUI>();
        finish = call.Find("Finish").GetComponent<TextMeshProUGUI>();
        mission.rectTransform.DOAnchorPosX(CanvasManager.canvas_width / 2 + mission.rectTransform.rect.width, 0);
        mission.DOColor(Color.clear, 0);
        rule.rectTransform.DOAnchorPosX(CanvasManager.canvas_width / 2 + rule.rectTransform.rect.width, 0);
        rule.DOColor(Color.clear, 0);
        finish.DOColor(Color.clear, 0);
        switch (BattleInfo.rule)
        {
            case Rule.BATTLE_ROYAL: rule.text = "Battle Royal"; break;
            case Rule.TERMINAL_CONQUEST: rule.text = "Termial Conquest"; break;
            case Rule.CRYSTAL_HUNTER: rule.text = "Crystal Hunter"; break;
        }
        #endregion

        CSManager.swipe_condition = (TouchExtension swipe) =>
        {
            if ((swipe.start_pos.Screen2Canvas(AnchorPosition.LeftDown) - controllStick.anchoredPosition).sqrMagnitude < Mathf.Pow(controllStick.rect.width / 2, 2)) return false;
            else return true;
        };
    }

    void Update()
    {
        StickMannager();
        BlastManager();
        LockOnManager();
        HpOnHead();
        GameTimeManager();
        ScoreUIManager();
        ScreenManager();
        ReportDestroy();
        SkillButtonUpdate();
        DotLighter();
        CPManager();
        ReportCombo();
    }


    public void EnterExitUI(bool enter, bool immediate)
    {
        Vector2 status_point = new Vector2(320, -120);
        Vector2 controllStick_point = new Vector2(330, 275);
        Vector2 blastAndSkills_point = new Vector2(-330, 275);
        Vector2 radar_point = new Vector2(-120, -120);
        Vector2 timeAndScores_point = new Vector2(0, -40);
        if (!enter)
        {
            status_point *= -1;
            controllStick_point *= -1;
            blastAndSkills_point *= -1;
            radar_point *= -1;
            timeAndScores_point *= -1;
        }

        float duration = immediate ? 0 : 0.5f;
        Ease ease = Ease.OutCubic;

        Sequence seq = DOTween.Sequence();
        seq.Append(status.DOAnchorPos(status_point, duration).SetEase(ease));
        seq.Join(controllStick.DOAnchorPos(controllStick_point, duration).SetEase(ease));
        seq.Join(blastAndSkills.DOAnchorPos(blastAndSkills_point, duration).SetEase(ease));
        seq.Join(radar.DOAnchorPos(radar_point, duration).SetEase(ease));
        seq.Join(timeAndScores.DOAnchorPos(timeAndScores_point, duration).SetEase(ease));
        seq.Play();
    }

    public void CleanUpScreen()
    {
        // Clear screen color.
        screen_color.color = Color.clear;

        // Hide KilledRepo.
        reviveCount.color = Color.clear;
        reviveCount.text = "";
        killed.color = Color.clear;
        reviveIn.color = Color.clear;

        // Hide Zone.
        zone_back.FadeColor(0);
        zone_text.FadeColor(0);
    }


    void GameTimeManager()
    {
        int currentTime_int = Mathf.CeilToInt(BattleConductor.I.timer.Value);
        int minute = currentTime_int / 60;
        int second = currentTime_int % 60;
        string time_string = second % 10 == second ? $"{minute}:0{second}" : $"{minute}:{second}";
        timer.text = time_string;
    }


    void ScoreUIManager()
    {
        float target_redScore = ScoreManager.I.GetScore(Team.RED);
        float target_blueScore = ScoreManager.I.GetScore(Team.BLUE);

        if (redScore < target_redScore)
        {
            redScore += SCORE_UPDATE_SPEED * Time.deltaTime;
            if (redScore > target_redScore)
            {
                redScore = target_redScore;
            }
            redScoreText.text = Mathf.CeilToInt(redScore).ToString();
        }
        else if (redScore > target_redScore)
        {
            redScore -= SCORE_UPDATE_SPEED * Time.deltaTime;
            if (redScore < target_redScore)
            {
                redScore = target_redScore;
            }
            redScoreText.text = Mathf.CeilToInt(redScore).ToString();
        }

        if (blueScore < target_blueScore)
        {
            blueScore += SCORE_UPDATE_SPEED * Time.deltaTime;
            if (blueScore > target_blueScore)
            {
                blueScore = target_blueScore;
            }
            blueScoreText.text = Mathf.CeilToInt(blueScore).ToString();
        }
        else if (blueScore > target_blueScore)
        {
            blueScore -= SCORE_UPDATE_SPEED * Time.deltaTime;
            if (blueScore < target_blueScore)
            {
                blueScore = target_blueScore;
            }
            blueScoreText.text = Mathf.CeilToInt(blueScore).ToString();
        }
    }


    void StickMannager()
    {
        TouchExtension[] onStick_touches;
        float detect_radius = controllStick.rect.width / 2;
        if (CSManager.currentTouches.Values.FindElement(s => (s.start_pos.Screen2Canvas(AnchorPosition.LeftDown) - controllStick.anchoredPosition).sqrMagnitude < Mathf.Pow(detect_radius, 2), out onStick_touches))
        {
            onStick = true;
        }
        else
        {
            onStick = false;
            stick.anchoredPosition = Vector2.zero;
            norm_diffPos = Vector2.zero;
        }

        if (onStick)
        {
            float diffPosMaxMag = controllStick.rect.width / 2;
            Vector2 diffPos;
            TouchExtension onStick_touch = onStick_touches[0];  // 最初に Left Stick に触れたもののみを検知
            Vector2 onStick_touch_canvas_pos = onStick_touch.current_pos.Screen2Canvas(AnchorPosition.LeftDown);

            if ((onStick_touch_canvas_pos - controllStick.anchoredPosition).sqrMagnitude < Mathf.Pow(diffPosMaxMag, 2)) diffPos = onStick_touch_canvas_pos - controllStick.anchoredPosition;
            else diffPos = (onStick_touch_canvas_pos - controllStick.anchoredPosition).normalized * diffPosMaxMag;
            stick.anchoredPosition = diffPos;
            norm_diffPos = diffPos / diffPosMaxMag;
        }
    }


    void BlastManager()
    {
        float detect_radius = blastAndSkills.rect.width / 2;
        IEnumerable<TouchExtension> touches = CSManager.currentTouches.Values.Where(s => (s.start_pos.Screen2Canvas(AnchorPosition.RightDown) - blastAndSkills.anchoredPosition).sqrMagnitude < Mathf.Pow(detect_radius, 2));
        if (touches.Count() > 0)
        {
            onBlast = true;

            TouchExtension blast_touch = touches.First();
            Vector2 blast_diff_pos = blast_touch.current_pos - blast_touch.start_pos;
            Vector2 clamped_blast_diff_pos = Vector2.ClampMagnitude(blast_diff_pos, blast_diff_pos_max_mag);
            normBlastDiffPos = clamped_blast_diff_pos / blast_diff_pos_max_mag;
        }
        else
        {
            onBlast = false;
        }
    }


    void LockOnManager()
    {
        if (playerInfo.attack.lockonCount == 0)
        {
            Color defaultColor = new Color(0.84f, 0.84f, 0.84f, 0.0f);
            Transform player_trans = playerInfo.fighter.transform;
            lockOn.rectTransform.anchoredPosition = RectTransformUtility.WorldToScreenPoint(CameraController.I.cam, player_trans.position + player_trans.forward * 40).Screen2Canvas();
            lockOn.color = defaultColor;
            lastTarget = null;
        }
        else
        {
            int currentTargetNo = playerInfo.attack.lockonTargetNos[0];
            GameObject currentTarget = ParticipantManager.I.fighterInfos[currentTargetNo].body;
            lockOn.rectTransform.anchoredPosition = RectTransformUtility.WorldToScreenPoint(CameraController.I.cam, currentTarget.transform.position).Screen2Canvas();
            lockOn.color = Color.red;
            if (currentTarget != lastTarget)
            {
                lockOnSound.Play();
                lastTarget = currentTarget;
            }
        }
    }


    void HpOnHead()
    {
        // Playerは除く
        for (int id = 0; id < GameInfo.MAX_PLAYER_COUNT; id++)
        {
            if (id != ParticipantManager.I.myFighterNo)
            {
                if (ParticipantManager.I.fighterInfos[id].visibility.isVisible)
                {
                    Vector2 canvasPos = RectTransformUtility.WorldToScreenPoint(CameraController.I.cam, ParticipantManager.I.fighterInfos[id].body.transform.position).Screen2Canvas();
                    HP_rects[id].anchoredPosition = canvasPos + new Vector2(0, 50);
                }
                else
                {
                    HP_rects[id].anchoredPosition = new Vector2(CanvasManager.canvas_width, CanvasManager.canvas_height);
                }
            }
        }
    }


    public void HPDecreaser_UI(int fighterNo, float norm_Hp)
    {
        HPs[fighterNo].fillAmount = norm_Hp;
        Tween whiteDecreaser = DOTween.To(() => HPs_white[fighterNo].fillAmount, x => HPs_white[fighterNo].fillAmount = x, HPs[fighterNo].fillAmount, 1f);
    }


    public void ResetHP_UI(int fighterNo)
    {
        HPs[fighterNo].fillAmount = 1;
        HPs_white[fighterNo].fillAmount = 1;
    }


    public void ScreenColorSetter(Color color) { screen_color.color = color; }

    void ScreenManager()
    {
        if (playerInfo.fighterCondition.isDead)
        {
            screen_color.color = new Color(1, 0, 0, 0.5f);
            reviveCount.text = Mathf.Ceil(playerInfo.fighterCondition.revivalTime - playerInfo.fighterCondition.reviveTimer).ToString();
            killed.color = Color.Lerp(killed.color, Color.white, 0.1f);
            if (playerInfo.fighterCondition.reviveTimer > playerInfo.fighterCondition.revivalTime - 5)
            {
                reviveCount.color = Color.white;
                reviveIn.color = Color.white;
            }
        }
        else
        {
            // Gradually reset screen color.
            screen_color.color = Color.Lerp(screen_color.color, Color.clear, 0.2f);
            reviveCount.color = Color.clear;
            reviveCount.text = "";
            killed.color = Color.clear;
            reviveIn.color = Color.clear;
        }
    }


    void ReportDestroy()
    {
        if (destroy_seqPlaying) return;

        if (destroy_sequences.Count > 0)
        {
            destroy_seqPlaying = true;
            destroy_currSeq = destroy_sequences[0];
            destroy_currSeq.Play();
        }
    }

    public void BookRepo(string destroyer, string destroyed, Team destroyed_team, string causeOfDeath)
    {
        Sequence newSeq = DOTween.Sequence()
            .OnStart(() =>
            {
                // Set text.
                destroyerTex.text = destroyer;
                destroyedTex.text = destroyed;

                // Set color.
                arrow.color = Color.white;
                switch (destroyed_team)
                {
                    case Team.RED:
                        destroyerTex.colorGradientPreset = gradv_blue;
                        destroyedTex.colorGradientPreset = gradv_red;
                        break;

                    case Team.BLUE:
                        destroyerTex.colorGradientPreset = gradv_red;
                        destroyedTex.colorGradientPreset = gradv_blue;
                        break;
                }

                // Specific cause of death.
                if (FighterCondition.IsSpecificDeath(causeOfDeath))
                {
                    destroyerTex.colorGradientPreset = gradv_gray;
                    skill_icon.enabled = false;
                }
                // Killed by normal blast.
                else if (causeOfDeath == FighterCondition.DEATH_NORMAL_BLAST)
                {
                    skill_icon.enabled = false;
                }
                // Killed by skills.
                else
                {
                    Sprite skill_sprite = SkillDatabase.I.SearchSkillByName(causeOfDeath).GetSprite();
                    skill_icon.sprite = skill_sprite;
                    skill_icon.color = Color.white;
                    skill_icon.enabled = true;
                }
            })
            .OnComplete(() =>
            {
                destroyerTex.text = "";
                destroyedTex.text = "";
                destroy_sequences.Remove(destroy_currSeq);
                destroy_seqPlaying = false;
            });

        newSeq.Append(destroyRepo.DOAnchorPosX(175, 0.2f));

        newSeq.Append(arrow.DOFade(0, 0.2f).SetLoops(5, LoopType.Yoyo));
        newSeq.Join(destroyerTex.DOFade(0, 0.2f).SetLoops(5, LoopType.Yoyo));
        newSeq.Join(destroyedTex.DOFade(0, 0.2f).SetLoops(5, LoopType.Yoyo));
        newSeq.Join(skill_icon.DOFade(0, 0.2f).SetLoops(5, LoopType.Yoyo));

        newSeq.Append(arrow.DOFade(1, 0.2f));
        newSeq.Join(destroyerTex.DOFade(1, 0.2f));
        newSeq.Join(destroyedTex.DOFade(1, 0.2f));
        newSeq.Join(skill_icon.DOFade(1, 0.2f));

        newSeq.Append(destroyRepo.DOAnchorPosX(-175, 0.2f).SetDelay(3));

        destroy_sequences.Add(newSeq);
    }


    void SkillButtonSetup()
    {
        for (int k = 0; k < GameInfo.MAX_SKILL_COUNT; k++)
        {
            int skill_no = k;

            skill_fills[k].fillAmount = 0;
            skill_fills[k].color = Color.red;
            skill_btns[k].interactable = false;

            int? skill_id = BattleInfo.battleDatas[ParticipantManager.I.myFighterNo].skillIds[k];
            if (skill_id == null)
            {
                skill_imgs[k].sprite = null;
                skill_imgs[k].color = Color.clear;
            }
            else
            {
                SkillData skill_data = SkillDatabase.I.SearchSkillById((int)skill_id);
                skill_imgs[k].sprite = skill_data.GetSprite();
                skill_imgs[k].color = new Color(1, 1, 1, 0.75f);
                skill_btns[k].onClick.AddListener(() =>
                {
                    skill_btns[skill_no].interactable = false;
                    playerSkillCtrl.SkillActivator(skill_no);
                });
            }
        }
    }


    void SkillButtonUpdate()
    {
        Color green = new Color(0.37f, 1, 0.3f, 1);

        // Playerが死んでいた場合 return
        if (playerInfo.fighterCondition.isDead)
        {
            for (int k = 0; k < GameInfo.MAX_SKILL_COUNT; k++)
            {
                if (skill_btns[k].interactable) { skill_btns[k].interactable = false; }
            }
            return;
        }

        for (int k = 0; k < GameInfo.MAX_SKILL_COUNT; k++)
        {
            Skill skill = playerSkillCtrl.skills[k];
            if (skill != null)
            {
                if (skill.isLocked)
                {
                    // Do not stop updating fillAmount even though the skill is locked.
                    skill_fills[k].fillAmount = skill.elapsed_time.Normalize(0, skill.charge_time) * 0.167f;
                    if (skill_btns[k].interactable) { skill_btns[k].interactable = false; }
                    if (!(skill_fills[k].color == Color.gray)) skill_fills[k].color = Color.gray;
                    continue;
                }

                // チャージ完了の時
                if (skill.isCharged)
                {
                    if (skill_fills[k].fillAmount != 0.167f) skill_fills[k].fillAmount = 0.167f;
                    if (!skill_btns[k].interactable) skill_btns[k].interactable = true;
                    if (!(skill_fills[k].color == green)) skill_fills[k].color = green;
                }

                // チャージ中の時
                else
                {
                    // fillAmountを更新
                    skill_fills[k].fillAmount = skill.elapsed_time.Normalize(0, skill.charge_time) * 0.167f;
                    if (!(skill_fills[k].color == Color.red)) skill_fills[k].color = Color.red;
                }
            }
        }
    }


    void DotLighter()
    {
        Color dot_blue = new Color(0.318f, 0.425f, 1, 1);
        Color dot_orange = new Color(1, 0.356f, 0.175f, 1);

        int speed_grade = playerInfo.fighterCondition.speed.grade;
        int power_grade = playerInfo.fighterCondition.power.grade;
        int defence_grade = playerInfo.fighterCondition.defence.grade;

        // gradeが変化した時だけDotsの色を変更
        if (speed_grade != speed_grade_cashe)
        {
            speed_grade_cashe = speed_grade;
            switch (speed_grade)
            {
                case -3: for (int k = 0; k < 3; k++) speed_dots[k].color = dot_blue; break;
                case -2: for (int k = 0; k < 2; k++) speed_dots[k].color = dot_blue; speed_dots[2].color = Color.clear; break;
                case -1: speed_dots[0].color = dot_blue; for (int k = 1; k < 3; k++) speed_dots[k].color = Color.clear; break;
                case 0: for (int k = 0; k < 3; k++) speed_dots[k].color = Color.clear; break;
                case 1: speed_dots[0].color = dot_orange; for (int k = 1; k < 3; k++) speed_dots[k].color = Color.clear; break;
                case 2: for (int k = 0; k < 2; k++) speed_dots[k].color = dot_orange; speed_dots[2].color = Color.clear; break;
                case 3: for (int k = 0; k < 3; k++) speed_dots[k].color = dot_orange; break;
            }
        }

        if (power_grade != power_grade_cashe)
        {
            power_grade_cashe = power_grade;
            switch (power_grade)
            {
                case -3: for (int k = 0; k < 3; k++) power_dots[k].color = dot_blue; break;
                case -2: for (int k = 0; k < 2; k++) power_dots[k].color = dot_blue; power_dots[2].color = Color.clear; break;
                case -1: power_dots[0].color = dot_blue; for (int k = 1; k < 3; k++) power_dots[k].color = Color.clear; break;
                case 0: for (int k = 0; k < 3; k++) power_dots[k].color = Color.clear; break;
                case 1: power_dots[0].color = dot_orange; for (int k = 1; k < 3; k++) power_dots[k].color = Color.clear; break;
                case 2: for (int k = 0; k < 2; k++) power_dots[k].color = dot_orange; power_dots[2].color = Color.clear; break;
                case 3: for (int k = 0; k < 3; k++) power_dots[k].color = dot_orange; break;
            }
        }

        if (defence_grade != defence_grade_cashe)
        {
            defence_grade_cashe = defence_grade;
            switch (defence_grade)
            {
                case -3: for (int k = 0; k < 3; k++) defence_dots[k].color = dot_blue; break;
                case -2: for (int k = 0; k < 2; k++) defence_dots[k].color = dot_blue; defence_dots[2].color = Color.clear; break;
                case -1: defence_dots[0].color = dot_blue; for (int k = 1; k < 3; k++) defence_dots[k].color = Color.clear; break;
                case 0: for (int k = 0; k < 3; k++) defence_dots[k].color = Color.clear; break;
                case 1: defence_dots[0].color = dot_orange; for (int k = 1; k < 3; k++) defence_dots[k].color = Color.clear; break;
                case 2: for (int k = 0; k < 2; k++) defence_dots[k].color = dot_orange; defence_dots[2].color = Color.clear; break;
                case 3: for (int k = 0; k < 3; k++) defence_dots[k].color = dot_orange; break;
            }
        }
    }


    public void ShowResult()
    {
        // Used for animating score texts.
        redScore = 0;
        blueScore = 0;
        scores_float = new float[GameInfo.MAX_PLAYER_COUNT + 2]; // Player count (= 8) + Zako (red & blue)

        // Animation settings.
        const float open_result_duration = 0.3f;
        const float interval = 0.5f;
        const float score_update_duration = 1.2f;
        Ease open_ease = Ease.OutQuart;

        // Create Animation.
        Sequence seq = DOTween.Sequence();
        seq.Append(result.DOScaleX(1, open_result_duration).SetEase(open_ease));
        seq.AppendInterval(interval);

        switch (BattleInfo.rule)
        {
            case Rule.BATTLE_ROYAL:
                for (int no = 0; no < GameInfo.MAX_PLAYER_COUNT + 2; no++)
                {
                    // === Scores for zakos === //
                    if (no >= GameInfo.MAX_PLAYER_COUNT)
                    {
                        // Just set scores to UI.
                        int zako_no = no;
                        seq.Join(DOTween.To(() => scores_float[zako_no], (value) => scores_float[zako_no] = value, ScoreManager.I.GetIndividualScore(zako_no), score_update_duration)
                            .OnUpdate(() => fighter_scores[zako_no].text = Mathf.CeilToInt(scores_float[zako_no]).ToString()));
                        continue;
                    }

                    // === Scores for fighters === //

                    // Check if battle data exists.
                    BattleInfo.ParticipantBattleData? nullable_data = BattleInfo.GetBattleDataByFighterNo(no);
                    if (!nullable_data.HasValue)
                    {
                        Debug.LogError($"Could not get battle data of fighterNo: {no}");
                        continue;
                    }

                    // Get battle data of fighter.
                    BattleInfo.ParticipantBattleData battle_data = nullable_data.Value;
                    int member_no = battle_data.memberNo;
                    string fighter_name = battle_data.name;
                    Team team = battle_data.team;

                    // Set fighter names to UI. (fighter_names: {red0, ... ,red3, blue0, ... ,blue3})
                    int ui_idx = team == Team.RED ? member_no : GameInfo.TEAM_MEMBER_COUNT + member_no;
                    fighter_names[ui_idx].text = fighter_name;

                    // Set individual scores to UI.
                    int fighter_no = no;
                    seq.Join(DOTween.To(() => scores_float[fighter_no], (value) => scores_float[fighter_no] = value, ScoreManager.I.GetIndividualScore(fighter_no), score_update_duration)
                        .OnUpdate(() => fighter_scores[ui_idx].text = Mathf.CeilToInt(scores_float[fighter_no]).ToString()));
                }
                break;

                // case Rule.TERMINALCONQUEST:
                //     break;

                // case Rule.CRYSTALHUNTER:
                //     break;
        }

        seq.AppendInterval(interval);
        seq.Append(DOTween.To(() => redScore, (value) => redScore = value, ScoreManager.I.GetScore(Team.RED), score_update_duration)
            .OnUpdate(() => result_redScore.text = Mathf.CeilToInt(redScore).ToString()));
        seq.Join(DOTween.To(() => blueScore, (value) => blueScore = value, ScoreManager.I.GetScore(Team.BLUE), score_update_duration)
            .OnUpdate(() => result_blueScore.text = Mathf.CeilToInt(blueScore).ToString()));
        seq.AppendInterval(interval);
        seq.AppendCallback(() => confirmButton.interactable = true);
        seq.Play();
    }

    public void ShowResultOccupation(Team occupation_team)
    {
        occupation.color = Color.white;
        switch (occupation_team)
        {
            case Team.RED:
                redTitle.rectTransform.DOAnchorPosX(0, 0);
                blueTitle.color = Color.clear;
                break;

            case Team.BLUE:
                blueTitle.rectTransform.DOAnchorPosX(0, 0);
                redTitle.color = Color.clear;
                break;

            default:
                Debug.LogError("占拠チームがNONEになっています!!");
                return;
        }

        float open_result_duration = 0.3f;
        float interval = 0.5f;
        Ease open_ease = Ease.OutQuart;
        Sequence seq = DOTween.Sequence();
        seq.Append(result.DOScaleX(1, open_result_duration).SetEase(open_ease));
        seq.AppendInterval(interval);
        seq.AppendCallback(() => confirmButton.interactable = true);
        seq.Play();
    }


    public float CallStart()
    {
        float mission_x = -150;
        float rule_x = 0;

        float move_duration = 0.2f;
        float stay_duration = 1.5f;
        float interval = 0.05f;

        Ease enter_color_ease = Ease.OutExpo;
        Ease exit_color_ease = Ease.InExpo;

        Sequence seq = DOTween.Sequence();
        seq.Append(mission.rectTransform.DOAnchorPosX(mission_x, move_duration));
        seq.Join(mission.DOColor(Color.white, move_duration).SetEase(enter_color_ease));
        seq.Join(rule.rectTransform.DOAnchorPosX(rule_x, move_duration).SetDelay(interval));
        seq.Join(rule.DOColor(Color.white, move_duration).SetEase(enter_color_ease).SetDelay(interval));
        seq.AppendInterval(stay_duration);
        seq.Append(mission.rectTransform.DOAnchorPosX(-CanvasManager.canvas_width / 2 - mission.rectTransform.rect.width, move_duration));
        seq.Join(mission.DOColor(Color.clear, move_duration).SetEase(exit_color_ease));
        seq.Join(rule.rectTransform.DOAnchorPosX(-CanvasManager.canvas_width / 2 - rule.rectTransform.rect.width, move_duration).SetDelay(interval));
        seq.Join(rule.DOColor(Color.clear, move_duration).SetEase(exit_color_ease).SetDelay(interval));
        seq.Play();

        return interval + move_duration + stay_duration + interval + move_duration;
    }


    public float CallFinish()
    {
        float enter_duration = 0.8f;
        float stay_duration = 1.5f;

        Ease ease = Ease.OutElastic;

        Sequence seq = DOTween.Sequence();
        seq.Append(finish.DOColor(Color.white, enter_duration).SetEase(ease));
        seq.AppendInterval(stay_duration)
            .OnComplete(() => finish.color = Color.clear);
        seq.Play();

        return enter_duration + stay_duration;
    }


    public async void OnPressedConfirm()
    {
        // Disconnect from server.
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.Shutdown();
            await UniTask.WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
        }

        // Go back to menu scene.
        SceneManager2.I.LoadSceneAsync2(GameScenes.MENU, FadeType.gradually);
    }


    public void StartZoneAnim()
    {
        animating_zone = true;

        // Initialize zone_back & zone_text & viginette
        zone_meter.DOColor(Color.red, 0);
        zone_back.FadeColor(0);
        zone_text.FadeColor(0);
        zone_text.rectTransform.DOAnchorPosX(-CanvasManager.canvas_width / 2 - zone_text.rectTransform.rect.width, 0);
        viginette.intensity.value = 0;

        // === Animation === //
        Sequence zone_anim = DOTween.Sequence();

        zone_anim.Append(
            zone_meter.DOFillAmount(1, 0.3f)
                .SetEase(Ease.OutCubic)
                .OnComplete(() => blink_zone_meter = zone_meter.DOColor(Color.white, 0.2f).SetLoops(-1, LoopType.Yoyo)));

        zone_anim.Append(zone_back.DOFade(1, 0.25f).SetEase(Ease.OutExpo));
        zone_anim.Join(DOTween.To(() => viginette.intensity.value, (x) => viginette.intensity.value = x, 0.3f, 0.25f).SetEase(Ease.OutExpo));

        zone_anim.Append(zone_text.rectTransform.DOAnchorPosX(-50, 0.25f).SetEase(Ease.OutExpo));
        zone_anim.Join(zone_text.DOFade(1, 0.25f).SetEase(Ease.OutExpo));

        zone_anim.Append(zone_text.rectTransform.DOAnchorPosX(50, 0.5f));

        zone_anim.Append(zone_text.rectTransform.DOAnchorPosX(CanvasManager.canvas_width / 2 + zone_text.rectTransform.rect.width, 0.25f).SetEase(Ease.InExpo));
        zone_anim.Join(zone_text.DOFade(0, 0.25f).SetEase(Ease.InExpo));

        zone_anim.Append(zone_back.DOFade(0, 0.25f).SetEase(Ease.InExpo));

        zone_anim.OnComplete(() =>
        {
            animating_zone = false;
        });
        // === Animation === //

        zone_anim.Play();
    }

    public void EndZoneAnim()
    {
        zone_back.FadeColor(0);
        zone_text.FadeColor(0);
        zone_text.rectTransform.DOAnchorPosX(-CanvasManager.canvas_width / 2 - zone_text.rectTransform.rect.width, 0);
        DOTween.To(() => viginette.intensity.value, (x) => viginette.intensity.value = x, 0, 0.3f);
        blink_zone_meter.Kill();
    }

    void CPManager()
    {
        // Do not edit fillAmount of cp & zone meters while animating zone.
        if (animating_zone) return;

        float cp = playerZoneCtrl.Cp;
        float cp_to_enter_zone = playerZoneCtrl.CpToEnterZone;
        cp_meter.fillAmount = cp / cp_to_enter_zone;
        if (playerZoneCtrl.isZone)
        {
            zone_meter.fillAmount = cp / cp_to_enter_zone;
        }
        else
        {
            zone_meter.fillAmount = 0;
        }
    }


    void ReportCombo()
    {
        if (combo_seqPlaying) return;

        if (combo_sequences.Count > 0)
        {
            combo_seqPlaying = true;
            combo_disp_timer = default_combo_disp_timer;
            combo_currSeq = combo_sequences[0];
            combo_currSeq.Play();
        }

        else
        {
            if (combo_displayed)
            {
                combo_disp_timer -= Time.deltaTime;
                if (combo_disp_timer < 0)
                {
                    combo_displayed = false;
                    combo_disp_timer = default_combo_disp_timer;
                    comboRepo.DOAnchorPosX(300, 0.2f).SetEase(Ease.OutExpo);
                }
            }
        }
    }

    public void BookCombo(int combo_count, float cp_magnif)
    {
        // Do not report combo when combo_count is less than 3.
        if (combo_count < 3) return;

        Sequence newSeq = DOTween.Sequence();
        newSeq.OnStart(() =>
        {
            combo_displayed = true;
            combo.text = "COMBO x" + combo_count;

            // Do not show CP magnification if it's 0 (ex: when zone)
            if (cp_magnif == 0)
            {
                cp_bonus.text = "";
            }
            else
            {
                cp_bonus.text = "CP BONUS x" + cp_magnif.ToString("f1");
            }

            // Change color by combo count.
            if (3 <= combo_count && combo_count <= 6)
            {
                combo.colorGradientPreset = gradv_green;
                cp_bonus.colorGradientPreset = gradv_green;
            }
            else if (7 <= combo_count && combo_count <= 10)
            {
                combo.colorGradientPreset = gradv_yellow;
                cp_bonus.colorGradientPreset = gradv_yellow;
            }
            else if (11 <= combo_count)
            {
                combo.colorGradientPreset = gradv_red;
                cp_bonus.colorGradientPreset = gradv_red;
            }
        });
        newSeq.Append(comboRepo.DOAnchorPosX(300, 0));
        newSeq.Append(comboRepo.DOAnchorPosX(-175, 0.2f).SetEase(Ease.OutExpo));
        newSeq.OnComplete(() =>
        {
            combo_sequences.Remove(combo_currSeq);
            combo_seqPlaying = false;
        });
        combo_sequences.Add(newSeq);
    }
}