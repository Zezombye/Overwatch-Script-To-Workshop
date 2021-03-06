using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.IO;
using HtmlAgilityPack;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.WorkshopWiki
{
    public class Wiki
    {
        public const string URL = "https://us.forums.blizzard.com/en/overwatch/t/wiki-workshop-syntax-script-database/";
        private static Log Log = new Log("Wiki");

        private static Wiki wiki = null; 
        private static bool gotWiki = false;
        public static Wiki GetWiki()
        {
            try
            {
                if (gotWiki)
                    return wiki;
                gotWiki = true;
                
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.OptionFixNestedTags = true;
            
                using (var webClient = new WebClient())
                    htmlDoc.Load(webClient.OpenRead(URL), Encoding.UTF8);
                
                List<WikiMethod> methods = new List<WikiMethod>();

                // Loop through all summaries
                foreach(var summary in htmlDoc.DocumentNode.Descendants("summary"))
                {
                    string name = summary.InnerText.Trim(); // Gets the name

                    // Sometimes methods have "- New!" at the end, be sure to remove it.
                    if (name.EndsWith("- New!"))
                        name = name.Substring(0, name.Length - "- New!".Length).Trim();

                    var details = summary.ParentNode;
                    string description = details.SelectNodes("p").First().InnerText.Trim(); // Gets the description.

                    // Get the parameters.
                    List<WikiParameter> parameters = new List<WikiParameter>();
                    var parameterSummaries = details.SelectSingleNode("ul")?.SelectNodes("li"); // 'ul' being list and 'li' being list element.
                    if (parameterSummaries != null)
                        foreach (var parameterSummary in parameterSummaries)
                        {
                            string[] data = parameterSummary.InnerText.Split(new char[]{'-'}, 2);
                            parameters.Add(new WikiParameter(data[0].Trim(), data.ElementAtOrDefault(1)?.Trim()));
                        }

                    methods.Add(new WikiMethod(name, description, parameters.ToArray()));
                }

                wiki = new Wiki(methods.ToArray());
                return wiki;
            }
            catch (Exception ex)
            {
                Log.Write(LogLevel.Normal, "Failed to load Workshop Wiki: ", new ColorMod(ex.Message, ConsoleColor.Red));
                return null;
            }
        }

        public WikiMethod[] Methods { get; }
        public Wiki(WikiMethod[] methods)
        {
            Methods = methods;
        }

        public WikiMethod GetMethod(string methodName)
        {
            return Methods.FirstOrDefault(m => m.Name.ToLower() == methodName.ToLower());
        }

        public string ToXML()
        {
            return Extras.SerializeToXML<Wiki>(this);
        }
    }

    public class WikiMethod
    {
        public string Name { get; }
        public string Description { get; }
        public WikiParameter[] Parameters { get; }
        public WikiMethod(string name, string description, WikiParameter[] parameters)
        {
            Name = name;
            Description = description;
            Parameters = parameters;
        }

        public override string ToString()
        {
            return Name;
        }

        public WikiParameter GetWikiParameter(string name)
        {
            return Parameters.FirstOrDefault(p => p.Name.ToLower() == name.ToLower());
        }
    }

    public class WikiParameter
    {
        public string Name { get; }
        public string Description { get; }
        public WikiParameter(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}