using HtmlAgilityPack;
using NTVideoData.Daos;
using NTVideoData_v1.Entities;
using NTVideoData.Util;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Data.Entity.Validation;
using NTVideoData_v1.Utils;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using NTVideoData_v1;

namespace NTVideoData.Victims
{
    public class PhimbathuVictim : BaseVictim
    {
        public PhimbathuVictim()
        {
            victimDomain = "phimbathu.com";
            victimDomainNoExt = victimDomain.Split('.')[0];
            victimMediaDomain = "media." + victimDomain;
            victimTypeNo = 0;
            victimMediaController += victimTypeNo + "/";
            destToGetUrls[0] = protocol + victimDomain + "/film/filter?order=publish_date";
            destToGetUrls[1] = protocol + victimDomain + "/danh-sach/phim-le.html?order=publish_date";
            destToGetUrls[2] = protocol + victimDomain + "/danh-sach/phim-bo.html?order=publish_date";
            destToGetUrls[3] = protocol + victimDomain + "/film/filter?year=2017";
            pageIndexParam = "?page=";
           
        }

        public override HtmlNodeCollection getItemsInPage(int pageIndex)
        {
            string[] currentDestToGetUrlInfo = currentDestToGetUrl.Split('?');
            string urlParams = (currentDestToGetUrlInfo.Length > 1) ? "&" + currentDestToGetUrlInfo[1] : "";
            string pageUrl = currentDestToGetUrlInfo[0] + pageIndexParam + pageIndex + urlParams;
            logForm.append("Begin getItemsInPage [pageUrl]:" + pageUrl);
            var ul = WebParserUtil.selectNodes(pageUrl, "//div[@class='left-content'] //ul");
            return (ul!=null) ? ul[0].SelectNodes(".//a") : null;
        }

        public override void doGetListItemsInfo(int pageIndex)
        {
            HtmlNodeCollection items = getItemsInPage(pageIndex);
            if (items != null)
            {
                for (int j = 0; j < items.Count; j++)
                {
                    while (stopInsertFlag)
                    {
                        Thread.Sleep(10);
                    }
                    string href = items[j].Attributes["href"].Value;
                    movie movieItemInfo = getMovieItemInfo(UriUtil.checkAndAddDomain(href, victimDomain, protocol));
                    if (movieItemInfo == null) continue;
                    saveNewMovie(movieItemInfo);
                }
                pageIndex++;
                doGetListItemsInfo(pageIndex);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("[" + victimDomain + "] - doGetListItemsInfo finished!!!");
            }
        }

        //public override movie getMovieItemInfo(string itemUrl)
        //{
        //    victimDetailContent = getRootNode(itemUrl);
        //    if (is404(victimDetailContent)) return null;
        //    if (checkIgnoreMovie(victimDetailContent)) return null;
        //    movie mv = getMovie(victimDetailContent);
        //    if (mv == null || movieExistedWithVictim(mv, victimDomain))  return null;
        //    victim vtm = getVictim(victimDetailContent);
        //    if (vtm == null)  return null;
        //    vtm.movieDetailHref = itemUrl;
        //    //getAndAddEpisodesToVictim(vtm);      
        //    if (!movieExisted(mv))
        //    {
        //        vtm.isDefault = 1;
        //        mv.directors = getDirectors(victimDetailContent);
        //        mv.actors = getActors(victimDetailContent);
        //        mv.countries = getCountries(victimDetailContent);
        //        mv.categories  = getCategories(victimDetailContent);
        //        mv.keywords = buildKeyWords(mv);
        //    } else
        //    {
        //        vtm.movieId = findMovieByCheckFields(mv.name1CheckField, mv.name2CheckField).movieId;
        //    }
        //    mv.victims.Add(vtm);
        //    return mv;
        //}

