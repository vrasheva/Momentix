namespace Momentix.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("AlbumDetailsPage", typeof(Views.AlbumDetailsPage));
            Routing.RegisterRoute("CreateAlbumPage", typeof(Views.CreateAlbumPage));
            Routing.RegisterRoute("CreateTimeCapsulePage", typeof(Views.CreateTimeCapsulePage));
            Routing.RegisterRoute("TimeCapsuleDetailsPage", typeof(Views.TimeCapsuleDetailsPage));
        }
    }
}
