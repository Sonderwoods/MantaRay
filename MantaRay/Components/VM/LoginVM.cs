using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.Components.VM
{
    public class LoginVM
    {
        public string Ip { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }

        public int Port { get; set; }
        public bool LocalIp(string ip) => string.Equals(ip, "127.0.0.1") || string.Equals(ip, "localhost");

        


    }
}
