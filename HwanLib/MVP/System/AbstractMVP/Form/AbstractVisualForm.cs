using System;
using MVP.System.BaseMVP.Form;
using MVP.UIData;

namespace MVP.System.AbstractMVP.Form
{
    public abstract class AbstractVisualForm : BaseForm, IUpdatable
    {
        private Func<UIParam> _source;

        public void BindUpdateSource(Func<UIParam> source) => _source = source;

        public void UpdateForm() => UpdateVisual(_source?.Invoke());

        protected abstract void UpdateVisual(UIParam data);
    }
}
