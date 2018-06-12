using HtmlAgilityPack;
using NTVideoData.Daos;
using NTVideoData_v1.Entities;
using NTVideoData.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity.Validation;
using NTVideoData_v1;
using NTVideoData_v1.Utils;

namespace NTVideoData.Victims
{

    abstract public class BaseVictim
    {
        public string protocol = "http://";
        string batchFolder = "batch";
        string batchFile = "LoadScripts.bat";
        string sqlResouceFolder = @"\sql-resource\";
        public string victimDomain = "";
        public string victimDomainNoExt = "";
        public string victimMediaDomain = "";
        public int victimTypeNo = 0;
        public string mediaUrl = "http://media.phimcuaban.com";
        public string victimMediaController = "/images/";
        public string meDomain = "phimcuaban.com";
        public string meDomainNoExt = "phimcuaban";
        public HtmlNode victimDetailContent;
        public HtmlNode homePageContent = null;
        public string[] destToGetUrls = new string[4];
        protected string currentDestToGetUrl = "";
        public string pageIndexParam = "";
        public bool stopUpdateFlag = true;
        public bool stopInsertFlag = true;
        public LogForm logForm;
        public List<string> checkFields = null;
      
        public void showForm()
        {
            logForm.Show();
        }

        public void setLogForm(LogForm logForm)
        {
            this.logForm = logForm;
        }

        public BaseVictim() {  }

        private static BaseVictim baseVictim = null;

        public static BaseVictim instance()
        {
            return baseVictim;
        }

        public static void setInstance(BaseVictim vt)
        {
            baseVictim = vt;
        }

        public Boolean IsNumber(String value)
        {
            return (value.All(Char.IsDigit) && value != "") ? true : false;
        }

        public void setTypeToGet(int typeToGet)
        {
            currentDestToGetUrl = destToGetUrls[typeToGet];
        }

        public episode newEpisodeInstance(string name, int victimId, string groupName, string episodeHref)
        {
            var episode = new episode()
            {
                name = name,
                victimId = victimId,
                groupName = groupName,
                episodeHref = episodeHref,
                alias = UnicodeUtil.convertToAlias(name)
            };
            return episode;
        }

        public string buildNameCheckField(string str)
        {
            return UnicodeUtil.convertToAlias(str.Split('(')[0].Trim());
        }



        abstract public HtmlNodeCollection getItemsInPage(int pageIndex);
        abstract public movie getMovieItemInfo(string itemUrl);
        abstract public movie getMovie(HtmlNode victimDetailContent);
        abstract public bool is404(HtmlNode victimDetailContent);
        abstract public victim getVictim(HtmlNode victimDetailContent);
        abstract public bool checkIgnoreMovie(HtmlNode victimDetailContent);
        abstract public ICollection<category> getCategories(HtmlNode victimDetailContent);
        abstract public ICollection<actor> getActors(HtmlNode victimDetailContent);
        abstract public ICollection<director> getDirectors(HtmlNode victimDetailContent);
        abstract public ICollection<country> getCountries(HtmlNode victimDetailContent);
        abstract public ICollection<episode> getEpisodes(string movieHref, bool excludeGroup);
        abstract public ICollection<episode> getAndAddEpisodesToVictim(victim vtm);
        abstract public List<String> getPlayListHtml(String movieHref);
        abstract public void filterImages(HtmlNodeCollection images);
        abstract public void checkToAddOrUpdateProposeMovies(movie movieItemInfo);
        abstract public void checkToAddOrUpdateOneEpisodeHostWeekMovies(movie movieItemInfo);
        abstract public void checkToAddOrUpdateMultiEpisodeHostWeekMovies(movie movieItemInfo);
        abstract public void checkToAddOrUpdateCinemaMovies(movie movieItemInfo);
        abstract public void checkToAddOrUpdateOneEpisodeLastestUpdatedMovies(movie movieItemInfo);
        abstract public void checkToAddOrUpdateMultiEpisodeLastestUpdatedMovies(movie movieItemInfo);
        abstract public void checkToAddOrUpdateCartoonMovies(movie movieItemInfo);


        public string getEpisodePlayerInfo(string episodeHref)
        {
            string moviePlayerInfo = WebParserUtil.getContentByUrl(episodeHref);
            return moviePlayerInfo;
        }

        public HtmlNode getHomePageContent()
        {
            if (homePageContent == null)
            {
                homePageContent = getRootNode("http://www." + victimDomain);
            }
            return homePageContent;
        }

