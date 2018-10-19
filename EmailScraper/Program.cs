using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EmailScraper
{
    class Program
    {
        private static HashSet<string> _visitedUrls = new HashSet<string>();
        private static HashSet<string> _storedMails = new HashSet<string>();
        private static SqlDbContext _db;
        private static string _originalBaseUrl, _cssTitle;

        static void Main(string[] args)
        {
            var originalStandardOutput = Console.Out;
            _db = new SqlDbContext();

            _visitedUrls = _db.Scrapeds.Select(x => x.Url).ToHashSet();
            _storedMails = _db.Scrapeds.Select(x => x.Email).ToHashSet();

            Console.WriteLine("Welcome to Email Scraper! \n Enter url where i can start: ");

            _originalBaseUrl = Console.ReadLine();

            Console.WriteLine("Enter css selector where i can find out each title: ");

            _cssTitle = Console.ReadLine();

            List<string> emails = new List<string>();
            ScrapeUrl(_originalBaseUrl, emails);

            Console.WriteLine("Scrape termined.");

            _db.Dispose();

            Console.SetOut(originalStandardOutput);
            Console.WriteLine("Scrape termined.");

            Console.ReadLine();
        }

        private static void ScrapeUrl(string url, IList<string> emails)
        {
            _visitedUrls.Add(url);

            Console.WriteLine($"Loading {url} ...");

            HtmlDocument doc = new HtmlWeb().Load(url);

            IList<HtmlNode> emailsFounded = doc.QuerySelectorAll($"a[href*=\"@\"]");

            Console.WriteLine($"Founded {emailsFounded.Count} emails in {url}");

            string name = doc.QuerySelectorAll(_cssTitle).FirstOrDefault()?.InnerText;

            foreach (var emailNode in emailsFounded)
            {
                string email = emailNode.GetAttributeValue("href", "#")
                    .Replace("mailto:", "")
                    .Replace("http://", "")
                    .Replace("https://", "");
                if (_storedMails.Contains(email)) continue;

                _db.Scrapeds.Add(new ScrapedItems
                {
                    Email = email,
                    Name = name,
                    Url = url
                });

                _storedMails.Add(email);
            }

            _db.SaveChanges();

            IList<HtmlNode> links = doc.QuerySelectorAll($"a[href^=\"{_originalBaseUrl}\"]");
            var hrefs = links.Select(x => x.GetAttributeValue("href", "#")).Except(_visitedUrls).Distinct().ToList();
            hrefs.Remove(url);

            Console.WriteLine($"Founded other {hrefs.Count} links to visit in {url}");

            foreach (var href in hrefs)
            {
                ScrapeUrl(RemoveQS(href), emails);
            }
        }

        private static string RemoveQS(string href)
        {
            int qmPos = href.IndexOf('?');
            if (qmPos > -1)
            {
                return href.Substring(0, qmPos);
            }

            return href;
        }
    }
}
