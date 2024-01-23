namespace Zeenox.Models;

public class Hit
{
    public Result result { get; set; }
}

public class Meta
{
    public int status { get; set; }
}

public class Response
{
    public List<Section> sections { get; set; }
}

public class Result
{
    public string? artist_names { get; set; }
    public string? full_title { get; set; }
    public string path { get; set; }
    public string? title { get; set; }
    public string? title_with_featured { get; set; }
}

public class Root
{
    public Meta meta { get; set; }
    public Response response { get; set; }
}

public class Section
{
    public string? type { get; set; }
    public List<Hit> hits { get; set; }
}