        public bool isExistPositionType(movie movieItemInfo, HtmlNodeCollection movieItemsByPositionType, string name1Selector)
        {
            foreach (HtmlNode movieItemByPositionType in movieItemsByPositionType)
            {
                var firstName = movieItemByPositionType.SelectSingleNode(name1Selector).InnerText.Trim();
                //if (firstName.ToLower() == movieItemInfo.name1.ToLower())
                if (firstName.ToLower().IndexOf(movieItemInfo.name1.ToLower()) != -1)
                {
                    return true;
                }
            }
            return false;
        }

        public ICollection<keyword> buildKeyWords(movie mv)
        {
            ICollection<string> keyWords = new HashSet<string>();
            string typeEpisode = (mv.isMultiEpisode == 0) ? "phim-le" : "phim-bo";
            
            foreach (var category in mv.categories)
            {
                keyWords.Add(typeEpisode + '-' + mv.name1CheckField + "-" + category.alias);
                keyWords.Add(typeEpisode + '-' + mv.name2CheckField + "-" + category.alias);
                keyWords.Add(typeEpisode + "-" + category.alias + '-' + mv.name1CheckField);
                keyWords.Add(typeEpisode + "-" + category.alias + '-' + mv.name2CheckField);
                
                foreach (var country in mv.countries)
                {
                    keyWords.Add(typeEpisode + "-" + category.alias + "-" + country.alias);
                    keyWords.Add(typeEpisode + "-" + category.alias + "-" + mv.publishYear);
                    keyWords.Add(typeEpisode + "-" + category.alias + "-" + country.alias + "-" + mv.publishYear);
                    keyWords.Add(typeEpisode + "-" + category.alias + "-" + mv.publishYear + "-" + country.alias);
                }

                foreach (var actor in mv.actors)
                {
                    keyWords.Add(typeEpisode + "-" + category.alias + "-" + actor.alias);
                    keyWords.Add(typeEpisode + "-" + category.alias + "-" + mv.publishYear);
                    keyWords.Add(typeEpisode + "-" + category.alias + "-" + actor.alias + "-" + mv.publishYear);
                    keyWords.Add(typeEpisode + "-" + category.alias + "-" + mv.publishYear + "-" + actor.alias);
                }

                foreach (var director in mv.directors)
                {
                    keyWords.Add(typeEpisode + "-" + category.alias + "-" + director.alias);
                    keyWords.Add(typeEpisode + "-" + category.alias + "-" + mv.publishYear);
                    keyWords.Add(typeEpisode + "-" + category.alias + "-" + director.alias + "-" + mv.publishYear);
                    keyWords.Add(typeEpisode + "-" + category.alias + "-" + mv.publishYear + "-" + director.alias);
                }
            }

            foreach (var country in mv.countries)
            {
                keyWords.Add(typeEpisode + '-' + mv.name1CheckField + "-" + country.alias);
                keyWords.Add(typeEpisode + '-' + mv.name2CheckField + "-" + country.alias);
                keyWords.Add(typeEpisode + "-" + country.alias + '-' + mv.name1CheckField);
                keyWords.Add(typeEpisode + "-" + country.alias + '-' + mv.name2CheckField);
            }

            foreach (var actor in mv.actors)
            {
                keyWords.Add(typeEpisode + '-' + mv.name1CheckField + "-" + actor.alias);
                keyWords.Add(typeEpisode + '-' + mv.name2CheckField + "-" + actor.alias);
                keyWords.Add(typeEpisode + "-" + actor.alias + '-' + mv.name1CheckField);
                keyWords.Add(typeEpisode + "-" + actor.alias + '-' + mv.name2CheckField);
            }

            foreach (var director in mv.directors)
            {
                keyWords.Add(typeEpisode + '-' + mv.name1CheckField + "-" + director.alias);
                keyWords.Add(typeEpisode + '-' + mv.name2CheckField + "-" + director.alias);
                keyWords.Add(typeEpisode + "-" + director.alias + '-' + mv.name1CheckField);
                keyWords.Add(typeEpisode + "-" + director.alias + '-' + mv.name2CheckField);
            }

            keyWords.Add(typeEpisode + '-' + mv.name1CheckField + "-" + mv.publishYear);
            keyWords.Add(typeEpisode + '-' + mv.name2CheckField + "-" + mv.publishYear);
            keyWords.Add(typeEpisode + "-" + mv.publishYear + '-' + mv.name1CheckField);
            keyWords.Add(typeEpisode + "-" + mv.publishYear + '-' + mv.name2CheckField);
         
            ICollection<keyword> rs = new HashSet<keyword>();
            rs.Add(new keyword() {
                movieId = mv.movieId,
                keyWords = string.Join("-", keyWords.ToArray())
            });
            return rs;
        }

