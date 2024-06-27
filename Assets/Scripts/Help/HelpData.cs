using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Help/Create HelpData", fileName = "HelpData")]
public class HelpData : ScriptableObject
{
    public string helpTitle;
    [TextArea(8, 10)] public string helpExplanation;
}
