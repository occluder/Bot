namespace Bot.Models;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable IDE1006 // Naming Styles
internal class DiscordEmbed
{
    public string title { get; set; }
    public string description { get; set; }
    public string url { get; set; }
    public DateTime timestamp { get; set; }
    public int color { get; set; }
    public Footer footer { get; private set; }
    public Image image { get; private set; }
    public Thumbnail thumbnail { get; private set; }
    public Author author { get; private set; }
    public List<Field> fields { get; private set; } = new(25);

    public DiscordEmbed SetFooter(Action<Footer> footerBuilder)
    {
        Footer f = new();
        footerBuilder(f);
        this.footer = f;
        return this;
    }

    public DiscordEmbed SetImage(Action<Image> imageBuilder)
    {
        Image i = new();
        imageBuilder(i);
        this.image = i;
        return this;
    }

    public DiscordEmbed SetAuthor(Action<Author> authorBuilder)
    {
        Author a = new();
        authorBuilder(a);
        this.author = a;
        return this;
    }

    public DiscordEmbed AddField(Action<Field> fieldBuilder)
    {
        Field f = new();
        fieldBuilder(f);
        this.fields.Add(f);
        return this;
    }

    public class Footer
    {
        public string text { get; set; }
        public string icon_url { get; set; }
        public string proxy_icon_url { get; set; }
    }
    public class Image
    {
        public string url { get; set; }
        public string proxy_url { get; set; }
        public int height { get; set; }
        public int width { get; set; }
    }
    public class Thumbnail
    {
        public string url { get; set; }
        public string proxy_url { get; set; }
        public int height { get; set; }
        public int width { get; set; }
    }
    public class Author
    {
        public string name { get; set; }
        public string url { get; set; }
        public string icon_url { get; set; }
        public string proxy_icon_url { get; set; }
    }
    public class Field
    {
        public string name { get; set; }
        public string value { get; set; }
        public bool inline { get; set; }
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore IDE1006 // Naming Styles
