using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;


namespace GrasshopperRadianceLinuxConnector
{
    internal static class SSH_Helper
    {
        public static SshClient Client {
            get => client;
            set
            {
                if (client == null)
                    client = value;

                else
                {
                    client.Disconnect();
                    client.Dispose();
                    client = value;

                }

            }
        }
        private static SshClient client;

        public static void Disconnect()
        {
            client?.Disconnect();
            client?.Dispose();
            client = null;
        }
    }
}
