using System.ComponentModel;
using System.Linq;

namespace LeStreamsFace
{
    internal enum StreamingSite
    {
        TwitchTv = 0, OwnedTv
    }

    internal class Stream : INotifyPropertyChanged
    {
        private string _name;
        private string _title;
        private int _viewers;
        private string _id;
        private StreamingSite _site;
        private bool _isFavorite;
        private string _gameName;
        private string channelId;
        private string _thumbnailURI;

        public string Name
        {
            get { return _name; }
            private set { _name = value; onPropertyChanged(this, "Name"); }
        }

        public string Title
        {
            get { return _title; }
            private set { _title = value; onPropertyChanged(this, "Title"); }
        }

        public int Viewers
        {
            get { return _viewers; }
            private set { _viewers = value; onPropertyChanged(this, "Viewers"); }
        }

        public string Id
        {
            get { return _id; }
            set { _id = value; onPropertyChanged(this, "Id"); }
        }

        public StreamingSite Site
        {
            get { return _site; }
            protected set { _site = value; onPropertyChanged(this, "Site"); }
        }

        public bool IsFavorite
        {
            get { return _isFavorite; }
            set { _isFavorite = value; onPropertyChanged(this, "IsFavorite"); }
        }

        public string GameName
        {
            get { return _gameName; }
            private set { _gameName = value; onPropertyChanged(this, "GameName"); }
        }

        public string ChannelId
        {
            get { return channelId; }
            protected set { channelId = value; onPropertyChanged(this, "ChannelId"); }
        }

        public string LoginNameTwtv
        {
            get { return _loginNameTwtv; }
            set { _loginNameTwtv = value; onPropertyChanged(this, "LoginNameTwtv"); }
        }

        public string ThumbnailURI
        {
            get { return _thumbnailURI; }
            set { _thumbnailURI = value; onPropertyChanged(this, "ThumbnailURI"); }
        }

        private string _loginNameTwtv;
        public bool GottenViaAutoGetFavs = false;

        protected Stream()
        {
        }

        public Stream(string name, string title, int viewers, string id, string channelId, string gameName, StreamingSite streamingSite)
        {
            Name = name;
            Title = title;
            Viewers = viewers;
            Id = id;
            GameName = gameName;
            Site = streamingSite;
            ChannelId = channelId;

            IsFavorite = ConfigManager.FavoriteStreams.Any(stream => stream.ChannelId == channelId);
        }

        public string GetUrl()
        {
            string url = string.Empty;
            if (Site == StreamingSite.TwitchTv)
            {
                url = "http://www.twitch.tv/" + LoginNameTwtv;
            }
            if (Site == StreamingSite.OwnedTv)
            {
                url = Id;
            }
            return url;
        }

        public void UpdateStreamData(Stream streamNewData)
        {
            if (streamNewData.Viewers != this.Viewers)
            {
                this.Viewers = streamNewData.Viewers;
            }
            if (streamNewData.Title != this.Title)
            {
                this.Title = streamNewData.Title;
            }
            if (streamNewData.GameName != this.GameName)
            {
                this.GameName = streamNewData.GameName;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void onPropertyChanged(object sender, string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(sender, new PropertyChangedEventArgs(propertyName));
            }
        }

        public override bool Equals(System.Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            Stream stream = obj as Stream;

            return Equals(stream);
        }

        public bool Equals(Stream stream)
        {
            if ((object)stream == null)
            {
                return false;
            }

            return Id == stream.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(Stream a, Stream b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(Stream a, Stream b)
        {
            return !(a == b);
        }
    }
}