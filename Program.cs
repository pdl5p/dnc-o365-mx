using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

[assembly: UserSecretsId("dnc-o365-mx-0000")]

namespace ConsoleApplication
{

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddEnvironmentVariables()
                    .AddUserSecrets();

            var config = builder.Build();
            var settings = new AppSettings();
            config.Bind(settings);



        }
    }
}
