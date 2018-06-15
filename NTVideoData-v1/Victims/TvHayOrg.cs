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
    public class TvHayOrg : BaseVictim
    {
        public TvHayOrg()
        {
            victimDomain = "tvhay.org";
            victimDomainNoExt = victimDomain.Split('.')[0];
            victimMediaDomain = "";
            victimTypeNo = 3;
            victimMediaController += victimTypeNo + "/";
            destToGetUrls[0] = protocol + victimDomain + "/phim-moi";
            destToGetUrls[1] = protocol + victimDomain + "/phim-le";
            destToGetUrls[2] = protocol + victimDomain + "/phim-bo";
            destToGetUrls[3] = protocol + victimDomain + "/phim-moi";
            pageIndexParam = "page";
        }

        public override HtmlNodeCollection getItemsInPage(int pageIndex)
        {
            var getUrl = currentDestToGetUrl + "/" + pageIndexParam + "/" + pageIndex;
            var ul = WebParserUtil.selectNodes(getUrl, "//ul[@class='list-film']");
            return ul[0].SelectNodes(".//li");
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
                    string href = items[j].SelectNodes(".//a")[0].Attributes["href"].Value;
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
            HtmlNodeCollection dd = victimDetailContent.SelectNodes(".//div[@class='blockbody']");
            return (dd == null);
        }

        public override bool is404(HtmlNode victimDetailContent)
        {
            if (victimDetailContent == null) return true;
            HtmlNodeCollection dd = victimDetailContent.SelectNodes(".//div[@class='blockbody']");
            return (dd == null);
        }

        public override movie getMovie(HtmlNode victimDetailContent)
        {
            HtmlNode blockbody = victimDetailContent.SelectSingleNode(".//div[@class='blockbody']");
            HtmlNodeCollection dd = blockbody.SelectNodes("//dd");
            var currentMovieInfo = dd[0].InnerText.Trim().ToLower().Replace("bản đẹp", "HD").ToLower();
            var title = UnicodeUtil.replaceSpecialCharacter(blockbody.SelectSingleNode(".//h1").InnerText).Trim();
            var secondTitle = UnicodeUtil.replaceSpecialCharacter(blockbody.SelectSingleNode(".//h3").InnerText.Trim());
            var alias = UnicodeUtil.convertToAlias(title + ((secondTitle != "") ? ( "-" + secondTitle) : ""));
            int isMultipleEpisode = (currentMovieInfo.IndexOf("tập") != -1 || currentMovieInfo.IndexOf("/") != -1) ? 1 : 0;
            var publishYear = 0;
            try {
                publishYear = int.Parse(title.Split('(')[1].Trim());
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
                HtmlNode blockbody = victimDetailContent.SelectSingleNode(".//div[@class='blockbody']");
                HtmlNodeCollection dd = blockbody.SelectNodes("//dd");
                var currentMovieInfo = dd[0].InnerText.Trim().ToLower().Replace("bản đẹp", "HD").ToLower();
                var smallImage = blockbody.SelectSingleNode(".//div[@class='poster'] //img").Attributes["src"].Value;
                if (smallImage.IndexOf(victimDomainNoExt) != -1)
                {
                    smallImage = UriUtil.getUrlNoDomain(smallImage);
                }
                var movieHref = UriUtil.checkAndAddDomain(blockbody.SelectSingleNode(".//a[@class='btn-watch']").Attributes["href"].Value, victimDomain, protocol);
                var totalEpisode = 1;
                var isContinue = 0;
                var duration = dd[dd.Count - 2].InnerText.Trim();
                int isMultipleEpisode = (duration.ToLower().IndexOf("tập") != -1 || duration.IndexOf("/") != -1) ? 1 : 0;
                int currentEpisode = 0;
                int viewed = 999;
                try { viewed = Int32.Parse(dd[dd.Count - 1].InnerText.Trim().Replace(".", "").Trim()); } catch (Exception ex) { Console.Write(ex.Message); }
                var rating = 9D;
                //try { rating = NumberUtil.GetDouble(victimDetailContent.SelectSingleNode("//span[@itempop='ratingValue']").InnerText.Trim()); } catch (Exception ex) { Console.Write(ex.Message); }
                var schedule = "";
                //var schedule = victimDetailContent.SelectSingleNode("//div[@class='broadcast']");
                var priority = 1;
                //var priority = Int32.Parse(smallImage.Split('/')[3]);
               
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
                var description = victimDetailContent.SelectSingleNode("//div[@class='tabs-content'] //p");
                description.InnerHtml = removeTextNodes(description, "h3");
                //enhanceDescription(description);
                var playListHtml = getPlayListHtml(movieHref);
                if (playListHtml == null) return null;
                victim vtm = new victim()
                {
                    name = victimDomain,
                    movieHref = movieHref,
                    thumb = smallImage,
                    poster = smallImage,
                    smallImage = smallImage,
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
                    schedule = (schedule != null) ? replaceDomain(schedule) : "",
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
            HtmlNode blockbody = victimDetailContent.SelectSingleNode(".//div[@class='blockbody']");
            HtmlNodeCollection dd = blockbody.SelectNodes("//dd");
            var genres = dd[dd.Count - 4].InnerText.Split(',');
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
            HtmlNode blockbody = victimDetailContent.SelectSingleNode(".//div[@class='blockbody']");
            HtmlNodeCollection dd = blockbody.SelectNodes("//dd");
            string[] actors = dd[dd.Count - 5].InnerText.Split(',');
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
            HtmlNode blockbody = victimDetailContent.SelectSingleNode(".//div[@class='blockbody']");
            HtmlNodeCollection dd = blockbody.SelectNodes("//dd");
            var directors = dd[dd.Count - 6].InnerText.Split(',');
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
            HtmlNode blockbody = victimDetailContent.SelectSingleNode(".//div[@class='blockbody']");
            HtmlNodeCollection dd = blockbody.SelectNodes("//dd");
            var countries = dd[dd.Count - 3].InnerText.Split(',');
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
            HtmlNodeCollection playListHtmlNodes = movieWatchPage.SelectNodes("//div[@class='serverlist'] //ul[@class='episodelist']");
            rs.Add((playListHtmlNodes.Count > 0) ? playListHtmlNodes[0].OuterHtml : movieHref);
            rs.Add((playListHtmlNodes.Count > 1) ? playListHtmlNodes[1].OuterHtml : movieHref);
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
            return false;
        }

        public override void checkToAddOrUpdateProposeMovies(movie movieItemInfo)
        {
            var homePageContent = getHomePageContent();
            if (homePageContent == null) return;
            var proposeMovieItems = homePageContent.SelectNodes(".//ul[@class='listfilm overview'] //li");
            if (proposeMovieItems == null) return;
            if (isExistPositionType(movieItemInfo, proposeMovieItems, ".//div[@class='name'] //a"))
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
            var homePageContent = getHomePageContent();
            var weekMultiEpisodeMovieItems = homePageContent.SelectNodes(".//div[@class='tab phim-le hide'] //ul[@class='list-film'] //li");
            if (weekMultiEpisodeMovieItems == null) return;
            if (isExistPositionType(movieItemInfo, weekMultiEpisodeMovieItems, ".//div[@class='name'] //a"))
            {
                home_page_position weekMultiEpisodeMovie = (home_page_position)movieItemInfo.home_page_position.FirstOrDefault(a => a.positionType == 2);
                if (weekMultiEpisodeMovie == null)
                {
                    movieItemInfo.home_page_position.Add(new home_page_position()
                    {
                        positionType = 1,
                        movieId = movieItemInfo.movieId,
                        description = "OneEpisodeHostWeekMovie",
                        dateUpdate = DateTime.Now
                    });
                }
                else
                {
                    weekMultiEpisodeMovie.dateUpdate = DateTime.Now;
                }

            }
        }

        public override void checkToAddOrUpdateMultiEpisodeHostWeekMovies(movie movieItemInfo)
        {
            var homePageContent = getHomePageContent();
            var weekMultiEpisodeMovieItems = homePageContent.SelectNodes(".//div[@class='tab phim-bo hide'] //ul[@class='list-film'] //li");
            if (weekMultiEpisodeMovieItems == null) return;
            if (isExistPositionType(movieItemInfo, weekMultiEpisodeMovieItems, ".//div[@class='name'] //a"))
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
            var homePageContent = getHomePageContent();
            var weekMultiEpisodeMovieItems = homePageContent.SelectNodes(".//div[@class='tab phim-le hide'] //ul[@class='list-film'] //li");
            if (weekMultiEpisodeMovieItems == null) return;
            if (isExistPositionType(movieItemInfo, weekMultiEpisodeMovieItems, ".//div[@class='name'] //a"))
            {
                home_page_position weekMultiEpisodeMovie = (home_page_position)movieItemInfo.home_page_position.FirstOrDefault(a => a.positionType == 2);
                if (weekMultiEpisodeMovie == null)
                {
                    movieItemInfo.home_page_position.Add(new home_page_position()
                    {
                        positionType = 3,
                        movieId = movieItemInfo.movieId,
                        description = "cinemaMovies",
                        dateUpdate = DateTime.Now
                    });
                }
                else
                {
                    weekMultiEpisodeMovie.dateUpdate = DateTime.Now;
                }

            }
        }

        public override void checkToAddOrUpdateOneEpisodeLastestUpdatedMovies(movie movieItemInfo)
        {
            var homePageContent = getHomePageContent();
            var weekMultiEpisodeMovieItems = homePageContent.SelectNodes(".//div[@class='tab phim-le hide'] //ul[@class='list-film'] //li");
            if (weekMultiEpisodeMovieItems == null) return;
            if (isExistPositionType(movieItemInfo, weekMultiEpisodeMovieItems, ".//div[@class='name'] //a"))
            {
                home_page_position weekMultiEpisodeMovie = (home_page_position)movieItemInfo.home_page_position.FirstOrDefault(a => a.positionType == 2);
                if (weekMultiEpisodeMovie == null)
                {
                    movieItemInfo.home_page_position.Add(new home_page_position()
                    {
                        positionType = 4,
                        movieId = movieItemInfo.movieId,
                        description = "OneEpisodeLastestUpdatedMovie",
                        dateUpdate = DateTime.Now
                    });
                }
                else
                {
                    weekMultiEpisodeMovie.dateUpdate = DateTime.Now;
                }

            }
        }

        public override void checkToAddOrUpdateMultiEpisodeLastestUpdatedMovies(movie movieItemInfo)
        {
            var homePageContent = getHomePageContent();
            var multiEpisodeLastestUpdatedMovieItems = homePageContent.SelectNodes(".//div[@class='tab phim-le hide'] //ul[@class='list-film'] //li");
            if (multiEpisodeLastestUpdatedMovieItems == null) return;
            if (isExistPositionType(movieItemInfo, multiEpisodeLastestUpdatedMovieItems, ".//div[@class='name'] //a"))
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