        public override movie getMovieItemInfo(string itemUrl)
        {
            victimDetailContent = getRootNode(itemUrl);
            if (is404(victimDetailContent) || checkIgnoreMovie(victimDetailContent))
            {
                logForm.append("   !!! Skip the movie !!!");
                return null;
            } 
            movie mv = getMovie(victimDetailContent);

            string[] checkFields = { mv.name1CheckField, mv.name2CheckField };
            if (!movieExisted(string.Join("-", checkFields)))
            {
                victim vtm = getVictim(victimDetailContent);
                if (vtm == null) return null;
                vtm.movieDetailHref = itemUrl;
                vtm.isDefault = 1;
                mv.directors = getDirectors(victimDetailContent);
                mv.actors = getActors(victimDetailContent);
                mv.countries = getCountries(victimDetailContent);
                mv.categories = getCategories(victimDetailContent);
                mv.keywords = buildKeyWords(mv);
                mv.victims.Add(vtm);
                return mv;
            }
            return null;
        }


        public override bool checkIgnoreMovie(HtmlNode victimDetailContent)
        {
            try
            {
                HtmlNodeCollection dd = victimDetailContent.SelectNodes("//div[@class='dinfo'] //dd");
                if (dd == null) return true;
                var countries = dd[dd.Count - 1].InnerText.Trim().ToLower();
                var genres = dd[2].InnerText.Trim().ToLower();
                if (countries.IndexOf("việt nam") != -1) return true;
                return false;
            }
            catch(Exception ex)
            {
                return true;
            }
   
        }

        public override bool is404(HtmlNode victimDetailContent)
        {
            if (victimDetailContent == null) return true;
            HtmlNodeCollection dd = victimDetailContent.SelectNodes("//div[@class='dinfo'] //dd");
            return (dd == null);
        }

