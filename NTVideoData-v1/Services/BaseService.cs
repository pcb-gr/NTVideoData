using NTVideoData.Util;
using NTVideoData_v1.Entities;
using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using NTVideoData.Victims;
using System.IO;
using System.Collections;
using System.Collections.ObjectModel;
using NTVideoData_v1.Utils;
using System.Collections.Specialized;
using System.Text;

namespace NTVideoData.Services
{
    class BaseService : WebClient
    {
        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            w.Timeout = 20 * 60 * 1000;
            return w;
        }

        string movieInfoServicePath = "http://localhost:85/tools/fb/pcb-service.php";
        string rootSavePath = @"F:/movies";
        string fileName = "";
        bool isDownloading = false;
        bool isUploading = false;
        bool stopDownloadFlag = false;
        bool stopUploadFlag = false;

        bool doStartApache = false;

        public bool beginDownLoad()
        {
            while(!CMDUtil.checkPort(85))
            {
                if (!doStartApache)
                {
                    doStartApache = true;
                    CMDUtil.startApache();
                }
            }
            while (isDownloading || stopDownloadFlag)
            {
                Thread.Sleep(1000);
            }
            using (var context = new NTVideoEntities())
            {
                var infoDownloadQuery = from m in context.movies
                                        from v in context.victims
                                        where m.movieId == v.movieId && v.downloadStatus != "completed" && v.downloadStatus != "pending" && m.countries.Any(c => c.alias != "my")
                                        orderby m.publishYear descending, m.isMultiEpisode ascending
                                        select new { m, v };
                var movieInfo = infoDownloadQuery.FirstOrDefault();
                if (movieInfo.m != null)
                {
                    string movieAlias = movieInfo.m.alias;
                    var episodes = BaseVictim.instance().getEpisodes(movieInfo.v.movieHref, true);
                    var totalEpisode = episodes.Count();
                    var downloadedInfo = getEpisodesDownloadedInfo(movieInfo.v.victimId);
                    var episodeIndex = 0;
                    foreach (var episode in episodes)
                    {
                        var typeDownload = getTypeDownload(episode, downloadedInfo);
                        if (typeDownload == "none")
                        {
                            episodeIndex++;
                            continue;
                        } 
                        var typeMovieFolder = (movieInfo.m.isMultiEpisode == 1) ? "multi-episode" : "one-episode";
                        var countryFolder = movieInfo.m.countries.ToList<country>()[0].alias;
                        var savePath = rootSavePath + "/" + typeMovieFolder + "/" + countryFolder + "/" + movieAlias;
                        if (!Directory.Exists(savePath))
                        {
                            Directory.CreateDirectory(savePath);
                        }
                        fileName = episode.alias + ".mp4";
                        isDownloading = true;
                        string status = downLoad(movieAlias, episodeIndex, savePath, fileName);
                        if (episodeIndex == (totalEpisode - 1) && status != "error" && status != "pending") status = "completed";
                        saveDownloadInfoToDB((typeDownload == "update") ? getDownloadedByName(episode.name, downloadedInfo) : null, episode.alias, savePath, status, movieInfo.m, movieInfo.v, context);
                        episodeIndex++;
                        isDownloading = false;
                    }
                    
                }
            }
            return true;
        }

        private string getTypeDownload(episode episode, ICollection<download> downloadedInfo)
        {
            foreach (var episodeDownloaded in downloadedInfo)
            {
                if (episode.name == episodeDownloaded.name)
                {
                    return (episodeDownloaded.status == "completed") ? "none" : "update";
                }
            }
            return "new";
        }

        private download getDownloadedByName(string name, ICollection<download> downloadedInfo)
        {
            foreach (var episodeDownloaded in downloadedInfo)
            {
                if (name == episodeDownloaded.name)
                {
                    return episodeDownloaded;
                }
            }
            return null;
        }

