using System;
using System.Collections.Generic;
using System.IO;
using PiedrasIncr.Audio;
using UnityEngine;

namespace PiedrasIncr.Options
{
    [Serializable]
    public sealed class OptionsData
    {
        public int version = 4;
        public int width;
        public int height;
        public int windowMode;
        public int quality;
        public int vSync;
        public int fpsLimit;
        public float masterVolume = 1f;
        public float sfxVolume = 1f;
        public float musicVolume = 1f;
        public float uiVolume = 1f;
        public float ambientVolume = 1f;
        public bool showFps;
        public bool pauseInMenu = true;
        public int palette;
        public int customR = 71;
        public int customG = 216;
        public int customB = 227;

        public OptionsData Copy()
        {
            return (OptionsData)MemberwiseClone();
        }
    }

    public static class PiedrasOptionsService
    {
        private const int Version = 4;
        private static readonly int[] FpsChoices = { -1, 30, 60, 90, 120, 144, 165, 240 };
        private static OptionsData defaults;
        private static OptionsData current;
        private static bool initialized;
        private static bool audioReady;

        public static event Action<OptionsData> Changed;
        public static IReadOnlyList<int> FpsOptions => FpsChoices;
        public static bool ShowFps
        {
            get
            {
                Initialize();
                return current.showFps;
            }
        }
        public static string FilePath => Path.Combine(Application.persistentDataPath, "PiedrasIncrOptions", "settings.json");

        public static OptionsData Current
        {
            get
            {
                Initialize();
                return current.Copy();
            }
        }

        public static OptionsData Defaults
        {
            get
            {
                Initialize();
                return defaults.Copy();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            initialized = false;
            audioReady = false;
            Changed = null;
            Initialize();
        }

        public static void Initialize()
        {
            if (initialized)
                return;

            initialized = true;
            defaults = CreateDefaults();
            current = defaults.Copy();
            OptionsData loaded = LoadFromFile(FilePath);
            if (loaded == null)
                loaded = LoadFromFile(FilePath + ".bak");

            if (loaded != null)
                current = Validate(loaded);

            Apply(current, loaded != null);
        }

        private static OptionsData LoadFromFile(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                OptionsData loaded = JsonUtility.FromJson<OptionsData>(File.ReadAllText(path));
                if (loaded == null || loaded.version < 1 || loaded.version > Version)
                    return null;
                if (loaded.version == 1)
                {
                    loaded.showFps = false;
                    loaded.pauseInMenu = true;
                }
                if (loaded.version < 3)
                {
                    loaded.sfxVolume = AudioManager.GetBusVolume(AudioBus.SFX);
                    loaded.musicVolume = AudioManager.GetBusVolume(AudioBus.Musica);
                    loaded.uiVolume = AudioManager.GetBusVolume(AudioBus.UI);
                    loaded.ambientVolume = AudioManager.GetBusVolume(AudioBus.Ambiente);
                }
                if (loaded.version < 4)
                {
                    loaded.palette = 0;
                    loaded.customR = 71;
                    loaded.customG = 216;
                    loaded.customB = 227;
                }
                loaded.version = Version;
                return loaded;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Piedras Options] No se pudo leer " + path + ": " + e.Message);
                return null;
            }
        }

