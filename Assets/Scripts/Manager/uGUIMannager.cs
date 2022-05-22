using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class uGUIMannager : Singleton<uGUIMannager>
{
    protected override bool dont_destroy_on_load { get; set; } = false;

    FighterInfo playerInfo;
    Camera player_camera;
    public void SetPlayerCamera(Camera playerCam) => player_camera = playerCam;

    [SerializeField] GameObject dynamic_canvas;

    [SerializeField] Image screen_color;

    static Vector2 firstStickPos;
    public static bool onStick {get; private set;}
    public static Vector2 norm_diffPos {get; private set;}

    [SerializeField] Image leftStick, leftStickBack;

    [SerializeField] Image blastButton;
    public static bool onBlast {get; private set;}

    [SerializeField] Image lockOn;
    [SerializeField] AudioSource lockOnSound;

    float gameTime = 180;
    [SerializeField] Text gameTimeTex;

    [SerializeField] GameObject destroyRepo;
    [SerializeField] Sprite defaultSkillIcon;
    Text destroyerTex, destroyedTex;
    Image arrow, skill_icon;
    
    [SerializeField] GameObject killedRepo;
    Text killed, reviveIn, reviveCount;

    public static GameObject lastTarget;

    RectTransform[] HP_rects;
    Image[] HPs, HPs_white;
    [SerializeField] GameObject HP_Player;

    static bool seqPlaying;
    static List<Sequence> sequences;
    static Sequence currSeq;

    [SerializeField] GameObject skillButtonsObj;
    Image[] skill_fills = new Image[GameInfo.max_skill_count];
    Button[] skill_btns = new Button[GameInfo.max_skill_count];
    Image[] skill_imgs = new Image[GameInfo.max_skill_count];

    [SerializeField] GameObject speedDots, powerDots, defenceDots;
    Image[] speed_dots, power_dots, defence_dots;
    int speed_grade_cash = 0, power_grade_cash = 0, defence_grade_cash = 0;



    void OnEnable()
    {
        playerInfo = ParticipantManager.I.fighterInfos[ParticipantManager.I.myFighterNo];

        firstStickPos = leftStick.rectTransform.anchoredPosition;
        sequences = new List<Sequence>();

        destroyerTex = destroyRepo.transform.Find("Destroyer").GetComponent<Text>();
        arrow = destroyRepo.transform.Find("Arrow").GetComponent<Image>();
        destroyedTex = destroyRepo.transform.Find("Destroyed").GetComponent<Text>();
        destroyedTex.text = "";
        destroyedTex.text = "";
        skill_icon = destroyRepo.transform.Find("SkillIcon").GetComponent<Image>();

        killed = killedRepo.transform.Find("Killed").GetComponent<Text>();
        reviveIn = killedRepo.transform.Find("Revive_In").GetComponent<Text>();
        reviveCount = killedRepo.transform.Find("Revive_Count").GetComponent<Text>();

        List<GameObject> HP_objects = GameObject.FindGameObjectsWithTag("HP").ToList();
        HP_objects.Insert(ParticipantManager.I.myFighterNo, HP_Player);
        HP_rects = HP_objects.Select(h => h.GetComponent<RectTransform>()).ToArray();
        HPs = HP_objects.Select(h => h.transform.Find("HP").GetComponent<Image>()).ToArray();
        HPs_white = HP_objects.Select(h => h.transform.Find("HP_white").GetComponent<Image>()).ToArray();
        Image[] team_icons = HP_objects.Select(h => h.transform.Find("Team").GetComponent<Image>()).ToArray();
        for(int id = 0; id < GameInfo.max_player_count; id++)
        {
            Team team = ParticipantManager.I.fighterInfos[id].fighterCondition.fighterTeam.Value;
            if(team == Team.Red) team_icons[id].color = Color.red;
            else if(team == Team.Blue) team_icons[id].color = Color.blue;
            else Debug.LogError("fighterinfoにTeam情報が入っていません");
        }

        for(int k = 0; k < GameInfo.max_skill_count; k++)
        {
            Transform skill_btn_trans = skillButtonsObj.transform.Find("SkillButton" + k);
            skill_fills[k] = skill_btn_trans.transform.Find("Fill").GetComponent<Image>();
            skill_btns[k] = skill_btn_trans.transform.Find("Button").GetComponent<Button>();
            skill_imgs[k] = skill_btn_trans.transform.Find("Skill_Img").GetComponent<Image>();
        }
        ButtonSetup();

        const int dot_count = 3;
        speed_dots = new Image[dot_count];
        power_dots = new Image[dot_count];
        defence_dots = new Image[dot_count];
        for(int k = 0; k < dot_count; k++)
        {
            speed_dots[k] = speedDots.transform.Find("Dot" + k).GetComponent<Image>();
            power_dots[k] = powerDots.transform.Find("Dot" + k).GetComponent<Image>();
            defence_dots[k] = defenceDots.transform.Find("Dot" + k).GetComponent<Image>();

            speed_dots[k].color = Color.clear;
            power_dots[k].color = Color.clear;
            defence_dots[k].color = Color.clear;
        }

        CSManager.swipe_condition = (TouchExtension swipe) => 
        {
            if((swipe.start_pos - firstStickPos).sqrMagnitude < Mathf.Pow(leftStickBack.rectTransform.rect.width/2, 2)) { return false; }
            else { return true; }
        };
    }

    void Update()
    {
        LeftStickMannager();
        BlastManager();
        LockOnManager();
        HpOnHead();
        GameTimeManager();
        ScreenColorManager();
        ReportKill();
        ButtonUpdate();
        DotLighter();
    }



    void GameTimeManager()
    {
        gameTime -= Time.deltaTime;
        gameTimeTex.text = Mathf.CeilToInt(gameTime).ToString();
    }



    void LeftStickMannager()
    {
        TouchExtension[] onStick_touches;
        float detect_radius = leftStickBack.rectTransform.rect.width/2;
        if(CSManager.currentTouches.FindElement(s => (s.start_pos - firstStickPos).sqrMagnitude < Mathf.Pow(detect_radius, 2), out onStick_touches))
        {
            onStick = true;
        }
        else
        {
            onStick = false;
            leftStick.rectTransform.anchoredPosition = firstStickPos;
            norm_diffPos = Vector2.zero;
        }

        if(onStick)
        {
            float diffPosMaxMag = leftStickBack.rectTransform.rect.width/2;
            Vector2 diffPos;
            TouchExtension onStick_touch = onStick_touches[0];  // 最初に Left Stick に触れたもののみを検知

            if((onStick_touch.current_pos - firstStickPos).sqrMagnitude < Mathf.Pow(diffPosMaxMag, 2)) { diffPos = onStick_touch.current_pos - firstStickPos; }
            else { diffPos = (onStick_touch.current_pos - firstStickPos).normalized*diffPosMaxMag; }
            leftStick.rectTransform.anchoredPosition = firstStickPos + diffPos;
            norm_diffPos = diffPos/diffPosMaxMag;
        }
    }



    void BlastManager()
    {
        float detect_radius = blastButton.rectTransform.rect.width/2;
        if(CSManager.currentTouches.Any(s => (s.start_pos.Screen2Canvas() - blastButton.rectTransform.position.XY().UnScaleCanvasPos()).sqrMagnitude < Mathf.Pow(detect_radius, 2)))
        {
            onBlast = true;
        }
        else { onBlast = false; }
    }

    void LockOnManager()
    {
        if(playerInfo.attack.homingCount == 0)
        {
            Color defaultColor = new Color(0.84f, 0.84f, 0.84f);
            Transform player_trans = playerInfo.fighter.transform;
            lockOn.rectTransform.position = RectTransformUtility.WorldToScreenPoint(player_camera, player_trans.position + player_trans.forward*40).Screen2Canvas().ScaleCanvasPos();
            lockOn.color = defaultColor;
            lastTarget = null;
        }
        else
        {
            int currentTargetNo = playerInfo.attack.homingTargetNos[0];
            GameObject currentTarget = ParticipantManager.I.fighterInfos[currentTargetNo].body;
            lockOn.rectTransform.position = RectTransformUtility.WorldToScreenPoint(player_camera, currentTarget.transform.position).Screen2Canvas().ScaleCanvasPos();
            lockOn.color = Color.red;
            if(currentTarget != lastTarget)
            {
                lockOnSound.Play();
                lastTarget = currentTarget;
            }
        }
    }



    void HpOnHead()
    {
        // Playerは除く
        for(int id = 0; id < GameInfo.max_player_count; id++)
        {
            if(id != ParticipantManager.I.myFighterNo)
            {
                if(ParticipantManager.I.fighterInfos[id].bodyManager.visible == true)
                {
                    Vector2 canvasPos = RectTransformUtility.WorldToScreenPoint(player_camera, ParticipantManager.I.fighterInfos[id].body.transform.position).Screen2Canvas().ScaleCanvasPos();
                    HP_rects[id].position = canvasPos + new Vector2(0, 50).ScaleCanvasPos();
                }
                else
                {
                    HP_rects[id].position = new Vector2(CanvasManager.canvas_width, CanvasManager.canvas_height).ScaleCanvasPos();   //とにかく画面外に出す(-50に特に意味はない)
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

    void ScreenColorManager()
    {
        if(!playerInfo.fighterCondition.isDead)
        {
            screen_color.color = Color.Lerp(screen_color.color, Color.clear, 0.2f);
            reviveCount.color = Color.clear;
            reviveCount.text = "";
            killed.color = Color.clear;
            reviveIn.color = Color.clear;
        }
        else
        {
            screen_color.color = new Color(1,0,0,0.5f);
            reviveCount.text = Mathf.Ceil(playerInfo.fighterCondition.revivalTime - playerInfo.fighterCondition.revive_timer).ToString();
            killed.color = Color.Lerp(killed.color, Color.white, 0.1f);
            if(playerInfo.fighterCondition.revive_timer > playerInfo.fighterCondition.revivalTime - 5)
            {
                reviveCount.color = Color.white;
                reviveIn.color = Color.white;
            }
        }
    }



    void ReportKill()
    {
        foreach(Sequence sequence in sequences)
        {
            if(sequence != null && !seqPlaying)
            {
                sequence.Play();
                currSeq = sequence;
                seqPlaying = true;
            }
        }
    }

    public void BookRepo(string destroyer, string destroyed, Color arrowColor, Sprite skill_sprite)
    {
        Sprite icon;
        if(skill_sprite == null) icon = defaultSkillIcon;
        else icon = skill_sprite;

        Sequence newSeq = DOTween.Sequence()
            .OnStart(() => {destroyerTex.text = destroyer; destroyedTex.text = destroyed; arrow.color = arrowColor; skill_icon.sprite = icon;})
            .Append(destroyRepo.transform.DOLocalMoveX(-720, 0.2f))
            .Append(arrow.DOColor(Color.clear, 0.2f).SetLoops(5, LoopType.Yoyo))
                .Join(destroyerTex.DOColor(Color.clear, 0.2f).SetLoops(5, LoopType.Yoyo))
                .Join(destroyedTex.DOColor(Color.clear, 0.2f).SetLoops(5, LoopType.Yoyo))
                .Join(skill_icon.DOColor(Color.clear, 0.2f).SetLoops(5, LoopType.Yoyo))
            .Append(arrow.DOColor(arrowColor, 0.2f))
                .Join(destroyerTex.DOColor(Color.white, 0.2f))
                .Join(destroyedTex.DOColor(Color.white, 0.2f))
                .Join(skill_icon.DOColor(Color.white, 0.2f))
            .Append(destroyRepo.transform.DOLocalMoveX(-1100, 0.2f).SetDelay(3))
            .OnComplete(() => 
            {
                destroyerTex.text = "";
                destroyedTex.text = "";
                sequences.Remove(currSeq);
                seqPlaying = false;
            }); 
        sequences.Add(newSeq);
    }



    void ButtonSetup()
    {
        for(int k = 0; k < GameInfo.max_skill_count; k++)
        {
            int m = k;

            skill_fills[k].fillAmount = 0;
            skill_fills[k].color = Color.red;
            skill_btns[k].interactable = false;

            int? skill_id = BattleInfo.battleDatas[ParticipantManager.I.myFighterNo].skillIds[k];
            if(skill_id == null)
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
                    skill_btns[m].interactable = false;
                    playerInfo.attack.skills[m].Activator(null);
                });
            }
        }
    }

    void ButtonUpdate()
    {
        Color green = new Color(0.37f, 1, 0.3f, 1);

        // Playerが死んでいた場合 return
        if(playerInfo.fighterCondition.isDead)
        {
            for(int k = 0; k < GameInfo.max_skill_count; k++)
            {
                if(skill_btns[k].interactable) { skill_btns[k].interactable = false; }
            }
            return;
        }

        for(int k = 0; k < GameInfo.max_skill_count; k++)
        {
            Skill skill_instance = playerInfo.attack.skills[k];
            if(skill_instance != null)
            {
                // チャージ完了の時
                if(skill_instance.isCharged)
                {
                    if(!skill_btns[k].interactable) skill_btns[k].interactable = true;
                    if(!(skill_fills[k].color == green)) skill_fills[k].color = green;
                }

                // チャージ中の時
                else
                {
                    // fillAmountを更新
                    skill_fills[k].fillAmount = skill_instance.elapsed_time.Normalize(0, skill_instance.charge_time) * 0.167f;
                    if(!(skill_fills[k].color == Color.red)) skill_fills[k].color = Color.red;
                }
            }
        }
    }



    void DotLighter()
    {
        Color dot_blue = new Color(0.318f, 0.425f, 1, 1);
        Color dot_orange = new Color(1, 0.356f, 0.175f, 1);

        int speed_grade = playerInfo.fighterCondition.speed_grade;
        int power_grade = playerInfo.fighterCondition.power_grade;
        int defence_grade = playerInfo.fighterCondition.defence_grade;

        // gradeが変化した時だけDotsの色を変更
        if(speed_grade != speed_grade_cash)
        {
            speed_grade_cash = speed_grade;
            switch(speed_grade)
            {
                case -3 : for(int k = 0; k < 3; k++) speed_dots[k].color = dot_blue; break;
                case -2 : for(int k = 0; k < 2; k++) speed_dots[k].color = dot_blue; speed_dots[2].color = Color.clear; break;
                case -1 : speed_dots[0].color = dot_blue; for(int k = 1; k < 3; k++) speed_dots[k].color = Color.clear; break;
                case 0 : for(int k = 0; k < 3; k++) speed_dots[k].color = Color.clear; break;
                case 1 : speed_dots[0].color = dot_orange; for(int k = 1; k < 3; k++) speed_dots[k].color = Color.clear; break;
                case 2 : for(int k = 0; k < 2; k++) speed_dots[k].color = dot_orange; speed_dots[2].color = Color.clear; break;
                case 3 : for(int k = 0; k < 3; k++) speed_dots[k].color = dot_orange; break;
            }
        }

        if(power_grade != power_grade_cash)
        {
            power_grade_cash = power_grade;
            switch(power_grade)
            {
                case -3 : for(int k = 0; k < 3; k++) power_dots[k].color = dot_blue; break;
                case -2 : for(int k = 0; k < 2; k++) power_dots[k].color = dot_blue; power_dots[2].color = Color.clear; break;
                case -1 : power_dots[0].color = dot_blue; for(int k = 1; k < 3; k++) power_dots[k].color = Color.clear; break;
                case 0 : for(int k = 0; k < 3; k++) power_dots[k].color = Color.clear; break;
                case 1 : power_dots[0].color = dot_orange; for(int k = 1; k < 3; k++) power_dots[k].color = Color.clear; break;
                case 2 : for(int k = 0; k < 2; k++) power_dots[k].color = dot_orange; power_dots[2].color = Color.clear; break;
                case 3 : for(int k = 0; k < 3; k++) power_dots[k].color = dot_orange; break;
            }
        }

        if(defence_grade != defence_grade_cash)
        {
            defence_grade_cash = defence_grade;
            switch(defence_grade)
            {
                case -3 : for(int k = 0; k < 3; k++) defence_dots[k].color = dot_blue; break;
                case -2 : for(int k = 0; k < 2; k++) defence_dots[k].color = dot_blue; defence_dots[2].color = Color.clear; break;
                case -1 : defence_dots[0].color = dot_blue; for(int k = 1; k < 3; k++) defence_dots[k].color = Color.clear; break;
                case 0 : for(int k = 0; k < 3; k++) defence_dots[k].color = Color.clear; break;
                case 1 : defence_dots[0].color = dot_orange; for(int k = 1; k < 3; k++) defence_dots[k].color = Color.clear; break;
                case 2 : for(int k = 0; k < 2; k++) defence_dots[k].color = dot_orange; defence_dots[2].color = Color.clear; break;
                case 3 : for(int k = 0; k < 3; k++) defence_dots[k].color = dot_orange; break;
            }
        }
    }
}