using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NTVideoData.Daos;
using NTVideoData_v1.Entities;
using NTVideoData.Util;
using NTVideoData.Victims;
using System.Data.Entity.Validation;
using System.Threading;

namespace LifeOfVictim
{
    public partial class LifeOfVictim : Form
    {
        public LifeOfVictim()
        {
            InitializeComponent();
        }

        private Thread findVictimsNeedUpdateThread, threadUpdatingVictim;
        private bool isUpating = false;
        public Dictionary<string, object> VICTIM_INSTANCE = new Dictionary<string, object>()
        {
            {"0", new PhimbathuVictim()},
            {"1", new BilutvVictim()},
            {"2", new PhimMoiVictim()}
        };

        private List<episode_need_update_to_live> episodesNeedUpdateToLive = new List<episode_need_update_to_live>();

        BaseVictim victimServiceInstance = null;

        private void InitializeTimerFindVictimsNeedUpdate()
        {
            findVictimsNeedUpdateThread = new Thread(() =>
            {
                while (findVictimsNeedUpdateThread.IsAlive)
                {
                    
                    var episodeNeedUpdateToLive = findEpisodeNeedUpdateToLive();
                    if (episodeNeedUpdateToLive != null && !episodesNeedUpdateToLive.Any(e => e.episodeUpdateToLiveId == episodeNeedUpdateToLive.episodeUpdateToLiveId))
                    {
                        episodesNeedUpdateToLive.Add(episodeNeedUpdateToLive);
                    }
                    Thread.Sleep(10);
                }

            });
            findVictimsNeedUpdateThread.IsBackground = true;
            findVictimsNeedUpdateThread.SetApartmentState(ApartmentState.STA);
            findVictimsNeedUpdateThread.Start();
        }

        private void InitializeTimerUpdatingVictimThread()
        {
            threadUpdatingVictim = new Thread(() =>
            {
                while (threadUpdatingVictim.IsAlive)
                {
                    try
                    {
                        Console.WriteLine("victimsNeedUpdate length: " + episodesNeedUpdateToLive.Count);
                        while (isUpating || episodesNeedUpdateToLive.Count == 0) Thread.Sleep(1000);
                        var enutl = episodesNeedUpdateToLive[0];
                        using (var context = new NTVideoEntities())
                        {
                            var episodeNeedUpdateQuery = from e in context.episode_need_update_to_live
                                                    where e.status == 0 && e.episodeUpdateToLiveId == enutl.episodeUpdateToLiveId //status = 0(not ok), 1(ok)
                                                    select e;
                            var episodeNeedUpdateToLive = episodeNeedUpdateQuery.FirstOrDefault();
                            if (episodeNeedUpdateToLive != null)
                            {
                                isUpating = true;
                                victimServiceInstance = (BaseVictim)VICTIM_INSTANCE[episodeNeedUpdateToLive.victim.victimTypeNo.ToString()];
                                episodeNeedUpdateToLive.victim.playListHtml = getEpisodePlayerInfoByIndexAndRebuildPlayListHtml(Int32.Parse(episodeNeedUpdateToLive.serverIndex.ToString()), Int32.Parse(episodeNeedUpdateToLive.episodeIndex.ToString()), episodeNeedUpdateToLive.victim.playListHtml);
                                episodeNeedUpdateToLive.status = 1;
                                context.Entry(episodeNeedUpdateToLive.victim).State = System.Data.Entity.EntityState.Modified;
                                context.SaveChanges();
                                episodesNeedUpdateToLive.RemoveAt(0);
                                isUpating = false;
                            }
                        }
                    }
                    catch (DbEntityValidationException e)
                    {
                        foreach (var eve in e.EntityValidationErrors)
                        {
                            Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                                eve.Entry.Entity.GetType().Name, eve.Entry.State);
                            foreach (var ve in eve.ValidationErrors)
                            {
                                Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                                    ve.PropertyName, ve.ErrorMessage);
                            }
                        }
                        episodesNeedUpdateToLive.RemoveAt(0);
                        isUpating = false;
                    }
                    
                }

            });
            threadUpdatingVictim.IsBackground = true;
            threadUpdatingVictim.SetApartmentState(ApartmentState.STA);
            threadUpdatingVictim.Start();
        }

        private string getEpisodePlayerInfoByIndexAndRebuildPlayListHtml(int serverIndex, int episodeIndex, string playListHtmlString)
        {
            var playListHtml = WebParserUtil.convertDomStringToHtmlNode(playListHtmlString);
            var episodePlayerInfo = playListHtml.SelectNodes(".//div[@class='server-item']")[serverIndex].SelectNodes(".//div[@class='episode-item']")[episodeIndex];
            var moviePlayerInfoLink = episodePlayerInfo.Attributes["moviePlayerInfoLink"].Value;
            var episodePlayerInfoReplace = "<div moviePlayerInfoLink='" + moviePlayerInfoLink + "' class='episode-item' name='" + episodePlayerInfo.Attributes["name"].Value + "'>";
            episodePlayerInfoReplace += "var playerSetting=" + victimServiceInstance.getEpisodePlayerInfo(moviePlayerInfoLink);
            episodePlayerInfoReplace += "</div>";
            episodePlayerInfo.ParentNode.ReplaceChild(WebParserUtil.convertDomStringToHtmlNode(episodePlayerInfoReplace), episodePlayerInfo);
            return playListHtml.OuterHtml;
        }

        private void processingForLive()
        {
            deleteAllEpisodeThatOldDate();
            // step1: store victim is need update
            InitializeTimerFindVictimsNeedUpdate();
            // step2: loop victimsNeedUpdate to update. 
            InitializeTimerUpdatingVictimThread();
        }

        
        private void processingForLiveBt_Click(object sender, EventArgs e)
        {
            processingForLive();
        }

        private episode_need_update_to_live findEpisodeNeedUpdateToLive()
        {
            try
            {
                using (var context = new NTVideoEntities())
                {
                    var victimUpdateQuery = from enutl in context.episode_need_update_to_live
                                            where enutl.status == 0 && enutl.victim.victimTypeNo == 0 //lifeOfVictimStatus = 0(not ok), 1(ok)
                                            select enutl;
                    var episodeNeedUpdateToLive = victimUpdateQuery.FirstOrDefault();
                    return episodeNeedUpdateToLive;
                }
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                return null;
            }
        }

        private void deleteAllEpisodeThatOldDate()
        {
            try
            {
                using (var context = new NTVideoEntities())
                {
                   
                    var episodesNeedDeleteQuery = from enutl in context.episode_need_update_to_live
                                            where enutl.dateCreate < DateTime.Today
                                            select enutl;
                    var a = episodesNeedDeleteQuery.ToArray();
                    context.episode_need_update_to_live.RemoveRange(episodesNeedDeleteQuery.ToArray());
                    context.SaveChanges();
                }
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
            }
        }
    }

}
