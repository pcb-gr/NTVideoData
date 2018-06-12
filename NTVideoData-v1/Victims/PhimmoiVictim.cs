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
    public class PhimMoiVictim : BaseVictim
    {
        private List<HtmlNode> ddItems = null;
        public PhimMoiVictim()
        {
            victimDomain = "phimmoi.net";
            victimDomainNoExt = victimDomain.Split('.')[0];
            victimMediaDomain = "phimmoi.newsuncdn.com";
            victimTypeNo = 2;
            victimMediaController += victimTypeNo + "/";
            destToGetUrls[0] = protocol + victimDomain + "/trailer";
            destToGetUrls[1] = protocol + victimDomain + "/phim-le";
            destToGetUrls[2] = protocol + victimDomain + "/phim-bo";
            destToGetUrls[3] = protocol + victimDomain + "/film/filter?year=2017";
            pageIndexParam = "/page-";
        }

        public override HtmlNodeCollection getItemsInPage(int pageIndex)
        {
            string pageUrl = currentDestToGetUrl + pageIndexParam + (++pageIndex) + ".html";
            return WebParserUtil.selectNodes(pageUrl, "//ul[@class='list-movie'] //a");
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
                    string href = "/" + items[j].Attributes["href"].Value;
                    
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
            var dd = getDDItems();
            if (dd == null) return true;
            var countries = dd[2].InnerText.Trim().ToLower();
            if (countries.IndexOf("việt nam") != -1 || victimDetailContent.SelectSingleNode("//a[@id='btn-film-watch']") == null) return true;
            return false;

        }

        public override bool is404(HtmlNode victimDetailContent)
        {
            if (victimDetailContent == null) return true;
            this.victimDetailContent = victimDetailContent;
            var dd = getDDItemNodes();
            return (dd == null || victimDetailContent.SelectSingleNode("//a[@id='btn-film-watch']") == null);
        }

        public override movie getMovie(HtmlNode victimDetailContent)
        {
            var dd = getDDItems();
            var currentMovieInfo = dd[0].InnerText.Trim().ToLower().Replace("bản đẹp", "HD").ToLower();
            var title = UnicodeUtil.replaceSpecialCharacter(victimDetailContent.SelectSingleNode("//a[@class='title-1']").InnerText.Trim());
            var secondTitle = UnicodeUtil.replaceSpecialCharacter(victimDetailContent.SelectSingleNode("//span[@class='title-2']").InnerText.Trim());
            var alias = UnicodeUtil.convertToAlias(title + ((secondTitle != "") ? ( "-" + secondTitle) : ""));
            int isMultipleEpisode = (currentMovieInfo.IndexOf("tập") != -1 || currentMovieInfo.IndexOf("/") != -1) ? 1 : 0;
            var publishYear = 0;
            try { publishYear = Int32.Parse(dd[3].InnerText.Trim()); } catch (Exception ex) { Console.Write(ex.Message); }

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
            var dd = getDDItems ();
            var currentMovieInfo = dd[0].InnerText.Trim().ToLower().Replace("bản đẹp", "HD").ToLower();
            var smallImage = victimDetailContent.SelectSingleNode("//div[@class='movie-l-img'] //img").Attributes["src"].Value;
            if (smallImage.IndexOf(victimDomainNoExt) != -1)
            {
                smallImage = UriUtil.getUrlNoDomain(smallImage);
            }
            var movieHref = UriUtil.checkAndAddDomain(victimDetailContent.SelectSingleNode("//a[@id='btn-film-watch']").Attributes["href"].Value, victimDomain, protocol);
            var totalEpisode = 1;
            var isContinue = 0;
            int isMultipleEpisode = (currentMovieInfo.IndexOf("tập") != -1 || currentMovieInfo.IndexOf("/") != -1) ? 1 : 0;
            int currentEpisode = 0;
            int viewed = Int32.Parse(dd[dd.Count() - 1].InnerText.Trim().Replace(",", "").Trim());
            var rating = NumberUtil.GetDouble(victimDetailContent.SelectSingleNode("//div[@id='star']").Attributes["data-score"].Value);
            var schedule = victimDetailContent.SelectSingleNode("//div[@class='show-time']");
            var priority = Int32.Parse(smallImage.Split('/')[2]);

            if (isMultipleEpisode == 1)
            {
                var infoArray = currentMovieInfo.Replace("(", "").Replace(")", "").Split(' ');
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
            var description = victimDetailContent.SelectSingleNode("//div[@id='film-content']");
            description.InnerHtml = removeTextNodes(description, "h3");
            enhanceDescription(description);
            var caption = dd[6].InnerText.Trim().ToLower();
            var playListHtml = getPlayListHtml(movieHref);
            if (playListHtml == null) return null;
            victim vtm = new victim ()
            {
                name = victimDomain,
                movieHref = movieHref,
                thumb = CryptoUtil.encrypt(smallImage),
                poster = CryptoUtil.encrypt(smallImage.Replace("/medium/", "/large/")),
                smallImage = CryptoUtil.encrypt(smallImage.Replace("/medium/", "/small/")),
                description = description.InnerHtml,
                currentEpisode = currentEpisode,
                totalEpisode = totalEpisode,
                duration = dd[4].InnerText.Trim().Replace(" / tập", "").Trim(),
                quality = dd[5].InnerText.Trim(),
                hasSub = (caption.IndexOf("phụ đề") != -1) ? 1 : 0,
                hasDubbing = (caption.IndexOf("lồng tiếng") != -1) ? 1 : 0,
                hasTrans = (caption.IndexOf("thuyết minh") != -1) ? 1 : 0,
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
            string[] ignoreCats = { "long-tieng", "thuyet-minh", "han-quoc", "movie-trailers", "", "bo", "le", "bo-han-quoc", "hoi-hop-gay-can" };
            var dd = getDDItems ();
            var genres = dd[7].InnerText.Trim().Split(',');
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
            var actors = victimDetailContent.SelectNodes("//div[@class='block-actors'] //span[@class='actor-name-a']");
            ICollection<actor> acts = new HashSet<actor>();
            if (actors == null) return acts;
            foreach (var actor in actors)
            {
                try {
                    string actorName = actor.InnerText.Trim();
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
            var dd = getDDItems ();
            var directors = dd[1].InnerText.Trim().Split(',');
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

        public override List<String> getPlayListHtml(String movieHref)
        {
            try
            {
                List<string> rs = new List<string>();
                var movieWatchPage = getRootNode(movieHref);
                HtmlNode playListHtmlNode = movieWatchPage.SelectSingleNode("//div[@class='list-server']");
                rs.Add((playListHtmlNode != null) ? playListHtmlNode.OuterHtml : movieWatchPage.SelectSingleNode("//div[@id='block-player']").PreviousSibling.Attributes["src"].Value);
                rs.Add("");
                return rs;
            } catch(Exception ex)
            {
                logForm.append("====> Error: " + ex.Message);
                return null;
            }
           
        }


        public override ICollection<country> getCountries(HtmlNode victimDetailContent)
        {
            var dd = getDDItems ();
            var countries = dd[2].InnerText.Trim().Split(',');
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
            var dd = getDDItems ();
            var currentMovieInfo = dd[0].InnerText.Trim().ToLower();
            if (currentMovieInfo.ToLower().IndexOf("trailer") != -1)
            {
                return true;
            }
            return (currentMovieInfo.ToLower().IndexOf("trailer") != -1);
        }

        private List<HtmlNode> getDDItems ()
        {
            var ddItemNodes = getDDItemNodes();
            if (ddItemNodes == null) return null;
            ddItems = new List<HtmlNode>();
            removeDdItemNotUse(ddItemNodes);
            return ddItems;
        }

        private HtmlNodeCollection getDDItemNodes()
        {
            return victimDetailContent.SelectNodes("//dl[@class='movie-dl'] //dd");
        }

        private void removeDdItemNotUse(HtmlNodeCollection ddItemNodes) 
        {
            int index = 0;
            var dtItems = victimDetailContent.SelectNodes("//dl[@class='movie-dl'] //dt");
            var temp = ddItemNodes.ToArray();
            foreach (var dtItem in dtItems)
            {
                if (dtItem.InnerText.Trim().IndexOf("Điểm") == -1 
                    && dtItem.InnerText.Trim().IndexOf("Ngày") == -1 
                    && dtItem.InnerText.Trim().IndexOf("Số tập") == -1 
                    && dtItem.InnerText.Trim().IndexOf("Công ty SX") == -1 
                    && dtItem.InnerText.Trim().IndexOf("Chất lượng") == -1
                    && dtItem.InnerText.Trim().IndexOf("Số người đánh giá") == -1)
                {
                   
                    ddItems.Add(ddItemNodes.ToArray()[index]);
                }
                index++;
            }
         }

        public override void checkToAddOrUpdateProposeMovies(movie movieItemInfo)
        {
            var homePageContent = getHomePageContent();
            var proposeMovieItems = homePageContent.SelectNodes("//ul[@id='movie-carousel-top'] //a");
            if (isExistPositionType(movieItemInfo, proposeMovieItems, ".//h3[@class='movie-name-1']"))
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
                } else
                {
                    proposeMovie.dateUpdate = DateTime.Now;
                }
               
            }
        }

        public override void checkToAddOrUpdateOneEpisodeHostWeekMovies(movie movieItemInfo)
        {
            var homePageContent = getHomePageContent();
            var weekOneEpisodeMovieItems = homePageContent.SelectNodes("//ul[@id='list-top-film-week']")[1].SelectNodes(".//a");
            if (isExistPositionType(movieItemInfo, weekOneEpisodeMovieItems, ".//span[@class='list-top-movie-item-vn']"))
            {
                home_page_position weekOneEpisodeMovie = (home_page_position)movieItemInfo.home_page_position.FirstOrDefault(a => a.positionType == 1);
                if (weekOneEpisodeMovie == null)
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
                    weekOneEpisodeMovie.dateUpdate = DateTime.Now;
                }
                
            }
        }

        public override void checkToAddOrUpdateMultiEpisodeHostWeekMovies(movie movieItemInfo)
        {
            var homePageContent = getHomePageContent();
            var weekMultiEpisodeMovieItems = homePageContent.SelectNodes("//ul[@id='list-top-film-week']")[2].SelectNodes(".//a");
            if (isExistPositionType(movieItemInfo, weekMultiEpisodeMovieItems, ".//span[@class='list-top-movie-item-vn']"))
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
            var cinemaMovieItems = homePageContent.SelectNodes("//ul[@id='movie-last-theater'] //a");
            if (isExistPositionType(movieItemInfo, cinemaMovieItems, ".//div[@class='movie-title-1']"))
            {
                home_page_position cinemaMovie = (home_page_position)movieItemInfo.home_page_position.FirstOrDefault(a => a.positionType == 3);
                if (cinemaMovie == null)
                {
                    movieItemInfo.home_page_position.Add(new home_page_position()
                    {
                        positionType = 3,
                        movieId = movieItemInfo.movieId,
                        description = "CinemaMovie",
                        dateUpdate = DateTime.Now
                    });
                }
                else
                {
                    cinemaMovie.dateUpdate = DateTime.Now;
                }
                
            }
        }

        public override void checkToAddOrUpdateOneEpisodeLastestUpdatedMovies(movie movieItemInfo)
        {
            var homePageContent = getHomePageContent();
            var oneEpisodeLastestUpdatedMovieItems = homePageContent.SelectNodes("//ul[@id='movie-last-movie'] //a");
            if (isExistPositionType(movieItemInfo, oneEpisodeLastestUpdatedMovieItems, ".//div[@class='movie-title-1']"))
            {
                home_page_position oneEpisodeLastestUpdatedMovie = (home_page_position)movieItemInfo.home_page_position.FirstOrDefault(a => a.positionType == 4);
                if (oneEpisodeLastestUpdatedMovie == null)
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
                    oneEpisodeLastestUpdatedMovie.dateUpdate = DateTime.Now;
                }
               
            }
        }

        public override void checkToAddOrUpdateMultiEpisodeLastestUpdatedMovies(movie movieItemInfo)
        {
            //var homePageContent = getHomePageContent();
            //var multiEpisodeLastestUpdatedMovieItems = homePageContent.SelectNodes("//ul[@id='movie-last-series'] //a");
            //if (isExistPositionType(movieItemInfo, multiEpisodeLastestUpdatedMovieItems, ".//div[@class='movie-title-1']"))
            //{
            //    home_page_position multiEpisodeLastestUpdatedMovie = (home_page_position)movieItemInfo.home_page_position.FirstOrDefault(a => a.positionType == 5);
            //    if (multiEpisodeLastestUpdatedMovie == null)
            //    {
            //        movieItemInfo.home_page_position.Add(new home_page_position()
            //        {
            //            positionType = 5,
            //            movieId = movieItemInfo.movieId,
            //            description = "MultiEpisodeLastestUpdatedMovie",
            //            dateUpdate = DateTime.Now
            //        });
            //    }
            //    else
            //    {
            //        multiEpisodeLastestUpdatedMovie.dateUpdate = DateTime.Now;
            //    }
               
            //}
        }

        public override void checkToAddOrUpdateCartoonMovies(movie movieItemInfo)
        {
            var homePageContent = getHomePageContent();
            var cartoonMovieItems = homePageContent.SelectNodes("//ul[@id='movie-last-cartoon'] //a");
            if (isExistPositionType(movieItemInfo, cartoonMovieItems, ".//div[@class='movie-title-1']"))
            {
                home_page_position cartoonMovie = (home_page_position)movieItemInfo.home_page_position.FirstOrDefault(a => a.positionType == 6);
                if (cartoonMovie == null)
                {
                    movieItemInfo.home_page_position.Add(new home_page_position()
                    {
                        positionType = 6,
                        movieId = movieItemInfo.movieId,
                        description = "CartoonMovie",
                        dateUpdate = DateTime.Now
                    });
                }
                else
                {
                    cartoonMovie.dateUpdate = DateTime.Now;
                }
               
            }
        }
    }
}
