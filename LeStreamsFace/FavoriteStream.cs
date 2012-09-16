namespace LeStreamsFace
{
    internal class FavoriteStream : Stream
    {
        // constructor for empty, favorite streams from config
        public FavoriteStream(string loginName, string channelId, StreamingSite streamingSite)
            : base("", "", 0, channelId, channelId, "", streamingSite)
        {
            LoginNameTwtv = loginName;
            IsFavorite = true;
        }
    }
}