using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LeStreamsFace
{
    internal enum StreamingSite
    {
        TwitchTv = 0, OwnedTv
    }

    internal class Stream : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _title = string.Empty;
        private int _viewers = -1;
        private string _id = "-1";
        private StreamingSite _site = StreamingSite.TwitchTv;
        private bool _isFavorite = false;
        private string _gameName = string.Empty;
        private string _channelId = string.Empty;
        private string _thumbnailURI = string.Empty;

        [JsonProperty(PropertyName = "Name")]
        public string Name
        {
            get { return _name; }
            private set { if (value == null) return; _name = value; onPropertyChanged(this, "Name"); }
        }

        [JsonProperty(PropertyName = "Title")]
        public string Title
        {
            get { return _title; }
            private set
            {
                if (value == null) return;
                _title = value.Replace("\r\n", " ").Replace("\n", " ").TrimEnd(new[] { ' ', '\n' });
                onPropertyChanged(this, "Title");
            }
        }

        [JsonProperty(PropertyName = "Viewers")]
        public int Viewers
        {
            get { return _viewers; }
            private set { _viewers = value; onPropertyChanged(this, "Viewers"); }
        }

        [JsonProperty(PropertyName = "_id")]
        public string Id
        {
            get { return _id; }
            set { if (value == null) return; _id = value; onPropertyChanged(this, "Id"); }
        }

        [JsonProperty(PropertyName = "Site")]
        public StreamingSite Site
        {
            get { return _site; }
            protected set { _site = value; onPropertyChanged(this, "Site"); }
        }

        [JsonProperty(PropertyName = "IsFavorite")]
        public bool IsFavorite
        {
            get { return _isFavorite; }
            set { _isFavorite = value; onPropertyChanged(this, "IsFavorite"); }
        }

        [JsonProperty(PropertyName = "game")]
        public string GameName
        {
            get { return _gameName; }
            private set { if (value == null) return; _gameName = value; onPropertyChanged(this, "GameName"); }
        }

        [JsonProperty(PropertyName = "ChannelId")]
        public string ChannelId
        {
            get { return _channelId; }
            protected set { if (value == null) return; _channelId = value; onPropertyChanged(this, "ChannelId"); }
        }

        public string LoginNameTwtv
        {
            get { return _loginNameTwtv; }
            set { if (value == null) return; _loginNameTwtv = value; onPropertyChanged(this, "LoginNameTwtv"); }
        }

        [JsonProperty(PropertyName = "ThumbnailURI")]
        public string ThumbnailURI
        {
            get { return _thumbnailURI; }
            set
            {
                if (value == null) return;
                _thumbnailURI = value;
                onPropertyChanged(this, "ThumbnailURI");
            }
        }

        private string _loginNameTwtv;
        public bool GottenViaAutoGetFavs = false;

//        public Stream()
//        {
//        } // we don't want the deserializer to call this

        public Stream(string name, string title, int? viewers, string id, string channelId, string gameName, StreamingSite streamingSite = StreamingSite.TwitchTv)//:this()
        {
            Name = name ?? string.Empty;
            Title = title ?? string.Empty;
            Viewers = viewers.GetValueOrDefault();
            Id = id ?? string.Empty;
            GameName = gameName ?? string.Empty;
            Site = streamingSite;
            ChannelId = channelId ?? string.Empty;
            ThumbnailURI = string.Empty;

            IsFavorite = ConfigManager.Instance.FavoriteStreams.Any(stream => stream.ChannelId == ChannelId);
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

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            foreach (var property in typeof(Stream).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                stringBuilder.Append(property.Name + ": " + property.GetValue(this, null) + Environment.NewLine);
            }
            return stringBuilder.ToString();
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(this._name != null);
            Contract.Invariant(this._title != null);
            Contract.Invariant(this._id != null);
            Contract.Invariant(this._gameName != null);
            Contract.Invariant(this._channelId != null);
            Contract.Invariant(this._thumbnailURI != null);
        }
    }
}