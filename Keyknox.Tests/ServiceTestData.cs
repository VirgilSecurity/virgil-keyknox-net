using System;
namespace Keyknox.Tests
{
    using System;
    using Microsoft.Extensions.Configuration;

    public class ServiceTestData
    {
        public ServiceTestData(string serviceName)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true).Build();
            this.AppId = configuration[$"{serviceName}:AppId"];
            this.ApiPrivateKey = configuration[$"{serviceName}:ApiPrivateKey"];
            this.ApiPublicKeyId = configuration[$"{serviceName}:ApiPublicKeyId"];
            this.ServiceAddress = configuration[$"{serviceName}:ServiceAddress"];
        }

        public string AppId { get; private set; }

        public string ApiPrivateKey { get; private set; }

        public string ApiPublicKeyId { get; private set; }

        public string ServiceAddress { get; private set; }

    }
}