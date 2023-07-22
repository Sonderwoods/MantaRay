using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using MantaRay.Setup;
using System.Net;

namespace MantaRay.Helpers
{
    public class ManPageHelper
    {
        public static ManPageHelper Instance { get; private set; }

        public Dictionary<string, string> AllRadiancePrograms { get; set; } = new Dictionary<string, string>();

        public static readonly Regex filter = new Regex(@"(?i)<a([^>]+)>(.+?)<\/a>", RegexOptions.Compiled);



        /// <summary>
        /// Sets paths to all the manpages from https://floyd.lbl.gov/radiance/whatis.html
        /// </summary>
        /// <returns></returns>
        /// <exception cref="HttpRequestException"></exception>
        public async Task<bool> Fetch()
        {

            AllRadiancePrograms.Clear();
            using (var client = new HttpClient())
            {

                // Ugly and perhaps dangerous way of bypassing expired SSL certificates on floyd. however we are not sending any confidential info, are we?
                ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;


                client.DefaultRequestHeaders.Add("User-Agent", ConstantsHelper.ProjectName);
                client.Timeout = new TimeSpan(0, 0, 5);
                string content = default;
                string path = "https://floyd.lbl.gov/radiance/whatis.html";

                try
                {

                    content = await client.GetStringAsync(path);

                }
                catch (HttpRequestException e)
                {
                    Rhino.RhinoApp.WriteLine($"{ConstantsHelper.ProjectName}: Tried connecting to {path}, but timed out or url not found. Radiance ManPages not loaded");
                    throw new HttpRequestException(e.Message);
                }


                Match m = filter.Match(content);

                //m = m.NextMatch(); // skip first link

                while (m.Success)
                {

                    string name = m.Groups[2].Captures[0].Value;

                    string href = m.Groups[1].Captures[0].Value.Trim().Replace("\t", " ").Replace(" =", "=").Replace("= ", "=").Split(' ')
                        .Where(s => s.Split('=')[0].ToLower() == "href").Select(s => s.Split('=')[1]).First().Trim('"');

                    AllRadiancePrograms.Add(name, /*"https://floyd.lbl.gov/radiance/" +*/ href);

                    m = m.NextMatch();
                }

                AllRadiancePrograms["rtpict"] = "https://www.radiance-online.org/learning/documentation/manual-pages/pdfs/rtpict.pdf/at_download/file";
                AllRadiancePrograms["falsecolor"] = "https://floyd.lbl.gov/radiance/man_html/falsecolor.1.html";

                Debug.WriteLine("successfully created díct");

                SetGlobals();


                return true;
            }


        }

        public void SetGlobals()
        {
            lock (GlobalsHelper.Lock)
                GlobalsHelper.Globals["AllRadProgs"] = string.Join("\\|", AllRadiancePrograms.Keys);

        }

        public void OpenManual(string name)
        {
            if (!AllRadiancePrograms.ContainsKey(name)) return;

            string link = AllRadiancePrograms[name];

            Form prompt = new Form()
            {
                Width = 960,
                Height = 970,
                FormBorderStyle = FormBorderStyle.Sizable,
                Text = $"Man: {name}",
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.FromArgb(255, 195, 195, 195),
                ForeColor = Color.FromArgb(255, 30, 30, 30),
                TopMost = true,
                //Capture = false


            };
            WebBrowser webBrowser = new WebBrowser()
            {
                Url = new Uri(link),
                Dock = DockStyle.Fill,
            };


            prompt.Controls.AddRange(new Control[] { webBrowser });

            prompt.Show();

        }

        public static void Initiate()
        {
#if !DEBUG
            if (Instance == null)
            {

                Instance = new ManPageHelper();

                Task.Run(async () => await Task.Run(() => Instance.Fetch().ConfigureAwait(false)));

            }
            else
            {
                Instance.SetGlobals();
            }
#else


            Instance = new ManPageHelper();
            try
            {
                Task.Run(async () => await Task.Run(() => Instance.Fetch().ConfigureAwait(false)));

            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    Rhino.RhinoApp.WriteLine("[MantaRay.ManPageHelper]: " + e.Message);
                }
            }

#endif

        }


    }
}
