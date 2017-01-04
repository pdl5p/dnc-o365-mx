using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

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

        private void Run(AppSettings config)
        {
            var cert = LoadCertificateFromFile("cert.pfx", config.CertificatePassword);
            //AddCert(cert);
            //var cert2 = LoadCertificateFromStore(config.CertificateThumbPrint);

            var tenant = config.TenantId;
            var authority = $"https://login.windows.net/{tenant}";

            var authContext = new AuthenticationContext(authority, false);
            var assertion = new ClientAssertionCertificate(config.ClientId, cert);

            //DeleteMxSubs(authContext, assertion, tenant);
            //ListMxSubs(authContext, assertion, tenant);
            //CreateMxSubs(authContext, assertion, tenant);
            //ListMxContent(authContext, assertion, tenant);
        }

        private void GetAction(AuthenticationContext authContext, ClientAssertionCertificate assertion, string url)
        {

            using (HttpClient client = SetUpClient(authContext, assertion, "https://manage.office.com"))
            {
                using (HttpResponseMessage resp = client.GetAsync(url).Result)
                {
                    var status = resp.StatusCode;
                    var contentString = resp.Content.ReadAsStringAsync().Result;
                }
            }
        }

        private void PostAction(AuthenticationContext authContext, ClientAssertionCertificate assertion, string url, string payload="")
        {
            using (HttpClient client = SetUpClient(authContext, assertion, "https://manage.office.com"))
            {

                HttpContent content = new StringContent(payload);
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json;utf-8");

                using (HttpResponseMessage resp = client.PostAsync(url, content).Result)
                {
                    var status = resp.StatusCode;
                    var contentString = resp.Content.ReadAsStringAsync().Result;
                }
            }
        }

        private void ListMxContent(AuthenticationContext authContext, ClientAssertionCertificate assertion, string tenantId)
        {
            var url = $"https://manage.office.com/api/v1.0/{tenantId}/activity/feed/subscriptions/content?contentType=Audit.SharePoint";
            GetAction(authContext, assertion, url);
        }
        private void CreateMxSubs(AuthenticationContext authContext, ClientAssertionCertificate assertion, string tenantId)
        {
            var payload = new
            {
                webhook = new
                {
                    address = "https://funfunfunctions.azurewebsites.net/api/o365mx",
                }
            };

            var url = $"https://manage.office.com/api/v1.0/{tenantId}/activity/feed/subscriptions/start?contentType=Audit.SharePoint";
            PostAction(authContext, assertion, url, JsonConvert.SerializeObject(payload));
        }

        private void DeleteMxSubs(AuthenticationContext authContext, ClientAssertionCertificate assertion, string tenantId)
        {
            var url = $"https://manage.office.com/api/v1.0/{tenantId}/activity/feed/subscriptions/stop?contentType=Audit.SharePoint";
            PostAction(authContext, assertion, url);
        }

        private void ListMxSubs(AuthenticationContext authContext, ClientAssertionCertificate assertion, string tenantId)
        {
            var url = $"https://manage.office.com/api/v1.0/{tenantId}/activity/feed/subscriptions/list";
            GetAction(authContext, assertion, url);
        }

        private HttpClient SetUpClient(AuthenticationContext authContext, ClientAssertionCertificate assertion, string resource)
        {
            var authResult = authContext.AcquireTokenAsync(resource, assertion).Result;
            var token = authResult.AccessToken;

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            return client;
        }

        private X509Certificate2 AddCert(X509Certificate2 cert)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
            store.Dispose();

            return null;
        }

        private X509Certificate2 LoadCertificateFromStore(string thumb)
        {
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
