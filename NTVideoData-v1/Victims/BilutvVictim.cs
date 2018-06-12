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
using NTVideoData_v1;

namespace NTVideoData.Victims
{
    public class BilutvVictim : BaseVictim
    {
        public BilutvVictim()
        {
            victimDomain = "bilutv.com";
            victimDomainNoExt = victimDomain.Split('.')[0];
            victimMediaDomain = "media." + victimDomain;
            victimTypeNo = 1;
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
            var ul = WebParserUtil.selectNodes(pageUrl, "//div[@class='left-content'] //ul");
            return ul[0].SelectNodes(".//a");
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
        //    if (mv == null || movieExistedWithVictim(mv, victimDomain)) return null;
        //    victim vtm = getVictim(victimDetailContent);
        //    if (vtm == null) return null;
        //    vtm.name = victimDomain;
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
            HtmlNodeCollection dd = victimDetailContent.SelectNodes(".//ul[@class='meta-data'] //li");
            if (dd == null) return true;
            var countries = string.Join(",", convertNodesToArray(dd[dd.Count - 7].SelectNodes("./a")));
            if (countries.IndexOf("việt nam") != -1) return true;
            return false;
        }

        public override bool is404(HtmlNode victimDetailContent)
        {
            if (victimDetailContent == null) return true;
            HtmlNodeCollection dd = victimDetailContent.SelectNodes("//ul[@class='meta-data'] //li");
            return (dd == null);
        }