        public string getQuality(string currentMovieInfo)
        {
            string[] qualitiesRepo = { "SD", "CAM", "DVD", "RAW", "HD" };
            var quality = "HD";
            foreach (var q in qualitiesRepo)
            {
                if (currentMovieInfo.IndexOf(q.ToLower()) != -1)
                {
                    quality = q;
                }
            }
            return quality;
        }

        public bool movieExisted(movie mv)
        {
            var movies = findMovieByCheckFields(mv.name1CheckField, mv.name2CheckField);
            return (movies != null) ? true : false;
        }

        public bool movieExisted(string checkField)
        {
            var movieExisted = checkFields.Contains(checkField);
            logForm.append("   movieExisted: " + movieExisted);
            return movieExisted;
        }

        public bool movieExistedWithVictim(movie mv, string victimName)
        {
            using (var context = new NTVideoEntities())
            {
                var movies = from m in context.movies
                             from vtm in context.victims
                             where m.movieId == vtm.movieId && vtm.victimTypeNo == victimTypeNo && (m.name1CheckField == mv.name1CheckField && m.name2CheckField == mv.name2CheckField)
                             select m;
                return (movies.Count() > 0) ? true : false;
            }
        }

        public List<string> findCheckFieldAllMovies()
        {
            using (var context = new NTVideoEntities())
            {
                var checkFields = from m in context.movies
                             select new { checkField = m.name1CheckField + "-" + m.name2CheckField };
                var listCheckFields = checkFields.Select(o => o.checkField).ToList();
                return listCheckFields;
            }
        }

        public movie findMovieByCheckFields(string name1CheckField, string name2CheckField)
        {
            using (var context = new NTVideoEntities())
            {
                var movies = from m in context.movies
                             where m.name1CheckField == name1CheckField && m.name2CheckField == name2CheckField
                             select m;
                return movies.FirstOrDefault();
            }
        }

        public bool actorExisted(string actorName)
        {
            var actor = findActorByName(actorName);
            return (actor != null) ? true : false;
        }

        public actor findActorByName(string actorName)
        {
            using (var context = new NTVideoEntities())
            {
                var actors = from m in context.actors
                             where m.name == actorName
                             select m;
                return actors.FirstOrDefault();
            }
        }

        public bool directorExisted(string directorName)
        {
            var director = findDirectorByName(directorName);
            return (director != null) ? true : false;
        }

        public director findDirectorByName(string directorName)
        {
            using (var context = new NTVideoEntities())
            {
                var directors = from m in context.directors
                                where m.name == directorName
                                select m;
                return directors.FirstOrDefault();
            }
        }

        public bool countryExisted(string countryName)
        {
            var country = findCountryByName(countryName);
            return (country != null) ? true : false;
        }

        public country findCountryByName(string countryName)
        {
            using (var context = new NTVideoEntities())
            {
                var countries = from m in context.countries
                                where m.name == countryName
                                select m;
                return countries.FirstOrDefault();
            }
        }

        public bool categoryExisted(string categoryName)
        {
            var category = findCategoryByName(categoryName);
            return (category != null) ? true : false;
        }

        public category findCategoryByName(string categoryName)
        {
            using (var context = new NTVideoEntities())
            {
                var categories = from m in context.categories
                                 where m.name == categoryName
                                 select m;
                return categories.FirstOrDefault();
            }
        }

        public void stopInsert()
        {
            stopInsertFlag = true;
        }

        public void startInsert()
        {
            stopInsertFlag = false;
        }

        abstract public void doGetListItemsInfo(int pageIndex);

        public HtmlNode getRootNode(string url)
        {
            return WebParserUtil.getRootDocument(url).DocumentNode.SelectSingleNode("//body");
        }

        public string enhanceDescription(HtmlNode desNode)
        {
            filterLinks(desNode.SelectNodes(".//a"));
            filterImages(desNode.SelectNodes(".//img"));
            var desTemp = replaceDomain(desNode.InnerHtml);
            desNode.InnerHtml = desTemp;
            var imgs = desNode.SelectNodes(".//img");
            if(imgs != null)
            {
                foreach (HtmlNode img in imgs)
                {
                    var imgSrcTemp = img.Attributes["src"].Value;
                    img.Attributes["src"].Value = mediaUrl + victimMediaController + CryptoUtil.encrypt(imgSrcTemp) + ".jpg";
                    if (img.Attributes["title"] != null && img.Attributes["title"].Value.ToLower().IndexOf(victimDomain) != -1)
                    {
                        img.Attributes["title"].Value = imgSrcTemp;
                    }
                }
            }
            
            return desNode.InnerHtml;
        }
        
