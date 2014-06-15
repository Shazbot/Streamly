using System.Xml.Linq;

namespace LeStreamsFace
{
    internal class TwitchStreamParser : IStreamParser
    {
        public Stream GetStreamFromXElement(XElement xElement)
        {
            string name = null, gameName = "", title = "", id = null, channelId = null, thumbnailURI;
            string twitchLogin = null;
            int viewers = 0;

            name = xElement.Element("channel").Element("title").Value;
            viewers = int.Parse(xElement.Element("channel_count").Value);
            id = xElement.Element("id").Value;
            title = (string)xElement.Element("title") ?? "";
            gameName = (string)xElement.Element("meta_game") ?? "";
            twitchLogin = xElement.Element("channel").Element("login").Value;
            channelId = xElement.Element("channel").Element("id").Value;

            //            thumbnailURI = stream.Element("channel").Element("screen_cap_url_large").Value;
            thumbnailURI = xElement.Element("channel").Element("screen_cap_url_huge").Value;

            if (gameName == "StarCraft II: Wings of Liberty")
            {
                gameName = "StarCraft II";
            }

            if (name == title)
            {
                title = string.Empty;
            }
            title = title.Replace("\\n", " ");

            var newStream = new Stream(name, title, viewers, id, channelId, gameName, StreamingSite.TwitchTv) { LoginNameTwtv = twitchLogin, ThumbnailURI = thumbnailURI };
            return newStream;
        }
    }
}