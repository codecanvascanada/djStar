// This is a static class to hold data that needs to persist between scenes,
// such as the currently selected song.
public static class GameData
{
    public static SongInfo SelectedSongInfo { get; set; }
    public static bool IsInitialCalibration { get; set; } = false;
}