        public override movie getMovie(HtmlNode victimDetailContent)
        {
            
            HtmlNodeCollection dd = victimDetailContent.SelectNodes("//div[@class='dinfo'] //dd");
            var currentMovieInfo = dd[4].InnerText.Trim().ToLower().Replace("bản đẹp", "HD").ToLower();
            var title = UnicodeUtil.replaceSpecialCharacter(victimDetailContent.SelectSingleNode("//div[@class='blockbody'] //p[@class='title']").InnerText.Trim());
            var secondTitle = UnicodeUtil.replaceSpecialCharacter(victimDetailContent.SelectSingleNode("//div[@class='blockbody'] //p[@class='real-name']").InnerText.Trim());
            var alias = UnicodeUtil.convertToAlias(title + ((secondTitle != "") ? ( "-" + secondTitle) : ""));
            int isMultipleEpisode = (currentMovieInfo.IndexOf("tập") != -1 || currentMovieInfo.IndexOf("/") != -1) ? 1 : 0;
            var publishYear = 0;
            try { publishYear = Int32.Parse(dd[dd.Count - 2].InnerText.Trim()); } catch (Exception ex) { Console.Write(ex.Message); }

            movie newMovie = new movie()
            {
                name1 = title,
                name2 = secondTitle,
                alias = alias,
                isMultiEpisode = isMultipleEpisode,
                dateUpdate = DateTime.Now,
                publishYear = publishYear,
                name1CheckField = buildNameCheckField(title),
                name2CheckField = buildNameCheckField(secondTitle),
                isPublic = 1
            };
            return newMovie;
        }
        public override victim getVictim(HtmlNode victimDetailContent)
        {
            if (checkIgnoreMovie(victimDetailContent)) return null;
            var dd = victimDetailContent.SelectNodes("//div[@class='dinfo'] //dd");
            var currentMovieInfo = dd[4].InnerText.Trim().ToLower().Replace("bản đẹp", "HD").ToLower();
            var smallImage = victimDetailContent.SelectSingleNode("//img[@itemprop='image']").Attributes["src"].Value;
            if (smallImage.IndexOf(victimDomainNoExt) != -1)
            {
                smallImage = UriUtil.getUrlNoDomain(smallImage);
            }
            var movieHref = UriUtil.checkAndAddDomain(victimDetailContent.SelectSingleNode("//a[@class='btn-see btn btn-info adspruce-streamlink']").Attributes["href"].Value, victimDomain, protocol);
            var totalEpisode = 1;
            var isContinue = 0;
            int isMultipleEpisode = (currentMovieInfo.IndexOf("tập") != -1 || currentMovieInfo.IndexOf("/") != -1) ? 1 : 0;
            int currentEpisode = 0;
            int viewed = Int32.Parse(dd[dd.Count - 3].InnerText.Trim().Replace(".", "").Trim());
            var rating = NumberUtil.GetDouble(victimDetailContent.SelectSingleNode("//span[@itemprop='ratingValue']").InnerText.Trim());
            var schedule = victimDetailContent.SelectSingleNode("//div[@class='show-time']");
            var priority = 0;
            try { priority = Int32.Parse(smallImage.Split('/')[3]);  } catch(Exception ex) { Console.Write(ex.Message); }
            
            if (isMultipleEpisode == 1)
            {
                var infoArray = currentMovieInfo.Split(' ');
                isContinue = 1;
                totalEpisode = 0;
                foreach (var info in infoArray)
                {
                    if (info.IndexOf("/") != -1)
                    {
                        var episodeInfo = info.Split('/');
                        try { currentEpisode = Int32.Parse(episodeInfo[0].Trim()); } catch (Exception ex) { Console.Write(ex.Message); }
                        try { totalEpisode = Int32.Parse(episodeInfo[1].Trim()); } catch (Exception ex) { Console.Write(ex.Message); }
                        if (currentEpisode == totalEpisode)
                        {
                            isContinue = 0;
                        }
                        break;
                    }
                    else if (IsNumber(info))
                    {
                        currentEpisode = Int32.Parse(info);
                        break;
                    }
                }
            }
            var description = victimDetailContent.SelectSingleNode("//div[@class='tabs-content']");
            description.InnerHtml = removeTextNodes(description, "h3");
            enhanceDescription(description);

            var playListHtml = getPlayListHtml(movieHref);
            if (playListHtml == null) return null;
            victim vtm = new victim ()
            {
                name = victimDomain,
                movieHref = movieHref,
                thumb = CryptoUtil.encrypt(smallImage.Replace("/150/", "/300/")),
                poster = CryptoUtil.encrypt(smallImage.Replace("/300/", "/600/")),
                smallImage = CryptoUtil.encrypt(smallImage),
                description = description.InnerHtml,
                currentEpisode = currentEpisode,
                totalEpisode = totalEpisode,
                duration = dd[dd.Count - 4].InnerText.Trim().Replace(" / tập", "").Trim(),
                quality = getQuality(currentMovieInfo),
                hasSub = (currentMovieInfo.IndexOf("vietsub") != -1) ? 1 : 0,
                hasDubbing = (currentMovieInfo.IndexOf("lồng tiếng") != -1) ? 1 : 0,
                hasTrans = (currentMovieInfo.IndexOf("thuyết minh") != -1) ? 1 : 0,
                isContinue = isContinue,
                dateUpdate = DateTime.Now,
                updateState = 1,
                viewed = viewed,
                rating = rating,
                schedule = (schedule != null) ? replaceDomain(schedule.InnerHtml) : "",
                victimTypeNo = victimTypeNo,
                priority = priority,
                isTrailer = isTrailer(victimDetailContent) ? 1 : 0,
                playListHtml = playListHtml[0],
                playListHtmlBk = playListHtml[1]
            };
            return vtm;
        }

