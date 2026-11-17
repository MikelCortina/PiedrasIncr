using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PiedrasIncr.Options
{
    public sealed class OptionsCycleRow : MonoBehaviour
    {
        public Button previous;
        public Button next;
        public TMP_Text value;

        public bool IsComplete => previous != null && next != null && value != null;

        public void SetValue(string text)
        {
            if (value != null)
                value.text = text;
        }

        public void SetInteractive(bool active)
        {
            if (previous != null)
                previous.interactable = active;
            if (next != null)
                next.interactable = active;
        }
    }

}