        public void filterLinks(HtmlNodeCollection links)
        {
            if (links == null) return;
            foreach (HtmlNode link in links)
            {
                HtmlDocument docTem = link.OwnerDocument;
                HtmlNode tempNode = docTem.CreateElement("span");
                tempNode.AppendChild(docTem.CreateTextNode(link.InnerText));
                link.ParentNode.ReplaceChild(tempNode, link);
            }
        }

        public string removeTextNodes(HtmlNode node, string tagName)
        {
            var childNodes = node.SelectNodes(".//" + tagName);
            if (childNodes == null) return node.InnerHtml;
            foreach (HtmlNode childnode in childNodes)
            {
                node.InnerHtml = node.InnerHtml.Replace(childnode.OuterHtml, "");
            }
            return node.InnerHtml;
        }

        public string replaceDomain(string str)
        {
            str = str.Replace(">", "> ").Replace("<", " <");
            var strSplitArr = str.Split(' ');
            
            List<int> tempIndex = new List<int>();
            for (int i = 0; i < strSplitArr.Length; i++)
            {
                string firstStrSplit = UnicodeUtil.removeVietnameseUnicode(strSplitArr[i].ToLower().Trim());
                if (firstStrSplit == "phim")
                {
                    tempIndex.Add(i);
                    while (UnicodeUtil.removeVietnameseUnicode(strSplitArr[++i].ToLower().Trim()) == "");
                    string secondStrSplit = UnicodeUtil.removeVietnameseUnicode(strSplitArr[i].ToLower().Trim());
                    if (secondStrSplit == "bat")
                    {
                        tempIndex.Add(i);
                        while (UnicodeUtil.removeVietnameseUnicode(strSplitArr[++i].ToLower().Trim()) == "");
                        string thirdStrSplit = UnicodeUtil.removeVietnameseUnicode(strSplitArr[i].ToLower().Trim());
                        if (thirdStrSplit == "hu")
                        {
                            strSplitArr[i] = meDomain;
                        } else
                        {
                            tempIndex.RemoveAt(tempIndex.Count - 1);
                            tempIndex.RemoveAt(tempIndex.Count - 1);
                        }
                    }
                }
            }

            string[] strsNeedReplace = { "phimbathu", "bilutv" };
            for (int i = 0; i < strSplitArr.Length; i++)
            {
                string strSplit = UnicodeUtil.removeVietnameseUnicode(strSplitArr[i].ToLower()).Trim();
                foreach (var strNeedReplace in strsNeedReplace)
                {
                    if (strSplit.Trim().IndexOf(strNeedReplace) != -1 && strSplit.IndexOf("http") == -1)
                    {
                        strSplitArr[i] = strSplit.Replace(strNeedReplace, meDomainNoExt);
                        break;
                    }
                }
            }

            var rs = "";
            for (int i = 0; i < strSplitArr.Length; i++)
            {
                if (!tempIndex.Contains(i))
                {
                    rs += strSplitArr[i] + " ";
                }
            }
            return rs.TrimEnd();
        }

        public void stopUpdate()
        {
            stopUpdateFlag = true;
        }

        public void startUpdate()
        {
            stopUpdateFlag = false;
        }

        public bool beginInsertContent(int typeToGet)
        {
            setTypeToGet(typeToGet);
            doGetListItemsInfo(0);
            return true;
        }

        public int saveNewMovie(movie movieItemInfo)
        {
            using (BaseDao.instance())
            {

                try
                {
                    if (movieItemInfo.victims.First().movieId == null)
                    {
                        checkFields.Add(movieItemInfo.name1CheckField + "-" + movieItemInfo.name2CheckField);
                        updatePositionTypeOnHomePage(movieItemInfo);
                        BaseDao.instance().movies.Add(movieItemInfo);
                    }
                    else
                    {
                        BaseDao.instance().victims.Add(movieItemInfo.victims.First());
                    }
                    BaseDao.instance().SaveChanges();
                    logForm.append("==================   Begin save new movie: " + movieItemInfo.victims.First().movieHref);
                    return movieItemInfo.movieId;
                }
                catch (DbEntityValidationException e)
                {
                    foreach (var eve in e.EntityValidationErrors)
                    {
                        logForm.append(String.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors: ",
                            eve.Entry.Entity.GetType().Name, eve.Entry.State));
                        foreach (var ve in eve.ValidationErrors)
                        {
                            logForm.append(String.Format("- Property: \"{0}\", Error: \"{1}\" ",
                                ve.PropertyName, ve.ErrorMessage));
                        }
                    }
                }
            }
            return -1;
        }

