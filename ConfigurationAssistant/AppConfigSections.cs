using System;
using System.Collections.Generic;
using System.Text;

namespace ConfigurationAssistant
{
    public interface IAppConfigSections
    {
        IApplicationSecrets appSecrets { get; set; }
        IApplicationSetupConfiguration appIntialConfig { get; set; }
    }

    public class AppConfigSections : IAppConfigSections
    {
        public IApplicationSecrets appSecrets { get; set; }
        public IApplicationSetupConfiguration appIntialConfig { get; set; }
    }


}
