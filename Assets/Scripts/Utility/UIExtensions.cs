using UnityEngine;

namespace Originals.UIExtensions
{
    using UnityEngine.UI;

    public class RadioButton
    {
        Toggle[] toggles;

        public RadioButton(params Toggle[] toggles)
        {
            int toggle_count = toggles.Length;

            if (toggle_count < 2)
            {
                Debug.LogError("You need at least 2 Toggles to use RadioButton!!");
                return;
            }

            this.toggles = new Toggle[toggle_count];
            for (int k = 0; k < toggle_count; k++)
            {
                Toggle toggle = toggles[k];
                this.toggles[k] = toggle;
            }
        }

        public void Interactable(bool interactable)
        {
            if (interactable)
            {
                foreach (Toggle toggle in toggles)
                {
                    toggle.interactable = !toggle.isOn;
                }
            }
            else
            {
                foreach (Toggle toggle in toggles)
                {
                    toggle.interactable = false;
                }
            }
        }

        public void SwtichToggle(Toggle target_toggle, bool maintain_interactable = false)
        {
            foreach (Toggle toggle in toggles)
            {
                if (toggle == target_toggle)
                {
                    toggle.isOn = true;
                    if (!maintain_interactable)
                    {
                        toggle.interactable = false;
                    }
                }
                else
                {
                    toggle.isOn = false;
                    if (!maintain_interactable)
                    {
                        toggle.interactable = true;
                    }
                }
            }
        }
    }
}
