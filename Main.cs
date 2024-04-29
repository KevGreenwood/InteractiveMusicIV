using GTA;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Un4seen.Bass;

namespace InteractiveMusicIV
{
    public class Main : Script
    {
        #region Variables and Enums
        private bool tempBool;
        private bool isHandleCurrentlyFadingOut;
        private bool loop;
        private bool fadeOut;
        private bool fadeIn;
        private bool enableWMusic;
        private bool enableFMusic;
        private bool enableDMusic;
        private bool enableBMusic;
        private bool enableMFMusic;
        private bool useGlobal_WMusic;
        private bool useGlobal_FMusic;
        private bool useGlobal_DMusic;
        private bool useGlobal_BMusic;
        private bool useGlobal_MFMusic;

        private int initalVolume;
        private int musicHandle;
        private int rndSeed;
        private int fadingSpeed;

        private Random rnd;

        private string[] wantedMusic3;
        private string[] wantedMusic4;
        private string[] wantedMusic5;
        private string[] flyingMusic;
        private string[] deathMusic;
        private string[] bustedMusic;
        private string[] failedMusic;

        // Main Global Dir
        private readonly string WantedMusic = Game.InstallFolder + @"\scripts\InteractiveMusicIV\WantedMusic";
        private readonly string FlyingMusic = Game.InstallFolder + @"\scripts\InteractiveMusicIV\FlyingMusic";
        private readonly string DeathMusic = Game.InstallFolder + @"\scripts\InteractiveMusicIV\DeathMusic";
        private readonly string BustedMusic = Game.InstallFolder + @"\scripts\InteractiveMusicIV\BustedMusic";
        private readonly string MissionFailedMusic = Game.InstallFolder + @"\scripts\InteractiveMusicIV\MissionFailedMusic";

        // Local Dir
        private readonly string IVFolder = @"\IV";
        private readonly string TLADFolder = @"\TLAD";
        private readonly string TBOGTFolder = @"\TBOGT";


        private enum AudioPlayMode
        {
            Play,
            Pause,
            Stop,
            None
        }
        #endregion

