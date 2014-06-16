namespace LeStreamsFace
{
    class GamesViewModel
    {
        public string GameName { get; set; }
        public string Thumbnail { get; set; }

        public GamesViewModel(string gameName, string thumbnail)
        {
            GameName = gameName;
            Thumbnail = thumbnail;
        }
    }
}