        public override ICollection<category> getCategories(HtmlNode victimDetailContent)
        {
            string[] ignoreCats = { "long-tieng", "thuyet-minh", "han-quoc", "movie-trailers", "" };
            var dd = victimDetailContent.SelectNodes("//div[@class='dinfo'] //dd");
            var genres = dd[2].InnerText.Trim().Split(',');
            ICollection<category> cats = new HashSet<category>();
            foreach(var genre in genres)
            {
                try
                {
                    string genreName = UnicodeUtil.uniformCatName(genre.Trim());
                    var alias = UnicodeUtil.convertToAlias(genreName);
                    if (ignoreCats.Contains(alias.Replace("phim-", ""))) continue;
                    var newEntity = new category() { name = genreName, alias = alias };
                    if (categoryExisted(genreName))
                    {
                        newEntity = findCategoryByName(genreName);
                        setModified(newEntity);
                    }
                    cats.Add(newEntity);
                }
                catch
                {
                    continue;
                }
               
            }
            return cats;
        }

        public override ICollection<actor> getActors(HtmlNode victimDetailContent)
        {
            var dd = victimDetailContent.SelectNodes("//div[@class='dinfo'] //dd");
            string[] actors = dd[1].InnerText.Trim().Split(',');
            ICollection<actor> acts = new HashSet<actor>();
            
            foreach (var actor in actors)
            {
                try {
                    string actorName = actor.Trim();
                    if (actorName == "") continue;
                    var alias = UnicodeUtil.convertToAlias(actorName);
                    var newEntity = new actor() { name = actorName, alias = alias };
                    if (actorExisted(actorName))
                    {
                        newEntity = findActorByName(actorName);
                        setModified(newEntity);
                    }
                    acts.Add(newEntity);
                }
                catch
                {
                    continue;
                }
            }
            return acts;
        }

        public override ICollection<director> getDirectors(HtmlNode victimDetailContent)
        {
            var dd = victimDetailContent.SelectNodes("//div[@class='dinfo'] //dd");
            var directors = dd[0].InnerText.Trim().Split(',');
            ICollection<director> dirs = new HashSet<director>();
            foreach (var director in directors)
            {
                try
                {
                    string directorName = director.Trim();
                    if (directorName == "") continue;
                    var alias = UnicodeUtil.convertToAlias(directorName);
                    var newEntity = new director() { name = directorName, alias = alias };
                    if (directorExisted(directorName))
                    {
                        newEntity = findDirectorByName(directorName);
                        setModified(newEntity);
                    }
                    dirs.Add(newEntity);
                }
                catch
                {
                    continue;
                }
                
            }
            return dirs;
        }

        public override ICollection<country> getCountries(HtmlNode victimDetailContent)
        {
            var dd = victimDetailContent.SelectNodes("//div[@class='dinfo'] //dd");
            var countries = dd[dd.Count - 1].InnerText.Trim().Split(',');
            ICollection<country> couns = new HashSet<country>();
            foreach (var country in countries)
            {
                try
                {
                    string countryName = UnicodeUtil.uniformCountryName(country.Trim());
                    if (countryName == "") continue;
                    var alias = UnicodeUtil.convertToAlias(countryName);
                    var newEntity = new country() { name = countryName, alias = alias };
                    if (countryExisted(countryName))
                    {
                        newEntity = findCountryByName(countryName);
                        setModified(newEntity);
                    }
                    couns.Add(newEntity);
                }
                catch
                {
                    continue;
                }

            }
            return couns;
        }

        public override List<String> getPlayListHtml(String movieHref)
        {
            try
            {
                List<string> rs = new List<string>();
                var movieWatchPage = getRootNode(movieHref);
                string dataId = movieWatchPage.SelectSingleNode("//input[@id='film_id']").Attributes["value"].Value;
                HtmlNodeCollection episodeNodes = getListEpisodes(movieWatchPage);
                HtmlNodeCollection listServer = getListServer(movieWatchPage);
                
                if (episodeNodes == null)
                {
                    episodeNodes = buildEpisodeNodes(movieHref);
                }
                
                rs.Add(buildPlayListHtmlInternal(listServer, episodeNodes, dataId));
                //rs.Add(buildPlayListHtmlExternal(listServer, episodeNodes, dataId));
                rs.Add("");
                return rs;
            }
            catch(Exception ex)
            {
                Console.Write(ex.StackTrace);
                return null;
            }
           
        }

