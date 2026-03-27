namespace Cleanse11.Views;

public class MountedImageItem
{
    public string Path { get; }
    public string Image { get; }
    public bool Ready { get; }

    public MountedImageItem(string path, string image, bool ready)
    {
        Path = path;
        Image = image;
        Ready = ready;
    }
}
