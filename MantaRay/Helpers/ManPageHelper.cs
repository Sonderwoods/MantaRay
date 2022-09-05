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

namespace MantaRay
{
    public class ManPageHelper
    {
        public static ManPageHelper Instance { get; private set; }

        public Dictionary<string, string> AllRadiancePrograms { get; set; } = new Dictionary<string, string>();

        public static readonly Regex filter = new Regex(@"(?i)<a([^>]+)>(.+?)<\/a>", RegexOptions.Compiled);



        public async Task<bool> Fetch()
        {

            AllRadiancePrograms.Clear();
            using (var client = new HttpClient())
            {
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

                m = m.NextMatch(); // skip first link

                while (m.Success)
                {

                    string name = m.Groups[2].Captures[0].Value;

                    string href = m.Groups[1].Captures[0].Value.Trim().Replace("\t", " ").Replace(" =", "=").Replace("= ", "=").Split(' ')
                        .Where(s => s.Split('=')[0].ToLower() == "href").Select(s => s.Split('=')[1]).First().Trim('"');

                    AllRadiancePrograms.Add(name, "https://floyd.lbl.gov/radiance/" + href);

                    m = m.NextMatch();
                }

                AllRadiancePrograms.Add("rtpict", "https://www.radiance-online.org/learning/documentation/manual-pages/pdfs/rtpict.pdf/at_download/file");

                Debug.WriteLine("successfully created díct");

                SetGlobals();
                

                return true;
            }


        }

        public void SetGlobals()
        {
            GlobalsHelper.Globals["AllRadProgs"] = String.Join("\\|", AllRadiancePrograms.Keys);
                
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

            Task.Run(async () => await Task.Run(() => Instance.Fetch().ConfigureAwait(false)));

#endif

        }


    }
}