        private HtmlNodeCollection getListServer(HtmlNode movieWatchPage)
        {
            var serverItems = movieWatchPage.SelectNodes(".//div[@class='server-item']");
            var temp = WebParserUtil.convertDomStringToHtmlNode("<div></div>");
            foreach(var serverItem in serverItems)
            {
                temp.AppendChild(serverItem.SelectNodes("./div")[0].SelectSingleNode(".//span"));
            }
            return temp.SelectNodes(".//span");
        }

        private HtmlNodeCollection getListEpisodes(HtmlNode movieWatchPage)
        {
            return movieWatchPage.SelectNodes(".//div[@id='list_episodes'] //a");
        }

        private HtmlNodeCollection buildEpisodeNodes(string movieHref)
        {
            return WebParserUtil.convertDomStringToHtmlNode("<div><a href='" + movieHref + "'>1</a></div>").SelectNodes(".//a");
        }

        private string buildPlayListHtmlExternal(HtmlNodeCollection listServer, HtmlNodeCollection episodeNodes, string dataId)
        {
            string rs = "";
            
            foreach(HtmlNode serverNode in listServer)
            {
                string dataServer = serverNode.Attributes["data-index"].Value;
                string dataType = serverNode.Attributes["data-type"].Value;
                rs += "<div data-id='" + dataId + "' class='server-item' name='" + serverNode.InnerText.Trim() + "'>";
                if (episodeNodes != null)
                {
                    foreach (HtmlNode episodeNode in episodeNodes)
                    {
                        //var hrefChunk = episodeNode.Attributes["href"].Value.Split('-');
                        //dataId = hrefChunk[hrefChunk.Length - 1].Split('_')[1].Split('.')[0].Replace("e", "");
                        rs += joinPlayListHtmlServerExternal(dataId, dataServer, dataType, episodeNode.InnerText.Trim());
                    }
                } else
                {
                    rs += joinPlayListHtmlServerExternal(dataId, dataServer, dataType, "1");
                }
                
                rs += "</div>";
            }
            return rs;
        }

        private string joinPlayListHtmlServerExternal(string dataId, string dataServer, string dataType, string episodeName)
        {
            string rs = "";
            string phimBatHuGetLinkPlayer = "http://phimbathu.com/ajax/getLinkPlayer/id/" + dataId + "/m/0/server/" + dataServer + "/type/" + dataType;
            string moviePlayerInfo = getEpisodePlayerInfo(phimBatHuGetLinkPlayer);
            rs += "<div moviePlayerInfoLink='" + phimBatHuGetLinkPlayer + "' class='episode-item' name='" + episodeName + "'>";
            rs += "var playerSetting=" + moviePlayerInfo;
            rs += "</div>";
            return rs;
        }

        private string buildPlayListHtmlInternal(HtmlNodeCollection listServer, HtmlNodeCollection episodeNodes, string dataId)
        {
            try
            {
                logForm.append("Begin buildPlayListHtmlInternal: listServer=" + listServer.Count + " | episodeNodes = " + episodeNodes.Count);
                string rs = "";
                int serverIndex = 0;
                foreach (HtmlNode serverNode in listServer)
                {
                    rs += "<div data-id='" + dataId + "' class='server-item' name='" + serverNode.InnerText.Trim() + "'>";
                    if (episodeNodes != null)
                    {
                        foreach (HtmlNode episodeNode in episodeNodes)
                        {
                            rs += joinPlayListHtmlServerInternal(serverIndex, dataId, episodeNode);
                        }
                    }
                    rs += "</div>";
                    serverIndex++;
                }
                logForm.append("Finished buildPlayListHtmlInternal: listServer=" + listServer.Count + " | episodeNodes = " + episodeNodes.Count);
                return rs;
            }
            catch (Exception e)
            {
                logForm.append(e.Message);
                return "";
            }
        }