        public override movie getMovie(HtmlNode victimDetailContent)
        {
            HtmlNodeCollection dd = victimDetailContent.SelectNodes("//ul[@class='meta-data'] //li");
            var currentMovieInfo = dd[0].SelectSingleNode("./strong").InnerText.Trim().ToLower().Replace("bản đẹp", "HD").ToLower();
            var title = UnicodeUtil.replaceSpecialCharacter(victimDetailContent.SelectSingleNode("//div[@class='left-content'] //h1[@class='name']").InnerText.Trim());
            var secondTitle = UnicodeUtil.replaceSpecialCharacter(victimDetailContent.SelectSingleNode("//div[@class='left-content'] //h2[@class='real-name']").InnerText.Trim());
            var alias = UnicodeUtil.convertToAlias(title + ((secondTitle != "") ? ( "-" + secondTitle) : ""));
            int isMultipleEpisode = (currentMovieInfo.IndexOf("tập") != -1 || currentMovieInfo.IndexOf("/") != -1) ? 1 : 0;
            var publishYear = 0;
            try {
                publishYear = Int32.Parse(dd[dd.Count - 4].SelectSingleNode("./span").InnerText.Trim().Replace("Năm xuất bản:", "").Trim());
            } catch (Exception ex) { Console.Write(ex.Message); }

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
            try
            {
                if (checkIgnoreMovie(victimDetailContent)) return null;
                var dd = victimDetailContent.SelectNodes("//ul[@class='meta-data'] //li");
                var currentMovieInfo = dd[0].SelectSingleNode("./strong").InnerText.Trim().ToLower().Replace("bản đẹp", "HD").ToLower();
                var smallImage = victimDetailContent.SelectSingleNode("//div[@class='poster'] //img").Attributes["src"].Value;
                if (smallImage.IndexOf(victimDomainNoExt) != -1)
                {
                    smallImage = UriUtil.getUrlNoDomain(smallImage);
                }
                var movieHref = UriUtil.checkAndAddDomain(victimDetailContent.SelectSingleNode(".//a[@class='btn-see btn btn-danger']").Attributes["href"].Value, victimDomain, protocol);
                var totalEpisode = 1;
                var isContinue = 0;
                var duration = dd[dd.Count - 6].SelectSingleNode("./span").InnerText.Trim();
                int isMultipleEpisode = (duration.IndexOf("tập") != -1 || duration.IndexOf("/") != -1) ? 1 : 0;
                int currentEpisode = 0;
                int viewed = 999;
                try { viewed = Int32.Parse(dd[dd.Count - 5].SelectSingleNode("./span").InnerText.Trim().Replace(".", "").Trim()); } catch (Exception ex) { Console.Write(ex.Message); }
                var rating = 9D;
                try { rating = NumberUtil.GetDouble(victimDetailContent.SelectSingleNode("//span[@itempop='ratingValue']").InnerText.Trim()); } catch (Exception ex) { Console.Write(ex.Message); }
                var schedule = victimDetailContent.SelectSingleNode("//div[@class='broadcast']");
                var priority = Int32.Parse(smallImage.Split('/')[3]);
               
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
                var description = victimDetailContent.SelectSingleNode("//div[@class='film-content']");
                description.InnerHtml = removeTextNodes(description, "h3");
                enhanceDescription(description);
                var playListHtml = getPlayListHtml(movieHref);
                if (playListHtml == null) return null;
                victim vtm = new victim()
                {
                    name = victimDomain,
                    movieHref = movieHref,
                    thumb = CryptoUtil.encrypt(smallImage),
                    poster = CryptoUtil.encrypt(smallImage.Replace("/240/", "/600/")),
                    smallImage = CryptoUtil.encrypt(smallImage.Replace("/240/", "/150/")),
                    description = description.InnerHtml,
                    currentEpisode = currentEpisode,
                    totalEpisode = totalEpisode,
                    duration = duration.Replace(" / tập", "").Trim(),
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
            catch(Exception ex)
            {
                Console.Write(ex.InnerException);
                return null;
            }
        }

        public override ICollection<category> getCategories(HtmlNode victimDetailContent)
        {
            string[] ignoreCats = { "long-tieng", "thuyet-minh", "han-quoc", "movie-trailers", "" };
            var dd = victimDetailContent.SelectNodes("//ul[@class='meta-data'] //li");
            var genres = convertNodesToArray(dd[dd.Count - 8].SelectNodes("./a"));
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
            var dd = victimDetailContent.SelectNodes("//ul[@class='meta-data'] //li");
            string[] actors = convertNodesToArray(dd[dd.Count - 9].SelectNodes("./a"));
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
            var dd = victimDetailContent.SelectNodes("//ul[@class='meta-data'] //li");
            var directors = convertNodesToArray(dd[dd.Count - 10].SelectNodes("./a"));
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
            var dd = victimDetailContent.SelectNodes("//ul[@class='meta-data'] //li");
            var countries = convertNodesToArray(dd[dd.Count - 7].SelectNodes("./a"));
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
            List<string> rs = new List<string>();
            var movieWatchPage = getRootNode(movieHref);
            HtmlNode playListHtmlNode = movieWatchPage.SelectSingleNode("//div[@class='list-server']");
            rs.Add((playListHtmlNode != null) ? playListHtmlNode.OuterHtml : movieWatchPage.SelectSingleNode("//div[@id='block-player']").PreviousSibling.Attributes["src"].Value);
            rs.Add("");
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
            HtmlNodeCollection dd = victimDetailContent.SelectNodes("//ul[@class='meta-data'] //li");
            var currentMovieInfo = dd[0].SelectSingleNode("./strong").InnerText.Trim();
            return (currentMovieInfo.ToLower().IndexOf("trailer") != -1);
        }

        public override void checkToAddOrUpdateProposeMovies(movie movieItemInfo)
        {

        }

        public override void checkToAddOrUpdateOneEpisodeHostWeekMovies(movie movieItemInfo)
        {

        }

        public override void checkToAddOrUpdateMultiEpisodeHostWeekMovies(movie movieItemInfo)
        {

        }

        public override void checkToAddOrUpdateCinemaMovies(movie movieItemInfo)
        {

        }

        public override void checkToAddOrUpdateOneEpisodeLastestUpdatedMovies(movie movieItemInfo)
        {

        }

        public override void checkToAddOrUpdateMultiEpisodeLastestUpdatedMovies(movie movieItemInfo)
        {

        }

        public override void checkToAddOrUpdateCartoonMovies(movie movieItemInfo)
        {

        }
    }
}
