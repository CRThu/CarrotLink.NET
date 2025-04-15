using System.Net;
using System.Net.Sockets;
using Makaretu.Dns;


namespace CarrotLink.Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("[CarrotLink.Server]");
            Console.WriteLine("Hello, World!");

            switch(Console.ReadKey().KeyChar)
            {
                case 's':
                    await Server();
                    break;
                case 'c':
                    await Client();
                    break;
                default:
                    break;
            }

            await Task.CompletedTask;
        }

        static async Task Server()
        {
            var sd = new ServiceDiscovery();
            var p = new ServiceProfile("ipfs1", "_ipfs-discovery._udp", 5010);
            p.AddProperty("connstr", "Server");
            sd.AnswersContainsAdditionalRecords = true;
            sd.Advertise(p);
            //sd.Announce(p);
            Console.ReadKey();
            sd.Unadvertise();
        }

        static async Task Client()
        {
            var mdns = new MulticastService();
            var sd = new ServiceDiscovery(mdns);

            sd.ServiceInstanceDiscovered += (s, e) =>
            {
                if (e.Message.Answers.All(w => !w.Name.ToString().Contains("ipfs1"))) return;
                Console.WriteLine($"service instance '{e.ServiceInstanceName}'");

                // Ask for the service instance details.
                mdns.SendQuery(e.ServiceInstanceName, type: DnsType.SRV);
            };

            mdns.AnswerReceived += (s, e) =>
            {
                if (e.Message.Answers.All(w => !w.Name.ToString().Contains("ipfs1"))) return;
                // Is this an answer to a service instance details?
                var servers = e.Message.Answers.OfType<SRVRecord>();
                foreach (var server in servers)
                {
                    Console.WriteLine($"[SRVRecord] host '{server.Target}' for '{server.Name}'");

                    // Ask for the host IP addresses.
                    mdns.SendQuery(server.Target, type: DnsType.A);
                    //mdns.SendQuery(server.Target, type: DnsType.AAAA);
                }

                // Is this an answer to host addresses?
                var addresses = e.Message.Answers.OfType<AddressRecord>();
                foreach (var address in addresses)
                {
                    if (address.Address.AddressFamily == AddressFamily.InterNetwork)
                        Console.WriteLine($"[AddressRecord] host '{address.Name}' at {address.Address}");
                }
                // Get connectionstring from DNS TXT record.
                var txts = e.Message.Answers.OfType<TXTRecord>();
                foreach (var txt in txts)
                {
                    //“connstr=Server”，获得对应connstr值
                    Console.WriteLine($"[TXTRecord] {txt.Strings.Single(w => w.Contains("connstr")).Split('=')[1]}");
                    //Console.WriteLine($"host '{address.Name}' at {address.Address}");
                }
            };

            try
            {
                mdns.Start();
                sd.QueryServiceInstances("_ipfs-discovery._udp");
                Console.ReadKey();
            }
            finally
            {
                sd.Dispose();
                mdns.Stop();
            }
        }
    }
}