        #region Methods
        private int CreateFile(string file, bool createWithZeroDecibels, bool dontDestroyOnStreamEnd = false, bool loopStream = false)
        {
            if (!string.IsNullOrWhiteSpace(file))
            {
                if (createWithZeroDecibels)
                {
                    if (dontDestroyOnStreamEnd)
                    {
                        int handle;
                        if (loopStream)
                        {
                            handle = Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_STREAM_PRESCAN | BASSFlag.BASS_MUSIC_LOOP);
                        }
                        else
                        {
                            handle = Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_STREAM_PRESCAN);
                        }
                        SetStreamVolume(handle, 0f);
                        return handle;
                    }
                    else
                    {
                        int handle = Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_STREAM_AUTOFREE);
                        SetStreamVolume(handle, 0f);
                        return handle;
                    }
                }
                else
                {
                    if (dontDestroyOnStreamEnd)
                    {
                        return Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_STREAM_PRESCAN);
                    }
                    else
                    {
                        return Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_STREAM_AUTOFREE);
                    }
                }
            }
            else
            {
                return 0;
            }
        }
        public bool SetStreamVolume(int stream, float volume)
        {
            if (stream != 0)
            {
                return Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, volume / 100.0F);
            }
            else
            {
                return false;
            }
        }
        private AudioPlayMode GetStreamPlayMode(int stream)
        {
            if (stream != 0)
            {
                switch (Bass.BASS_ChannelIsActive(stream))
                {
                    case BASSActive.BASS_ACTIVE_PLAYING:
                        return AudioPlayMode.Play;
                    case BASSActive.BASS_ACTIVE_PAUSED:
                        return AudioPlayMode.Pause;
                    case BASSActive.BASS_ACTIVE_STOPPED:
                        return AudioPlayMode.Stop;
                    default:
                        return AudioPlayMode.None;
                }
            }
            else
            {
                return AudioPlayMode.None;
            }
        }
        private async void FadeStreamOut(int stream, AudioPlayMode after, int fadingSpeed = 1000)
        {
            if (!isHandleCurrentlyFadingOut)
            {
                isHandleCurrentlyFadingOut = true;

                float handleVolume = 0f;
                Bass.BASS_ChannelSlideAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, 0f, fadingSpeed);

                while (Bass.BASS_ChannelIsActive(stream) == BASSActive.BASS_ACTIVE_PLAYING)
                {
                    Bass.BASS_ChannelGetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, ref handleVolume);

                    if (handleVolume <= 0f)
                    {
                        switch (after)
                        {
                            case AudioPlayMode.Stop:
                                Bass.BASS_ChannelStop(stream);
                                isHandleCurrentlyFadingOut = false;
                                musicHandle = 0;
                                break;
                            case AudioPlayMode.Pause:
                                Bass.BASS_ChannelPause(stream);
                                isHandleCurrentlyFadingOut = false;
                                musicHandle = 0;
                                break;
                        }
                        break;
                    }

                    await Task.Delay(5);
                }
            }
        }
        private void FadeStreamIn(int stream, float fadeToVolumeLevel, int fadingSpeed)
        {
            Bass.BASS_ChannelPlay(stream, false);
            Bass.BASS_ChannelSlideAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, fadeToVolumeLevel / 100.0f, fadingSpeed);
        }

        private void PlayRandomSoundtrack(string[] musicFiles)
        {
            try
            {
                if (loop)
                {
                    musicHandle = CreateFile(musicFiles[rnd.Next(0, musicFiles.Length)], fadeIn, true, true);
                }
                else
                {
                    musicHandle = CreateFile(musicFiles[rnd.Next(0, musicFiles.Length)], fadeIn);
                }

                if (musicHandle != 0)
                {
                    if (fadeIn)
                    {
                        FadeStreamIn(musicHandle, initalVolume, fadingSpeed);
                    }
                    else
                    {
                        Bass.BASS_ChannelPlay(musicHandle, false);
                    }
                }
                else
                {
                    Game.Console.Print("InteractiveMusicIV could not play file. musicHandle was zero.");
                }
            }
            catch (Exception ex)
            {
                Game.Console.Print("InteractiveMusicIV error in Play method. Details: " + ex.ToString());
            }
        }
        private void StopSoundtrack(bool instant = false)
        {
            if (musicHandle != 0)
            {
                if (GetStreamPlayMode(musicHandle) == AudioPlayMode.Play)
                {
                    if (instant)
                    {
                        Bass.BASS_ChannelStop(musicHandle);
                        musicHandle = 0;
                    }
                    else
                    {
                        if (fadeOut)
                        {
                            FadeStreamOut(musicHandle, AudioPlayMode.Stop, fadingSpeed);
                        }
                        else
                        {
                            Bass.BASS_ChannelStop(musicHandle);
                            musicHandle = 0;
                        }
                    }
                }
                else
                {
                    Bass.BASS_ChannelStop(musicHandle);
                    musicHandle = 0;
                }
            }
        }
        #endregion

        public Main()
        {
            try
            {
                // Setup Bass.dll
                Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);

                // Get and set settings
                rndSeed = Settings.GetValueInteger("RndSeed", "General", DateTime.Now.Millisecond);
                // Set new random
                rnd = new Random(rndSeed);
                LoadSettings();
                this.Interval = 100;
                this.Tick += InteractiveMusicIV_Tick;
                this.ConsoleCommand += InteractiveMusicIV_Command;
            }
            catch (Exception ex)
            {
                Game.Console.Print("VWantedMusic error: " + ex.ToString() + " - Please let the developer know about this problem.");
            }
        }

        private void LoadSettings()
        {
            loop = Settings.GetValueBool("Loop", "Music", false);
            fadeOut = Settings.GetValueBool("FadeOut", "Music", true);
            fadeIn = Settings.GetValueBool("FadeIn", "Music", true);
            fadingSpeed = Settings.GetValueInteger("FadingSpeed", "Music", 3000);
            initalVolume = Settings.GetValueInteger("Volume", "Music", 20);
        }

        private void InteractiveMusicIV_Command(object sender, ConsoleEventArgs e)
        {
            switch (e.Command.ToLower())
            {
                case "imusic:reloadsettings":
                    try
                    {
                        Game.Console.Print("InteractiveMusicIV: Reloading settings...");
                        LoadSettings();
                        Game.Console.Print("InteractiveMusicIV: Ready.");
                    }
                    catch (Exception ex)
                    {
                        Game.Console.Print("InteractiveMusicIV error while reloading settings: " + ex.Message);
                    }
                    break;
            }
        }

        private void InteractiveMusicIV_Tick(object sender, EventArgs e)
        {
            if (Directory.Exists(WantedMusic))
            {
                if (useGlobal_WMusic)
                {
                    wantedMusic3 = Directory.EnumerateFiles(WantedMusic + @"\ThirdLevel").Where(file => Path.GetExtension(file) == ".mp3" || Path.GetExtension(file) == ".wav").ToArray();
                    wantedMusic4 = Directory.EnumerateFiles(WantedMusic + @"\FourthLevel").Where(file => Path.GetExtension(file) == ".mp3" || Path.GetExtension(file) == ".wav").ToArray();
                    wantedMusic5 = Directory.EnumerateFiles(WantedMusic + @"\FifthLevel").Where(file => Path.GetExtension(file) == ".mp3" || Path.GetExtension(file) == ".wav").ToArray();
                }
                else
                {
                    wantedMusic3 = Directory.EnumerateFiles(WantedMusic + IVFolder + @"\ThirdLevel").Where(file => Path.GetExtension(file) == ".mp3" || Path.GetExtension(file) == ".wav").ToArray();
                    wantedMusic4 = Directory.EnumerateFiles(WantedMusic + TLADFolder + @"\FourthLevel").Where(file => Path.GetExtension(file) == ".mp3" || Path.GetExtension(file) == ".wav").ToArray();
                    wantedMusic5 = Directory.EnumerateFiles(WantedMusic + TBOGTFolder + @"\FifthLevel").Where(file => Path.GetExtension(file) == ".mp3" || Path.GetExtension(file) == ".wav").ToArray();
                }
                
                if (wantedMusic3.Length != 0 || wantedMusic4.Length != 0 || wantedMusic5.Length != 0)
                {
                    switch (Game.LocalPlayer.WantedLevel)
                    {
                        case 0:
                            if (tempBool)
                            {
                                StopSoundtrack();
                                tempBool = false;
                            }

                            if (!isHandleCurrentlyFadingOut)
                            {
                                if (GetStreamPlayMode(musicHandle) == AudioPlayMode.Play)
                                {
                                    if (fadeOut)
                                    {
                                        FadeStreamOut(musicHandle, AudioPlayMode.Stop, fadingSpeed);
                                    }
                                    else
                                    {
                                        Bass.BASS_ChannelStop(musicHandle);
                                        musicHandle = 0;
                                    }
                                }
                            }
                            break;

                        case 3:
                            if (!isHandleCurrentlyFadingOut)
                            {
                                if (!tempBool)
                                {
                                    PlayRandomSoundtrack(wantedMusic3);
                                    tempBool = true;
                                }
                            }

                            break;

                        case 4:
                            if (!isHandleCurrentlyFadingOut)
                            {
                                if (!tempBool)
                                {
                                    PlayRandomSoundtrack(wantedMusic4);
                                    tempBool = true;
                                }
                            }

                            break;

                        case 5:
                        case 6:
                            if (!isHandleCurrentlyFadingOut)
                            {
                                if (!tempBool)
                                {
                                    PlayRandomSoundtrack(wantedMusic5);
                                    tempBool = true;
                                }
                            }
                            break;
                    }
                }
            }
        }
    }
}