        private string joinPlayListHtmlServerInternal(int serverIndex, string dataId, HtmlNode episodeNode)
        {
            logForm.append("joinPlayListHtmlServerInternal: " + serverIndex);
            string rs = "";
            JArray episodeQualitiesInfo = null;
            string phimBatHuGetLinkPlayer = episodeNode.Attributes["href"].Value;
            try
            {
                var episodePlayerInfoList = getEpisodePlayerInfoAndEnhance(phimBatHuGetLinkPlayer);
                if (isCurrentServerHasCurrentEpisode(serverIndex, episodePlayerInfoList))
                {
                    var currentQualitiesList = (JArray)episodePlayerInfoList[serverIndex];
                    episodeQualitiesInfo = getEpisodeQualitiesInfo(currentQualitiesList);
                    var episodeQualitiesInfoBk = String.Join(",", getEpisodeQualitiesInfoBk(currentQualitiesList));
                    rs += "<div moviePlayerInfoLink='" + phimBatHuGetLinkPlayer + "' class='episode-item' name='" + episodeNode.InnerText.Trim() + "'>";
                    rs += (episodeQualitiesInfo != null)
                        ? "var playerSetting=[" +  episodeQualitiesInfo + "," + episodeQualitiesInfoBk + "]</div>"
                        : "var playerSetting=[" + episodeQualitiesInfo + "]</div>";
                } 
            } catch(Exception e)
            {
                logForm.append(e.StackTrace);
            }
            return rs;
        }

        private List<JArray> getEpisodeQualitiesInfoBk(JArray qualitiesList)
        {
            List<JArray> rs = new List<JArray>();
            if (qualitiesList.Children().Count() > 2)
            {
                var serverIndex = 0;
                var ignoreIndex = getIndexFirstQualityByServerName(qualitiesList, "gd");
                foreach (var quality in qualitiesList)
                {
                    if (ignoreIndex != serverIndex)
                    {
                        rs.Add((JArray)quality["links"]);
                    }
                    serverIndex++;
                }
            }
            return rs;
        }

        private int? getIndexFirstQualityByServerName(JArray qualitiesList, string serverName)
        {
            int index = 0;
            foreach (var quality in qualitiesList)
            {
                if (((string)quality["server"]).ToLower().Equals(serverName))
                {
                    return index;
                }
                index++;
            }
            return null;
        }

        private JArray getEpisodeQualitiesInfo(JArray qualitiesList)
        {
            var episodeQualitiesInfo = getEpisodeQualitiesInfoByServerName(qualitiesList, "gd");
            return (episodeQualitiesInfo == null) ? (JArray)qualitiesList[0]["links"]: episodeQualitiesInfo;
        }

        private JArray getEpisodeQualitiesInfoByServerName(JArray qualitiesList, string serverName)
        {
            foreach(var quality in qualitiesList)
            {
                if (((string)quality["server"]).ToLower().Equals(serverName))
                {
                    return (JArray)quality["links"];
                }
            }
            return null;
        }

        private bool isCurrentServerHasCurrentEpisode(int serverIndex, List<JArray> episodePlayerInfoList)
        {
            if ((serverIndex + 1) <= episodePlayerInfoList.Count())
            {
                return true;
            }
            return false;
        }

        private List<JArray> getEpisodePlayerInfoAndEnhance(string episodeHref)
        {
            List<JArray> rs = new List<JArray>();
            logForm.append("getEpisodePlayerInfoAndEnhance: " + episodeHref);
            string moviePlayerInfo = WebParserUtil.selectSingleNode(episodeHref, ".//div[@class='left-content-player ']").SelectSingleNode(".//script").InnerText.Trim();
            var parts = moviePlayerInfo.Split(new string[] { "$(document)" }, StringSplitOptions.None);
            var playerSettingString = parts[0].Replace("var playerSetting = ", "");
            playerSettingString = playerSettingString.Substring(0, playerSettingString.LastIndexOf(";"));
            JObject json = JObject.Parse(playerSettingString);
            foreach (var x in json)
            {
                if (x.Value.GetType() != typeof(JArray)) break;
                if (((JArray)x.Value).Children().Count() > 0)
                    rs.Add((JArray)x.Value);
            }
            return rs;
        }

