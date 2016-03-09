using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EloBuddy;
using EloBuddy.SDK;

namespace AutoRift.Utilities.AutoLvl
{
    internal class SkillGrabber
    {
        private struct ChampSkilltoLvl
        {
            public Champion Champ;
            public SkillToLvl[] S;
        }

        private struct ChampName
        {
            public Champion Champ;
            public string Name;
        }
        private string _status = "Updater started...";
        private List<ChampName> _cn;
        private readonly string _path;
        public SkillGrabber(string path)
        {
            this._path = path;
            
        }

        public void UpdateBuilds(bool[] locked = null)
        {
            if (locked != null)
                locked[0] = true;
            Drawing.OnEndScene += Drawing_OnDraw;
            BackgroundWorker bw = new BackgroundWorker {WorkerReportsProgress = true};
            bw.DoWork += delegate(object o, DoWorkEventArgs args)
            {
                BackgroundWorker b = o as BackgroundWorker;
                GenerateChampList();
                ToFile(b);

            };

            bw.ProgressChanged += delegate(object o, ProgressChangedEventArgs args)
            {
                _status = args.UserState.ToString();
            };

            bw.RunWorkerCompleted += delegate
            {
                _status = "Skill sequences updated succesfully.";
                Core.DelayAction(() => { Drawing.OnEndScene -= Drawing_OnDraw; }, 2000);
                if (locked != null)
                    locked[0] = false;
            };

            bw.RunWorkerAsync();
        }
        

        private void Drawing_OnDraw(EventArgs args)
        {
            Drawing.DrawText(800, 10, Color.Coral, _status, 14);
        }

        private void ToFile(BackgroundWorker bw=null)
        {
            
            List<string> stringi = new List<string>();
            foreach (string champLink in GetChampLinks("http://www.mobafire.com/league-of-legends/champions"))
            {


                ChampSkilltoLvl iss = GetSequence(GetBestBuildLink(champLink));
                if(bw!=null)
                    bw.ReportProgress(0, "Updating skill sequences, current champ: " + iss.Champ);
                else
                    _status = "Updating skill sequences, current champ: " + iss.Champ;
                string s = iss.Champ + "=";
                for (int i = 0; i < 18; i++)
                {
                    s += iss.S[i].ToString();
                    if (i < 17)
                        s += ";";
                }
                stringi.Add(s);

            }
            File.WriteAllLines(_path, stringi);
            
        }


        private List<string> GetChampLinks(string startingLink)
        {

            string resp = startingLink.GetResponseText();
            List<string> ret = new List<string>();
            List<int> ind = BrutalExtensions.AllIndexesOf(resp, "\" class=\"champ-box");
            foreach (int i in ind)
            {
                string s = resp.Substring(i - 60, 60);
                ret.Add(s.Substring(s.IndexOf("<a href=\"") + 9));
            }
            return ret;
        }

        private static string[] GetBestBuildLink(string champLink)
        {
            string resp = ("http://www.mobafire.com" + champLink).GetResponseText();
            string st =
    resp.Substring(
        resp.IndexOf("<span class=\"badge \"></span>") + 64, 200);
            string[] ret = new string[2];
            ret[0] = champLink.Substring(champLink.LastIndexOf("/")+1, champLink.LastIndexOf("-") - (champLink.LastIndexOf("/")+1));
            ret[1] = st.Substring(0, st.IndexOf("\" class=\"build-title"));
            
            return ret;
        }

        private ChampSkilltoLvl GetSequence(string[] nameGuide)
        {

            SkillToLvl[] seq = new SkillToLvl[18];
            for (int i = 0; i < 18; i++)
            {
                seq[i] = SkillToLvl.NotSet;
            }


            string resp = ("http://www.mobafire.com" + nameGuide[1]).GetResponseText();
            string q =
                resp.Substring(
                    resp.IndexOf("<div class=\"float-right\" style=\"margin-left:7px;\"><img src=\"/images/key-q.png\"") - 2000, 2000);
            q = q.Substring(q.LastIndexOf("<div class=\"float-left\" style=\"margin-left:7px;\">") + 62);


            MatchCollection matches = Regex.Matches(q, "[0-9]+");
            foreach (Match match in matches)
            {
                seq[int.Parse(match.ToString()) - 1] = SkillToLvl.Q;
            }


            q =
                resp.Substring(
                    resp.IndexOf("<div class=\"float-right\" style=\"margin-left:7px;\"><img src=\"/images/key-w.png\"") - 2000, 2000);
            q = q.Substring(q.LastIndexOf("<div class=\"float-left\" style=\"margin-left:7px;\">") + 62);


            matches = Regex.Matches(q, "[0-9]+");
            foreach (Match match in matches)
            {
                seq[int.Parse(match.ToString()) - 1] = SkillToLvl.W;
            }
            q =
    resp.Substring(
        resp.IndexOf("<div class=\"float-right\" style=\"margin-left:7px;\"><img src=\"/images/key-e.png\"") - 2000, 2000);
            q = q.Substring(q.LastIndexOf("<div class=\"float-left\" style=\"margin-left:7px;\">") + 62);


            matches = Regex.Matches(q, "[0-9]+");
            foreach (Match match in matches)
            {
                seq[int.Parse(match.ToString()) - 1] = SkillToLvl.E;
            }
            q =
    resp.Substring(
        resp.IndexOf("<div class=\"float-right\" style=\"margin-left:7px;\"><img src=\"/images/key-r.png\"") - 2000, 2000);
            q = q.Substring(q.LastIndexOf("<div class=\"float-left\" style=\"margin-left:7px;\">") + 62);


            matches = Regex.Matches(q, "[0-9]+");
            foreach (Match match in matches)
            {
                seq[int.Parse(match.ToString()) - 1] = SkillToLvl.R;
            }
            return new ChampSkilltoLvl
            {
                Champ = _cn.OrderByDescending(it => it.Name.Match(nameGuide[0])).First().Champ,
                S = seq
            };
        }


        private void GenerateChampList()
        {
            _cn = new List<ChampName>();
            foreach (ChampName c in from Champion champ in Enum.GetValues(typeof(Champion))
                                    select new ChampName
                                        {
                                            Champ = champ,
                                            Name = champ.ToString()
                                        })
            {
                _cn.Add(c);
            }
        }


    }
}

