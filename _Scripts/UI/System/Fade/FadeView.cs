using MVP.Forms;
using MVP.System.BaseMVP;
using UnityEngine;

namespace UI.System.Fade
{
    public class FadeView : BaseView
    {
        [SerializeField] private FadeForm fadeForm;

        public FadeForm Fade => fadeForm;
    }
}
