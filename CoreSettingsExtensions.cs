using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

namespace CodedThought.Core.Configuration
{
    public static class CoreSettingsExtensions
    {
        public static ApiConnectionSetting GetCorePrimaryApiConnection(this IConfiguration configuration)
        {
            CoreSettings options = new() { Settings = [], Connections = [] };
            configuration.GetSection(nameof(CoreSettings)).Bind(options);
            ConnectionSetting connection = options.Connections.FirstOrDefault(x => x.Primary == true && x.ProviderType.ToLower() == "apiserver");
            return connection != null ? new ApiConnectionSetting(connection) :  null;
        }
        public static ApiConnectionSetting GetCoreApiConnection(this IConfiguration configuration, string name)
        {
            CoreSettings options = new() { Settings = [], Connections = [] };
            configuration.GetSection(nameof(CoreSettings)).Bind(options);
            ConnectionSetting connection = options.Connections.FirstOrDefault(x => x.Name.ToLower() == name.ToLower() && x.ProviderType.ToLower() == "apiserver");
            return connection != null ? new ApiConnectionSetting(connection) :  null;
        }
    }
}
