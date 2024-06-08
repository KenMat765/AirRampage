using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(menuName = "Help/Create HelpData", fileName = "HelpData")]
public class HelpData : ScriptableObject
{
    public string helpTitle;
    [ResizableTextArea] public string helpExplanation;
}
