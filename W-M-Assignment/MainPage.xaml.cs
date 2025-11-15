namespace Demo;

public record Photo(string PhotoURL, string Caption);

public partial class MainPage : ContentPage
{
    private List<Photo> photos =
    [
        new("photo_0.jpg", "Developed by LEE JI EUN"),
        new("photo_1.jpg", "JENNIE KIM"),
        new("photo_2.jpg", "KIM JI SOO"),
        new("photo_3.jpg", "LALISA MANOBAN"),
        new("photo_4.jpg", "And ROSEANNE PARK"),
    ];

    public MainPage()
    {
        InitializeComponent();

        // TODO
    }
}
