namespace LeStreamsFace
{
    internal class TabCreationEvent
    {
        public Stream Stream { get; private set; }

        public TabCreationEvent(Stream stream)
        {
            Stream = stream;
        }
    }
}