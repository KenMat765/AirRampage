using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class FighterStatus
{
    public float value { get; private set; }
    public float defaultValue { get; private set; }

    // (Debuff) -3 << 0 >> 3 : (Buff)
    public int grade { get; private set; }
    public float gradeDuration { get; private set; }
    float gradeTimer;

    // Use this when you want to temporarily assign a different value to the status, suspending updates based on grade.
    Dictionary<Guid, float> tmpStatusStack;

    public FighterStatus(float defaultValue)
    {
        this.defaultValue = defaultValue;
        tmpStatusStack = new Dictionary<Guid, float>();
        Reset();
    }

    public void Reset()
    {
        value = defaultValue;
        grade = 0;
        gradeDuration = 0;
        gradeTimer = 0;
        tmpStatusStack.Clear();
    }

    public void Timer()
    {
        if (grade != 0)
        {
            gradeTimer += Time.deltaTime;
            if (gradeTimer > gradeDuration)
            {
                Reset();
            }
        }
    }

    public void Grade(int delta_grade, float duration)
    {
        // Update grade.
        int pre_grade = grade;
        grade += delta_grade;
        grade = Mathf.Clamp(grade, -3, 3);

        // Update grade duration.
        if (grade == 0)
        {
            gradeTimer = 0;
            gradeDuration = 0;
        }
        else
        {
            if (pre_grade == 0)
            {
                gradeTimer = 0;
                gradeDuration = duration;
            }
            else
            {
                // When buffs and debuffs are swapped.
                bool sign_flipped = pre_grade * grade < 0;
                if (sign_flipped)
                {
                    gradeTimer = 0;
                    gradeDuration = duration;
                }
                else
                {
                    gradeDuration += duration;
                }
            }
        }

        // Update status value only if temporary status is none.
        if (tmpStatusStack.Count == 0)
        {
            UpdateStatusByGrade();
        }
    }

    /// <returns>Guid used to remove added temp value.</returns>
    public Guid ApplyTempStatus(float tmp_value)
    {
        Guid guid = Guid.NewGuid();
        tmpStatusStack[guid] = tmp_value;
        value = tmp_value;
        return guid;
    }

    /// <param name="guid">Use the same guid published from ApplyTempStatus.</param>
    public void RemoveTempStatus(Guid guid)
    {
        if (!tmpStatusStack.ContainsKey(guid))
        {
            return;
        }
        tmpStatusStack.Remove(guid);

        // If all temporary status were removed, resume updating status by grade.
        if (tmpStatusStack.Count == 0)
        {
            UpdateStatusByGrade();
        }

        // If temporary status still remains, apply the last value to current status.
        else
        {
            float tmp_value = tmpStatusStack.Last().Value;
            value = tmp_value;
        }
    }

    void UpdateStatusByGrade()
    {
        float multiplier = 1.0f;
        switch (grade)
        {
            case -3: multiplier = 1.0f / 2.0f; break;
            case -2: multiplier = 1.0f / 1.5f; break;
            case -1: multiplier = 1.0f / 1.2f; break;
            case 1: multiplier = 1.2f; break;
            case 2: multiplier = 1.5f; break;
            case 3: multiplier = 2.0f; break;
        }
        value = defaultValue * multiplier;
    }

}
