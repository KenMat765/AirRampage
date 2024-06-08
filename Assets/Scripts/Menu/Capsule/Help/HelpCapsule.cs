using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net.Http.Headers;

public class HelpCapsule : MonoBehaviour
{
    Capsule helpCapsule;

    [SerializeField] RectTransform helpListTrans, explanationTrans;
    [SerializeField] Image highlight;
    Button[] helpButtons = new Button[contents_count];
    TextMeshProUGUI[] helpContentTexts = new TextMeshProUGUI[contents_count];
    TextMeshProUGUI helpTitle, helpExplanation;

    const int contents_count = 5;
    int max_page;
    int current_page
    {
        get { return Current_Page; }
        set { Current_Page = value % max_page; }
    }
    int Current_Page = 0;
    int current_id = -1;

    void Start()
    {
        max_page = Mathf.CeilToInt(HelpDatabase.I.help_count / (float)contents_count);
        highlight.color = Color.clear;

        // Get Components
        for (int k = 0; k < contents_count; k++)
        {
            Transform helpContent_trans = helpListTrans.GetChild(k);
            helpButtons[k] = helpContent_trans.GetComponent<Button>();
            helpContentTexts[k] = helpContent_trans.GetComponentInChildren<TextMeshProUGUI>();
        }
        helpTitle = explanationTrans.Find("Title").GetComponent<TextMeshProUGUI>();
        helpExplanation = explanationTrans.Find("Frame/Text").GetComponent<TextMeshProUGUI>();

        // Initialize components
        ShowPage(0);
        ClearExplanation();
        foreach (Button button in helpButtons)
        {
            button.interactable = false;
        }

        // Setup capsule actions
        helpCapsule = GetComponentInParent<Capsule>();
        helpCapsule.finish_open_action = () =>
        {
            ShowPage(current_page);
        };
        helpCapsule.start_close_action = () =>
        {
            foreach (Button button in helpButtons)
            {
                button.interactable = false;
            }
            current_id = -1;
            ClearExplanation();
            highlight.color = Color.clear;
        };
    }

    public void OnPressedHelp(int num)
    {
        int help_id = current_page * contents_count + num;
        if (help_id == current_id) // Same help selected
        {
            current_id = -1;
            ClearExplanation();
            highlight.color = Color.clear;
        }
        else // New help selected
        {
            current_id = help_id;
            ShowExplanation(help_id);
            highlight.rectTransform.position = helpContentTexts[num].rectTransform.position;
            highlight.color = Color.white;
        }
    }

    /// <param name="direction"> 0&+:down, -:up </param>
    public void OnPressedArrow(int direction)
    {
        // previous page (go up)
        if (direction < 0)
        {
            current_page--;
        }
        // next page (go down)
        else
        {
            current_page++;
        }
        highlight.color = Color.clear;
        ShowPage(current_page);
        ClearExplanation();
    }

    void ShowPage(int page)
    {
        for (int k = 0; k < contents_count; k++)
        {
            int help_id = page * contents_count + k;
            if (help_id >= HelpDatabase.I.help_count)
            {
                helpContentTexts[k].text = "";
                helpButtons[k].interactable = false;
                continue;
            }
            HelpData help_data = HelpDatabase.I.GetHelpDataById(help_id);
            helpContentTexts[k].text = help_data.helpTitle;
            helpButtons[k].interactable = true;
        }
    }

    void ClearExplanation()
    {
        helpTitle.text = "";
        helpExplanation.text = "";
    }

    void ShowExplanation(int help_id)
    {
        HelpData help_data = HelpDatabase.I.GetHelpDataById(help_id);
        helpTitle.text = help_data.helpTitle;
        helpExplanation.text = help_data.helpExplanation;
    }
}
