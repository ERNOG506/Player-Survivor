using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AudioAssetBuilder
{
    private const string SourceAssetPath = "Assets/SourceAudio/Speed_Dash_Souffle_Source.mp3";
    private const string MusicOutputPath = "Assets/Resources/Music/Speed_Dash_Souffle_Loop.wav";
    private const string ShotOutputPath = "Assets/Resources/SFX/PlayerShot_Alt.wav";
    private const string RestartMenuOutputPath = "Assets/Resources/SFX/RestartMenu_Alt_Loop.wav";
    private const string EnemyExplosionOutputPath = "Assets/Resources/SFX/EnemyExplosion_Alt.wav";
    private const string PlayerExplosionOutputPath = "Assets/Resources/SFX/PlayerExplosion_Alt.wav";
    private const string PowerUpClickOutputPath = "Assets/Resources/SFX/PowerUpClick_Alt.wav";

    [MenuItem("Survivor Squares/Gerar musica e som de tiro")]
    public static void BuildFromDownloads()
    {
        try
        {
            string sourcePath = FindSourceAudio();
            Directory.CreateDirectory("Assets/SourceAudio");
            Directory.CreateDirectory("Assets/Resources/Music");
            Directory.CreateDirectory("Assets/Resources/SFX");

            File.Copy(sourcePath, SourceAssetPath, overwrite: true);
            AssetDatabase.ImportAsset(SourceAssetPath, ImportAssetOptions.ForceUpdate);
            ConfigureSourceImporter(SourceAssetPath);

            AudioClip sourceClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SourceAssetPath);
            if (sourceClip == null)
            {
                throw new InvalidOperationException("Unity nao conseguiu importar a musica: " + SourceAssetPath);
            }

            float[] sourceSamples = ReadSamples(sourceClip);
            float[] trimmedMusic = TrimTrailingSilence(sourceSamples, sourceClip.channels, threshold: 0.0015f, paddingSeconds: 0f, sourceClip.frequency);
            WriteWav(MusicOutputPath, trimmedMusic, sourceClip.channels, sourceClip.frequency);

            float[] shotSamples = BuildShotSound(sourceSamples, sourceClip.channels, sourceClip.frequency);
            WriteWav(ShotOutputPath, shotSamples, channels: 1, sourceClip.frequency);

            float[] restartMenuSamples = BuildRestartMenuLoop(sourceSamples, sourceClip.channels, sourceClip.frequency);
            WriteWav(RestartMenuOutputPath, restartMenuSamples, channels: 2, sourceClip.frequency);

            float[] enemyExplosionSamples = BuildExplosionSound(sourceSamples, sourceClip.channels, sourceClip.frequency, playerVersion: false);
            WriteWav(EnemyExplosionOutputPath, enemyExplosionSamples, channels: 1, sourceClip.frequency);

            float[] playerExplosionSamples = BuildExplosionSound(sourceSamples, sourceClip.channels, sourceClip.frequency, playerVersion: true);
            WriteWav(PlayerExplosionOutputPath, playerExplosionSamples, channels: 2, sourceClip.frequency);

            float[] powerUpClickSamples = BuildPowerUpClick(sourceSamples, sourceClip.channels, sourceClip.frequency);
            WriteWav(PowerUpClickOutputPath, powerUpClickSamples, channels: 1, sourceClip.frequency);

            AssetDatabase.ImportAsset(MusicOutputPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(ShotOutputPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(RestartMenuOutputPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(EnemyExplosionOutputPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(PlayerExplosionOutputPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(PowerUpClickOutputPath, ImportAssetOptions.ForceUpdate);
            ConfigureRuntimeAudio(MusicOutputPath, stream: true);
            ConfigureRuntimeAudio(ShotOutputPath, stream: false);
            ConfigureRuntimeAudio(RestartMenuOutputPath, stream: false);
            ConfigureRuntimeAudio(EnemyExplosionOutputPath, stream: false);
            ConfigureRuntimeAudio(PlayerExplosionOutputPath, stream: false);
            ConfigureRuntimeAudio(PowerUpClickOutputPath, stream: false);
            AssetDatabase.Refresh();

            Debug.Log("AudioAssetBuilder: musica de loop, tiro, explosoes e clique gerados com sucesso.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static string FindSourceAudio()
    {
        string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        string[] matches = Directory.GetFiles(downloads, "Speed_Dash_Souffl*.mp3");
        if (matches.Length == 0)
        {
            throw new FileNotFoundException("Nao encontrei Speed_Dash_Souffle.mp3 na pasta Downloads.");
        }

        return matches[0];
    }

    private static void ConfigureSourceImporter(string assetPath)
    {
        AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
        if (importer == null)
        {
            return;
        }

        importer.forceToMono = false;
        importer.loadInBackground = false;

        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.quality = 1f;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
    }

    private static void ConfigureRuntimeAudio(string assetPath, bool stream)
    {
        AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
        if (importer == null)
        {
            return;
        }

        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = stream ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = stream ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
        settings.quality = 0.82f;
        settings.preloadAudioData = !stream;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
    }

    private static float[] ReadSamples(AudioClip clip)
    {
        if (!clip.LoadAudioData())
        {
            throw new InvalidOperationException("Nao consegui carregar os dados de audio do clip.");
        }

        float[] samples = new float[clip.samples * clip.channels];
        if (!clip.GetData(samples, 0))
        {
            throw new InvalidOperationException("Nao consegui ler as amostras da musica.");
        }

        return samples;
    }

    private static float[] TrimTrailingSilence(float[] samples, int channels, float threshold, float paddingSeconds, int frequency)
    {
        int frames = samples.Length / channels;
        int lastAudibleFrame = frames - 1;
        for (int frame = frames - 1; frame >= 0; frame--)
        {
            float peak = 0f;
            int baseIndex = frame * channels;
            for (int channel = 0; channel < channels; channel++)
            {
                peak = Mathf.Max(peak, Mathf.Abs(samples[baseIndex + channel]));
            }

            if (peak > threshold)
            {
                lastAudibleFrame = frame;
                break;
            }
        }

        int paddingFrames = Mathf.RoundToInt(paddingSeconds * frequency);
        int outputFrames = Mathf.Clamp(lastAudibleFrame + paddingFrames, 1, frames);
        float[] output = new float[outputFrames * channels];
        Array.Copy(samples, output, output.Length);
        return output;
    }

    private static float[] BuildShotSound(float[] samples, int channels, int frequency)
    {
        int sourceFrames = samples.Length / channels;
        int windowFrames = Mathf.Clamp(Mathf.RoundToInt(frequency * 0.12f), 1, sourceFrames);
        int stride = Mathf.Max(1, Mathf.RoundToInt(frequency * 0.035f));
        int bestStart = 0;
        float bestEnergy = 0f;

        for (int start = 0; start < sourceFrames - windowFrames; start += stride)
        {
            float energy = 0f;
            for (int frame = 0; frame < windowFrames; frame += 32)
            {
                energy += Mathf.Abs(ReadMono(samples, channels, start + frame));
            }

            if (energy > bestEnergy)
            {
                bestEnergy = energy;
                bestStart = start;
            }
        }

        int outputFrames = Mathf.RoundToInt(frequency * 0.18f);
        float[] output = new float[outputFrames];
        float previous = 0f;
        for (int frame = 0; frame < outputFrames; frame++)
        {
            int sourceFrame = Mathf.Min(sourceFrames - 1, bestStart + Mathf.RoundToInt(frame * 2.25f));
            float mono = ReadMono(samples, channels, sourceFrame);
            float high = mono - previous * 0.82f;
            previous = mono;

            float envelope = ShotEnvelope(frame, outputFrames);
            float shaped = Mathf.Clamp((high * 3.8f + mono * 0.7f) * envelope, -1f, 1f);
            output[frame] = (float)Math.Tanh(shaped * 2.4f) * 0.82f;
        }

        return output;
    }

    private static float[] BuildRestartMenuLoop(float[] samples, int channels, int frequency)
    {
        int sourceFrames = samples.Length / channels;
        int outputFrames = Mathf.Min(sourceFrames, Mathf.RoundToInt(frequency * 5.5f));
        int startFrame = Mathf.Clamp(Mathf.RoundToInt(sourceFrames * 0.38f), 0, Mathf.Max(0, sourceFrames - outputFrames - 1));
        float[] output = new float[outputFrames * 2];

        for (int frame = 0; frame < outputFrames; frame++)
        {
            float t = outputFrames <= 1 ? 0f : (float)frame / (outputFrames - 1);
            int sourceFrame = Mathf.Min(sourceFrames - 1, startFrame + Mathf.RoundToInt(frame * 0.55f));
            float mono = ReadMono(samples, channels, sourceFrame);
            float pad = Mathf.Sin(t * Mathf.PI * 2f) * 0.08f;
            float pulse = Mathf.Sin(t * Mathf.PI * 10f) * 0.035f;
            float envelope = Mathf.Sin(t * Mathf.PI);
            float value = Mathf.Clamp((mono * 0.28f + pad + pulse) * envelope, -0.55f, 0.55f);

            output[frame * 2] = value * 0.92f;
            output[frame * 2 + 1] = value;
        }

        CrossfadeLoop(output, 2, frequency, 0.18f);
        return output;
    }

    private static float[] BuildExplosionSound(float[] samples, int channels, int frequency, bool playerVersion)
    {
        int sourceFrames = samples.Length / channels;
        int outputFrames = Mathf.RoundToInt(frequency * (playerVersion ? 0.78f : 0.42f));
        int startFrame = Mathf.Clamp(Mathf.RoundToInt(sourceFrames * (playerVersion ? 0.52f : 0.28f)), 0, sourceFrames - 1);
        int outputChannels = playerVersion ? 2 : 1;
        float[] output = new float[outputFrames * outputChannels];

        for (int frame = 0; frame < outputFrames; frame++)
        {
            float t = outputFrames <= 1 ? 1f : (float)frame / (outputFrames - 1);
            int sourceFrame = Mathf.Min(sourceFrames - 1, startFrame + Mathf.RoundToInt(frame * (playerVersion ? 0.42f : 0.85f)));
            float music = ReadMono(samples, channels, sourceFrame);
            float noise = HashSigned(frame, playerVersion ? 331 : 173);
            float rumble = Mathf.Sin(t * Mathf.PI * (playerVersion ? 32f : 24f)) * Mathf.Exp(-t * (playerVersion ? 4.2f : 6.8f));
            float crack = noise * Mathf.Exp(-t * (playerVersion ? 7f : 10f));
            float body = music * (playerVersion ? 0.34f : 0.22f) * Mathf.Exp(-t * 2.2f);
            float value = Mathf.Clamp((crack * 0.6f + rumble * 0.45f + body) * (1f - t * 0.15f), -1f, 1f);
            value = (float)Math.Tanh(value * (playerVersion ? 2.1f : 2.8f)) * (playerVersion ? 0.9f : 0.78f);

            if (playerVersion)
            {
                float pan = Mathf.Sin(t * Mathf.PI * 3f) * 0.18f;
                output[frame * 2] = value * (1f - pan);
                output[frame * 2 + 1] = value * (1f + pan);
            }
            else
            {
                output[frame] = value;
            }
        }

        ApplyTinyFade(output, outputChannels, frequency, 0.004f, 0.05f);
        return output;
    }

    private static float[] BuildPowerUpClick(float[] samples, int channels, int frequency)
    {
        int sourceFrames = samples.Length / channels;
        int outputFrames = Mathf.RoundToInt(frequency * 0.2f);
        int startFrame = Mathf.Clamp(Mathf.RoundToInt(sourceFrames * 0.14f), 0, sourceFrames - 1);
        float[] output = new float[outputFrames];

        for (int frame = 0; frame < outputFrames; frame++)
        {
            float t = outputFrames <= 1 ? 1f : (float)frame / (outputFrames - 1);
            int sourceFrame = Mathf.Min(sourceFrames - 1, startFrame + Mathf.RoundToInt(frame * 1.8f));
            float music = ReadMono(samples, channels, sourceFrame);
            float chime = Mathf.Sin(t * Mathf.PI * 46f) * Mathf.Exp(-t * 8f);
            float sparkle = Mathf.Sin(t * Mathf.PI * 78f) * Mathf.Exp(-t * 12f);
            output[frame] = Mathf.Clamp((music * 0.35f + chime * 0.34f + sparkle * 0.16f) * (1f - t), -0.8f, 0.8f);
        }

        ApplyTinyFade(output, 1, frequency, 0.003f, 0.035f);
        return output;
    }

    private static float ReadMono(float[] samples, int channels, int frame)
    {
        int baseIndex = frame * channels;
        float sum = 0f;
        for (int channel = 0; channel < channels; channel++)
        {
            sum += samples[baseIndex + channel];
        }

        return sum / channels;
    }

    private static float ShotEnvelope(int frame, int totalFrames)
    {
        float t = totalFrames <= 1 ? 1f : (float)frame / (totalFrames - 1);
        float attack = Mathf.Clamp01(t / 0.045f);
        float decay = Mathf.Exp(-t * 7.5f);
        float release = Mathf.Clamp01((1f - t) / 0.18f);
        return attack * decay * release;
    }

    private static void ApplyTinyFade(float[] samples, int channels, int frequency, float fadeInSeconds, float fadeOutSeconds)
    {
        int frames = samples.Length / channels;
        int fadeInFrames = Mathf.Min(frames, Mathf.RoundToInt(fadeInSeconds * frequency));
        int fadeOutFrames = Mathf.Min(frames, Mathf.RoundToInt(fadeOutSeconds * frequency));

        for (int frame = 0; frame < fadeInFrames; frame++)
        {
            float gain = fadeInFrames <= 1 ? 1f : (float)frame / (fadeInFrames - 1);
            ApplyFrameGain(samples, channels, frame, gain);
        }

        for (int frame = 0; frame < fadeOutFrames; frame++)
        {
            float gain = fadeOutFrames <= 1 ? 0f : 1f - (float)frame / (fadeOutFrames - 1);
            ApplyFrameGain(samples, channels, frames - 1 - frame, gain);
        }
    }

    private static void CrossfadeLoop(float[] samples, int channels, int frequency, float seconds)
    {
        int frames = samples.Length / channels;
        int fadeFrames = Mathf.Clamp(Mathf.RoundToInt(seconds * frequency), 1, frames / 3);
        for (int frame = 0; frame < fadeFrames; frame++)
        {
            float t = (float)frame / fadeFrames;
            int headFrame = frame;
            int tailFrame = frames - fadeFrames + frame;
            for (int channel = 0; channel < channels; channel++)
            {
                int headIndex = headFrame * channels + channel;
                int tailIndex = tailFrame * channels + channel;
                float blended = Mathf.Lerp(samples[tailIndex], samples[headIndex], t);
                samples[headIndex] = blended;
                samples[tailIndex] = blended;
            }
        }
    }

    private static void ApplyFrameGain(float[] samples, int channels, int frame, float gain)
    {
        int baseIndex = frame * channels;
        for (int channel = 0; channel < channels; channel++)
        {
            samples[baseIndex + channel] *= gain;
        }
    }

    private static float HashSigned(int x, int salt)
    {
        unchecked
        {
            int hash = x * 73856093 ^ salt * 19349663;
            hash = (hash << 13) ^ hash;
            int value = hash * (hash * hash * 15731 + 789221) + 1376312589;
            return ((value & 0x7fffffff) / 1073741823.5f) - 1f;
        }
    }

    private static void WriteWav(string path, float[] samples, int channels, int frequency)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        int sampleCount = samples.Length;
        int dataBytes = sampleCount * 2;

        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataBytes);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(frequency);
            writer.Write(frequency * channels * 2);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataBytes);

            for (int i = 0; i < sampleCount; i++)
            {
                short value = (short)Mathf.RoundToInt(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                writer.Write(value);
            }
        }
    }
}
