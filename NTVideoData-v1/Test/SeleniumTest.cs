using NTVideoData_v1.Utils;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTVideoData_v1.Test
{
    class SeleniumTest
    {
        IWebDriver driver;

        [SetUp]
        public void initTest()
        {
            driver = new FirefoxDriver();
           
        }

        [Test]
        public void loadPageTest()
        {
            WebDriverHelper.singleton().ExampleUse();
            driver.Url = "http://www.demoqa.com";
        }

        [TearDown]
        public void endTest()
        {
            driver.Url = "http://www.demoqa.com";
        }
    }
}
