using AutomatedTester.BrowserMob;
using AutomatedTester.BrowserMob.HAR;
using HtmlAgilityPack;
using NTVideoData.Util;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NTVideoData_v1.Utils
{
    public class WebDriverHelper: WebParserUtil
    {
        public void ExampleUse()
        {
            // Supply the path to the Browsermob Proxy batch file
            Server server = new Server(@"E:\D-hard-disk\jeff-projects\servers\selenium\browsermob-proxy-2.1.4\bin\browsermob-proxy.bat");
            server.Start();

            Client client = server.CreateProxy();
            client.NewHar("vnexpress");
          
            var seleniumProxy = new Proxy { HttpProxy = client.SeleniumProxy };
            var profile = new FirefoxProfile();
            profile.SetProxyPreferences(seleniumProxy);
          
            // Navigate to the page to retrieve performance stats for
            IWebDriver driver = new FirefoxDriver(profile);
            driver.Navigate().GoToUrl("http://www.phimcuaban.com/xem-phim-vo-cuc-the-promise.html");
            //driver.FindElement(By.ClassName("drive-viewer-video-play-icon")).Click();
            //var iframe = waitElementPresent(driver, "//iframe[@id='drive-viewer-video-player-object-0']");

            //var iframe = waitElementPresent(driver, "//iframe[@id='drive-viewer-video-player-object-0']");
            //var src = iframe.GetAttribute("src");
            //driver.Navigate().GoToUrl(src);
            //var v = driver.FindElement(By.TagName("video"));
            //driver.SwitchTo().Frame(driver.FindElement(By.XPath("//iframe[@id='drive-viewer-video-player-object-0']")));
            //var url = driver.FindElement(By.TagName("video")).GetAttribute("src");
            // Get the performance stats
            //HarResult harData = client.GetHar();
            //var a = harData;
            // Do whatever you want with the metrics here. Easy to persist 
            // out to a data store for ongoing metrics over time.

            driver.Quit();
            client.Close();
            server.Stop();
        }

        private static IWebElement waitElementPresent(IWebDriver driver, string xpath)
        {
            try
            {
               return driver.FindElement(By.XPath(xpath));
            } catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                waitElementPresent(driver, xpath);
            }
            return null;
        }

        static WebDriverHelper webDriverHelper = null;
        //public IWebDriver browser = new FirefoxDriver();

        public static WebDriverHelper singleton()
        {
            if (webDriverHelper == null)
            {
               webDriverHelper = new WebDriverHelper();
            }
            return webDriverHelper;
        }

        //public string getContent(string url)
        //{
        //    browser.Url = url;
        //    return browser.PageSource.ToString(); ;
        //}

        //public new HtmlNode selectSingleNode(string url, string selector)
        //{
        //    var rootNode = convertDomStringToHtmlNode(getContent(url));
        //    return rootNode.SelectSingleNode(selector);
        //}

        //public new HtmlNodeCollection selectNodes(string url, string selector)
        //{
        //    var rootNode = convertDomStringToHtmlNode(getContent(url));
        //    return rootNode.SelectNodes(selector);
        //}
    }

    
}
