using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

[assembly: UserSecretsId("dnc-o365-mx")]

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

            (new Program()).Run(settings);
        }

        private void Run(AppSettings config){
            var cert = LoadCertificateFromFile("cert.pfx", config.CertificatePassword);
            //AddCert(cert);
            //var cert2 = LoadCertificateFromStore(config.CertificateThumbPrint);

            var tenant = config.TenantId;
            var authority = $"https://login.windows.net/{tenant}";

            var authContext = new AuthenticationContext(authority, false);
            var clientAssertion = new ClientAssertionCertificate(config.ClientId, cert);

            using (HttpClient client = SetUpClient(authContext, clientAssertion, "https://manage.office.com"))
            {
                var url = $"https://manage.office.com/api/v1.0/{tenant}/activity/feed/subscriptions/list";

                using(var resp = client.GetAsync(url).Result)
                {
                    var status = resp.StatusCode;
                    var content = resp.Content.ReadAsStringAsync().Result;
                }
            }
        }

        private HttpClient SetUpClient(AuthenticationContext authContext, ClientAssertionCertificate assertion, string resource)
        {
            var authResult = authContext.AcquireTokenAsync(resource, assertion).Result;
            var token = authResult.AccessToken;

            //Trace.WriteLine($"Access token: {token}");

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            return client;
        }

        private X509Certificate2 AddCert(X509Certificate2 cert){

            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
            store.Dispose();

            return null;
        }

        private X509Certificate2 LoadCertificateFromStore(string thumb){

            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumb, false);
            return certs[0];
        }

        private X509Certificate2 LoadCertificateFromFile(string fileName, string password)
        {

            var certFile = File.OpenRead(fileName);
            var certBytes = new Byte[certFile.Length];
            certFile.Read(certBytes, 0, (int)certFile.Length);

            var flags = X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet;

            var cert = new X509Certificate2(certBytes, password, flags);
            return cert;

        }
    }
}