        public bool beginUpLoad()
        {
            while (!CMDUtil.checkPort(85))
            {
                if (!doStartApache)
                {
                    doStartApache = true;
                    CMDUtil.startApache();
                }
            }
            while (isUploading || stopUploadFlag)
            {
                Thread.Sleep(1000);
            }
            using (var context = new NTVideoEntities())
            {
                var infoUploadQuery =   from m in context.movies
                                        from v in context.victims
                                        from d in context.downloads
                                        where m.movieId == v.movieId && d.victimId == v.victimId 
                                            && m.countries.Any(c => c.alias != "my") 
                                            && v.downloadStatus == "completed" 
                                            && v.uploadStatus != "completed"
                                        orderby m.publishYear, m.isMultiEpisode
                                        select new { m, v, d };
                var movieInfo = infoUploadQuery.FirstOrDefault();
                if (movieInfo.m != null)
                {
                    string movieAlias = movieInfo.m.alias;
                    var episodesDownloaded = movieInfo.v.downloads;
                    var episodeUploaded = movieInfo.v.uploads;
                    var episodeIndex = 0;
                    foreach (var episodeDownloaded in episodesDownloaded)
                    {
                        isUploading = true;
                        var title = movieInfo.m.name1 + "-" + movieInfo.m.name2 + ((movieInfo.m.isMultiEpisode == 1) ? (" - Tập " + episodeDownloaded.name) : "");
                        var description = HtmlUtil.StripHTML(movieInfo.v.description);
                        var file = episodeDownloaded.name + ".mp4";
                        string status = upLoad(title, description, episodeDownloaded.savePath,  file);
                        if (episodeIndex == (episodesDownloaded.Count() - 1) && status != "error" && status != "pending") status = "completed";
                        //saveDownloadInfoToDB(episodeDownloaded.alias, savePath, status, movieInfo.m, movieInfo.v, context);
                        episodeIndex++;
                        isUploading = false;
                    }

                }
            }
            return true;
        }

        public ICollection<download> getEpisodesDownloadedInfo(int victimId)
        {
            try
            {
                using (var context = new NTVideoEntities())
                {
                    var downloadInfoQuery = from d in context.downloads
                                 where d.victimId == victimId
                                 select d;
                    return downloadInfoQuery.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new HashSet<download>();
            }
        }

        public ICollection<episode> removeEpisodeDownloaded(ICollection<episode> episodes, ICollection<download> downloadedInfo)
        {
            ICollection<episode> episodesTemp = new Collection<episode>();
            if (downloadedInfo.Count == 0) return episodes;
            for(var i = 0; i < episodes.Count; i++)
            {
                if ( i > (downloadedInfo.Count - 1))
                {
                    episodesTemp.Add((episodes.ToList<episode>())[i]);
                }
            }
            return episodesTemp;
        }

        private bool saveDownloadInfoToDB(download updateDownload, string name, string savePath, string status, dynamic movie, dynamic victim, NTVideoEntities context)
        {
            victim.downloadStatus = status;
            if (updateDownload == null)
            {
                var download = new download()
                {
                    victimId = victim.victimId,
                    name = name,
                    savePath = savePath,
                    status = status
                };
                victim.downloads.Add(download);
                context.Entry(victim).State = System.Data.Entity.EntityState.Modified;
                
            } else
            {
                updateDownload.status = status;
                context.Entry(updateDownload).State = System.Data.Entity.EntityState.Modified;
            }
            context.SaveChanges();
            return true;
        }

        public string upLoad(string title, string description, string savePath, string file)
        {
            var data = "";
            string doneFile = "";
            using (WebClient client = new WebClient())
            {
                try
                {
                    doneFile = savePath + "/done.txt";
                    var values = new NameValueCollection();
                    values["action"] = "uploadFB";
                    values["title"] = title;
                    values["description"] = description;
                    values["savePath"] = savePath;
                    values["file"] = file;
                    client.UploadValues(movieInfoServicePath, values);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    while (!File.Exists(doneFile)) ;
                    data = File.ReadAllText(doneFile);
                }
            }
            return data;
        }

        public string downLoad(string movieAlias, int episodeIndex, string path, string fileName)
        {
            string movieSrc = getMovieSrc(movieAlias, episodeIndex);
            if (movieSrc != "error" && movieSrc != "pending")
            {
                CMDUtil.callDownLoadByIDM(movieSrc, path, fileName);
                return episodeIndex.ToString();
            }
            return movieSrc;
        }

        public string getMovieSrc(string movieAlias, int episodeIndex)
        {
            var data = "";
            using (WebClient client = new WebClient())
            {
                try
                {
                    var linkDownload = movieInfoServicePath + "?action=getMovieInfo&movieAlias=" + movieAlias + "&episodeIndex=" + episodeIndex;
                    data = client.DownloadString(linkDownload);
                } catch (Exception em)
                {
                    Console.WriteLine(em.Message);
                    return "error";
                }
            }
            return data;
        }

        public void stopDownload()
        {
            stopDownloadFlag = true;
        }

        public void startDownload()
        {
            stopDownloadFlag = false;
        }
    }
}
