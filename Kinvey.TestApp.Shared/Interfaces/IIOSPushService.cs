using System;
using System.Collections.Generic;
using System.Text;

namespace Kinvey.TestApp.Shared.Interfaces
{
    public interface IIOSPushService
    {
        void Register();

        void UnRegister();
    }
}