        public override ICollection<episode> getAndAddEpisodesToVictim(victim vtm)
        {
            var victimWatchContent = getRootNode(UriUtil.checkAndAddDomain(vtm.movieHref, victimDomain, protocol));
            var groups = victimWatchContent.SelectNodes("//ul[@class='choose-server'] //a");
            var groupMap = new List<string>();
            groupMap.Add("default~" + vtm.movieHref);
            
            if (groups != null)
            {
                groupMap.Clear();
                foreach (var group in groups)
                {
                    groupMap.Add(group.InnerText.Trim() + "~" + group.Attributes["href"].Value);
                }
            }
            
            foreach (var g in groupMap)
            {
                var ginfo = g.Split('~');
                var href = UriUtil.checkAndAddDomain(ginfo[1], victimDomain, protocol);
                var episodeNodes = getRootNode(href).SelectNodes(".//div[@class='list-episode'] //a");
                if (episodeNodes != null)
                {
                    foreach (var episodeNode in episodeNodes)
                    {
                        vtm.episodes.Add(newEpisodeInstance(episodeNode.InnerText.Trim(), vtm.victimId, ginfo[0], episodeNode.Attributes["href"].Value));
                    }
                }
                else
                {
                    vtm.episodes.Add(newEpisodeInstance("1", vtm.victimId, g.Split('~')[0], vtm.movieHref));
                }
                
            }
            return vtm.episodes;
        }

        public override ICollection<episode> getEpisodes(string movieHref, bool excludeGroup)
        {
            victim vtm = new victim();
            vtm.movieHref = movieHref;
            var victimWatchContent = getRootNode(UriUtil.checkAndAddDomain(vtm.movieHref, victimDomain, protocol));
            var groupMap = new List<string>();
            groupMap.Add("default~" + vtm.movieHref);
            if (!excludeGroup)
            {
                var groups = victimWatchContent.SelectNodes("//ul[@class='choose-server'] //a");
                if (groups != null)
                {
                    groupMap.Clear();
                    foreach (var group in groups)
                    {
                        groupMap.Add(group.InnerText.Trim() + "~" + group.Attributes["href"].Value);
                    }
                }
            }
            

            foreach (var g in groupMap)
            {
                var ginfo = g.Split('~');
                var href = UriUtil.checkAndAddDomain(ginfo[1], victimDomain, protocol);
                var episodeNodes = getRootNode(href).SelectNodes(".//div[@class='list-episode'] //a");
                if (episodeNodes != null)
                {
                    foreach (var episodeNode in episodeNodes)
                    {
                        vtm.episodes.Add(newEpisodeInstance(episodeNode.InnerText.Trim(), vtm.victimId, ginfo[0], episodeNode.Attributes["href"].Value));
                    }
                }
                else
                {
                    vtm.episodes.Add(newEpisodeInstance("1", vtm.victimId, g.Split('~')[0], vtm.movieHref));
                }

            }
            return vtm.episodes;
        }

        public override void filterImages(HtmlNodeCollection images)
        {
            if (images == null) return;
            foreach (HtmlNode image in images)
            {
                if (image.Attributes["src"].Value.IndexOf("media.") == -1)
                {
                    image.Remove();
                }
            }
        }

        public override bool isTrailer(HtmlNode victimDetailContent)
        {
            var dd = victimDetailContent.SelectNodes("//div[@class='dinfo'] //dd");
            var genres = dd[2].InnerText.Trim();
            var currentMovieInfo = dd[4].InnerText.Trim().ToLower();
            return (genres.ToLower().IndexOf("trailer") != -1 || currentMovieInfo.ToLower().IndexOf("trailer") != -1 || currentMovieInfo.ToLower().IndexOf("preview") != -1);
        }