        public static OptionsData Validate(OptionsData data)
        {
            InitializeIfNeeded();
            if (data == null)
                return defaults.Copy();

            OptionsData result = data.Copy();
            result.version = Version;
            result.windowMode = Mathf.Clamp(result.windowMode, 0, 1);
            result.quality = Mathf.Clamp(result.quality, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            result.vSync = Mathf.Clamp(result.vSync, 0, 2);
            result.palette = Mathf.Clamp(result.palette, 0, 5);
            result.customR = Mathf.Clamp(result.customR, 0, 255);
            result.customG = Mathf.Clamp(result.customG, 0, 255);
            result.customB = Mathf.Clamp(result.customB, 0, 255);
            if (float.IsNaN(result.masterVolume) || float.IsInfinity(result.masterVolume))
                result.masterVolume = defaults.masterVolume;
            result.masterVolume = Mathf.Clamp01(result.masterVolume);
            result.sfxVolume = SafeVolume(result.sfxVolume);
            result.musicVolume = SafeVolume(result.musicVolume);
            result.uiVolume = SafeVolume(result.uiVolume);
            result.ambientVolume = SafeVolume(result.ambientVolume);

            if (Array.IndexOf(FpsChoices, result.fpsLimit) < 0)
                result.fpsLimit = defaults.fpsLimit;

            if (result.width < 640 || result.height < 480)
            {
                result.width = defaults.width;
                result.height = defaults.height;
            }

            return result;
        }

        private static float SafeVolume(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp01(value);
        }

        public static void ActivateAudio()
        {
            Initialize();
            audioReady = true;
            PreviewAudio(current);
        }

        public static void PreviewAudio(OptionsData data)
        {
            if (data == null) return;
            AudioListener.volume = SafeVolume(data.masterVolume);
            AudioManager.SetMasterVolume(1f);
            AudioManager.SetBusVolume(AudioBus.SFX, SafeVolume(data.sfxVolume));
            AudioManager.SetBusVolume(AudioBus.Musica, SafeVolume(data.musicVolume));
            AudioManager.SetBusVolume(AudioBus.UI, SafeVolume(data.uiVolume));
            AudioManager.SetBusVolume(AudioBus.Ambiente, SafeVolume(data.ambientVolume));
        }

        private static void InitializeIfNeeded()
        {
            if (!initialized)
                Initialize();
        }

        public static void Apply(OptionsData data, bool applyDisplay)
        {
            OptionsData valid = Validate(data);
            QualitySettings.SetQualityLevel(valid.quality, true);
            QualitySettings.vSyncCount = valid.vSync;
            Application.targetFrameRate = valid.fpsLimit;
            AudioListener.volume = valid.masterVolume;
            if (audioReady)
                PreviewAudio(valid);
#if !UNITY_EDITOR
            if (applyDisplay)
            {
                FullScreenMode mode = valid.windowMode == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                Screen.SetResolution(valid.width, valid.height, mode);
            }
#endif
            Changed?.Invoke(valid.Copy());
        }

        public static bool Save(OptionsData data)
        {
            OptionsData valid = Validate(data);
            string path = FilePath;
            string temporary = path + ".tmp";
            string backup = path + ".bak";

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporary, JsonUtility.ToJson(valid, true));
                OptionsData check = LoadFromFile(temporary);
                if (check == null)
                    throw new IOException("No se pudo verificar el archivo temporal.");

                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(temporary, path, backup);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        ReplaceFallback(temporary, path, backup);
                    }
                    catch (NotSupportedException)
                    {
                        ReplaceFallback(temporary, path, backup);
                    }
                    catch (IOException)
                    {
                        ReplaceFallback(temporary, path, backup);
                    }
                }
                else
                {
                    File.Move(temporary, path);
                }

                current = valid.Copy();
                PreviewAudio(current);
                PlayerPrefs.Save();
                Changed?.Invoke(current.Copy());
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[Piedras Options] No se pudieron guardar las opciones: " + e.Message);
                return false;
            }
            finally
            {
                try
                {
                    if (File.Exists(temporary))
                        File.Delete(temporary);
                }
                catch (IOException) { }
            }
        }

        private static void ReplaceFallback(string temporary, string path, string backup)
        {
            File.Copy(path, backup, true);
            File.Copy(temporary, path, true);
            File.Delete(temporary);
        }

        public static Color GetAccent(OptionsData data)
        {
            switch (data == null ? 0 : Mathf.Clamp(data.palette, 0, 5))
            {
                case 1: return new Color32(255, 190, 86, 255);
                case 2: return new Color32(203, 139, 250, 255);
                case 3: return new Color32(90, 220, 166, 255);
                case 4: return new Color32(255, 126, 119, 255);
                case 5: return new Color32((byte)Mathf.Clamp(data.customR, 0, 255), (byte)Mathf.Clamp(data.customG, 0, 255), (byte)Mathf.Clamp(data.customB, 0, 255), 255);
                default: return new Color32(71, 216, 227, 255);
            }
        }

        public static List<Vector2Int> GetResolutions()
        {
            var resolutions = new List<Vector2Int>();
            var seen = new HashSet<string>();
            foreach (Resolution resolution in Screen.resolutions)
            {
                if (resolution.width < 640 || resolution.height < 480)
                    continue;
                string key = resolution.width + "x" + resolution.height;
                if (seen.Add(key))
                    resolutions.Add(new Vector2Int(resolution.width, resolution.height));
            }

            string currentKey = Screen.width + "x" + Screen.height;
            if (seen.Add(currentKey))
                resolutions.Add(new Vector2Int(Screen.width, Screen.height));

            resolutions.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            return resolutions;
        }

        private static OptionsData CreateDefaults()
        {
            int fps = Application.targetFrameRate;
            if (Array.IndexOf(FpsChoices, fps) < 0)
                fps = -1;

            return new OptionsData
            {
                version = Version,
                width = Mathf.Max(640, Application.isEditor ? Screen.currentResolution.width : Screen.width),
                height = Mathf.Max(480, Application.isEditor ? Screen.currentResolution.height : Screen.height),
                windowMode = Screen.fullScreenMode == FullScreenMode.Windowed ? 0 : 1,
                quality = QualitySettings.GetQualityLevel(),
                vSync = Mathf.Clamp(QualitySettings.vSyncCount, 0, 2),
                fpsLimit = fps,
                masterVolume = AudioListener.volume,
                sfxVolume = AudioManager.GetBusVolume(AudioBus.SFX),
                musicVolume = AudioManager.GetBusVolume(AudioBus.Musica),
                uiVolume = AudioManager.GetBusVolume(AudioBus.UI),
                ambientVolume = AudioManager.GetBusVolume(AudioBus.Ambiente),
                showFps = false,
                pauseInMenu = true,
                palette = 0,
                customR = 71,
                customG = 216,
                customB = 227
            };
        }
    }
}
