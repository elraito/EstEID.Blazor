using System;
using EstEID.Blazor.Models;

namespace EstEID.Blazor.Services
{
    public class EventService
    {
        public event EventHandler<PersonData> UiUpdater;

        public void CallUiUpdate(PersonData personData)
        {
            OnUiUpdater(personData);
        }

        protected virtual void OnUiUpdater(PersonData e)
        {
            UiUpdater?.Invoke(this, e);
        }
    }
}