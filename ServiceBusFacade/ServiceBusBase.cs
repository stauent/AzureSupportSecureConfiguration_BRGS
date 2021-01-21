using System;
using System.Collections.Generic;
using System.Text;
using ConfigurationAssistant;

namespace ServiceBusFacade
{
    public class ServiceBusBase
    {
        protected readonly IApplicationSecrets _applicationSecrets;

        public ServiceBusBase(IApplicationSecrets applicationSecrets)
        {
            _applicationSecrets = applicationSecrets;
        }
    }

}