        public override void checkToAddOrUpdateProposeMovies(movie movieItemInfo)
        {
            var homePageContent = getHomePageContent();
            if (homePageContent == null) return;
            var proposeMovieItems = homePageContent.SelectNodes(".//ul[@id='film_hot'] //a");
            if (proposeMovieItems == null) return;
            if (isExistPositionType(movieItemInfo, proposeMovieItems, ".//span[@class='title']"))
            {
                home_page_position proposeMovie = (home_page_position)movieItemInfo.home_page_position.FirstOrDefault(a => a.positionType == 0);
                if (proposeMovie == null)
                {
                    movieItemInfo.home_page_position.Add(new home_page_position()
                    {
                        positionType = 0,
                        movieId = movieItemInfo.movieId,
                        description = "ProposeMovie",
                        dateUpdate = DateTime.Now
                    });
                }
                else
                {
                    proposeMovie.dateUpdate = DateTime.Now;
                }

            }
        }

        public override void checkToAddOrUpdateOneEpisodeHostWeekMovies(movie movieItemInfo)
        {
          
        }

        public override void checkToAddOrUpdateMultiEpisodeHostWeekMovies(movie movieItemInfo)
        {
            var homePageContent = getHomePageContent();
            var weekMultiEpisodeMovieItems = homePageContent.SelectNodes(".//div[@class='most-view block'] //ul[@class='list-film'] //a");
            if (weekMultiEpisodeMovieItems == null) return;
            if (isExistPositionType(movieItemInfo, weekMultiEpisodeMovieItems, ".//span"))
            {
                home_page_position weekMultiEpisodeMovie = (home_page_position)movieItemInfo.home_page_position.FirstOrDefault(a => a.positionType == 2);
                if (weekMultiEpisodeMovie == null)
                {
                    movieItemInfo.home_page_position.Add(new home_page_position()
                    {
                        positionType = 2,
                        movieId = movieItemInfo.movieId,
                        description = "MultiEpisodeHostWeekMovie",
                        dateUpdate = DateTime.Now
                    });
                }
                else
                {
                    weekMultiEpisodeMovie.dateUpdate = DateTime.Now;
                }

            }
        }

        public override void checkToAddOrUpdateCinemaMovies(movie movieItemInfo)
        {
           
        }

        public override void checkToAddOrUpdateOneEpisodeLastestUpdatedMovies(movie movieItemInfo)
        {
           
        }

        public override void checkToAddOrUpdateMultiEpisodeLastestUpdatedMovies(movie movieItemInfo)
        {
            var homePageContent = getHomePageContent();
            var multiEpisodeLastestUpdatedMovieItems = homePageContent.SelectNodes(".//div[@class='left-content'] //div[@class='list-films film-new']")[0].SelectNodes(".//ul //a");
            if (multiEpisodeLastestUpdatedMovieItems == null) return;
            if (isExistPositionType(movieItemInfo, multiEpisodeLastestUpdatedMovieItems, ".//div[@class='name']"))
            {
                home_page_position multiEpisodeLastestUpdatedMovie = (home_page_position)movieItemInfo.home_page_position.FirstOrDefault(a => a.positionType == 5);
                if (multiEpisodeLastestUpdatedMovie == null)
                {
                    movieItemInfo.home_page_position.Add(new home_page_position()
                    {
                        positionType = 5,
                        movieId = movieItemInfo.movieId,
                        description = "MultiEpisodeLastestUpdatedMovie",
                        dateUpdate = DateTime.Now
                    });
                }
                else
                {
                    multiEpisodeLastestUpdatedMovie.dateUpdate = DateTime.Now;
                }

            }
        }

        public override void checkToAddOrUpdateCartoonMovies(movie movieItemInfo)
        {
          
        }
    }
}