        List<string> movieUpdatedTemp = new List<string>();
        public bool beginUpdateContent()
        {
            try
            {
                using (var context = new NTVideoEntities())
                {
                    var victimUpdateQuery = from m in context.movies
                                            from v in context.victims
                                            where m.movieId == v.movieId && m.isPublic == 1 && v.victimTypeNo == victimTypeNo && !(movieUpdatedTemp.Contains(v.movieDetailHref))
                                            orderby v.isContinue descending, m.publishYear descending, v.priority descending, v.viewed descending
                                            select new { m, v };
                    var updateInfo = victimUpdateQuery.FirstOrDefault();
                    if (updateInfo != null)
                    {
                        updatePositionTypeOnHomePage(updateInfo.m);
                        context.Entry(updateInfo.m).State = System.Data.Entity.EntityState.Modified;
                        context.SaveChanges();
                        var victim = updateInfo.v;
                        var victimUpdate = getVictim(getRootNode(victim.movieDetailHref.Replace("http://", "http://www.")));
                        if (victimUpdate != null)
                        {
                            
                            if (victim.currentEpisode < victimUpdate.currentEpisode
                               || victim.schedule != victimUpdate.schedule
                               || victim.viewed < victimUpdate.viewed
                               || victim.priority < victimUpdate.priority
                               || victim.isTrailer != victimUpdate.isTrailer
                               || victim.playListHtml != victim.playListHtml)
                            {
                                logForm.append("=====================   Begin update: " + victim.movieHref);
                                victim.dateUpdate = victimUpdate.dateUpdate;
                                victim.currentEpisode = victimUpdate.currentEpisode;
                                victim.totalEpisode = victimUpdate.totalEpisode;
                                victim.isContinue = victimUpdate.isContinue;
                                victim.hasTrans = victimUpdate.hasTrans;
                                victim.hasDubbing = victimUpdate.hasDubbing;
                                victim.hasSub = victimUpdate.hasSub;
                                victim.hasTrans = victimUpdate.hasTrans;
                                victim.quality = victimUpdate.quality;
                                victim.updateState = victim.updateState + 1;
                                victim.viewed = victimUpdate.viewed;
                                victim.rating = victimUpdate.rating;
                                victim.schedule = victimUpdate.schedule;
                                victim.isTrailer = victimUpdate.isTrailer;
                                victim.playListHtml = victimUpdate.playListHtml;
                                
                                context.Entry(victim).State = System.Data.Entity.EntityState.Modified;
                                context.SaveChanges();
                            }
                        }
                        else
                        {
                            updateInfo.m.isPublic = 0;
                            context.Entry(updateInfo.m).State = System.Data.Entity.EntityState.Modified;
                            context.SaveChanges();
                        }
                        movieUpdatedTemp.Add(victim.movieDetailHref);
                    }
                }
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    logForm.append(String.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State));
                    foreach (var ve in eve.ValidationErrors)
                    {
                        logForm.append(String.Format("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage));
                    }
                }
            }
            
            return true;
        }

        protected void setModified(object ex)
        {
            BaseDao.instance().Entry(ex).State = System.Data.Entity.EntityState.Modified;
        }

        public void getComment(HtmlNode victimDetailContent)
        {
            //var rating = victimDetailContent.SelectSingleNode("//span[@itemprop='ratingValue']")
        }

        public string[] convertNodesToArray(HtmlNodeCollection nodes)
        {
            List<string> temp = new List<string>();
            foreach (var node in nodes)
            {
                temp.Add(node.InnerText);
            }
            return temp.ToArray();
        }

        public void updatePositionTypeOnHomePage(movie movie)
        {
            checkToAddOrUpdateProposeMovies(movie);
            checkToAddOrUpdateOneEpisodeHostWeekMovies(movie);
            checkToAddOrUpdateMultiEpisodeHostWeekMovies(movie);
            checkToAddOrUpdateCinemaMovies(movie);
            checkToAddOrUpdateOneEpisodeLastestUpdatedMovies(movie);
            checkToAddOrUpdateMultiEpisodeLastestUpdatedMovies(movie);
            checkToAddOrUpdateCartoonMovies(movie);
           
        }

        abstract public bool isTrailer(HtmlNode victimDetailContent);
           
    }
    
}
