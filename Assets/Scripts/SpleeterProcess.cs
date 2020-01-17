using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEditor;

public class SpleeterProcess : MonoBehaviour
{
    public AudioClip inputSong;
    public AudioSource[] orbs;

    void Awake()
    {
        print(inputSong.name + ".mp3");
        Process process = new Process();
        // Configure the process using the StartInfo properties.
        string filePath = Application.streamingAssetsPath + "/spleeter/spleeter/"; //Current Directory plus song path
        string outputPath = Application.persistentDataPath + "/Spleets/";
        process.StartInfo.FileName = filePath + "spleeter.exe";
        process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        print("Output:" + Application.persistentDataPath);
        process.StartInfo.Arguments = "separate -i Assets/Songs/" + inputSong.name + ".mp3 -p spleeter:4stems -o " + outputPath; //Shell executable
        print(process.StartInfo.FileName);

        process.Start();
        process.WaitForExit();

        LoadSongTrack("vocals");
        LoadSongTrack("other");
        LoadSongTrack("drums");
        LoadSongTrack("bass");

        //Play
        foreach (AudioSource audio in orbs)
        {
            audio.Play();
        }
    }

    void LoadSongTrack(string track)
    {
        string url = Application.persistentDataPath + "/Spleets/" + inputSong.name + "/" + track + ".wav";
        var bytes = File.ReadAllBytes(url);
        WAV wav = new WAV(bytes);
        AudioClip audioClip = AudioClip.Create(track, wav.SampleCount, 1, wav.Frequency, false);
        audioClip.SetData(wav.LeftChannel, 0);
        switch (track)
        {
            case "vocals":
                orbs[0].clip = audioClip;
                break;
            case "other":
                orbs[1].clip = audioClip;
                break;
            case "drums":
                orbs[2].clip = audioClip;
                break;
            case "bass":
                orbs[3].clip = audioClip;
                break;
        }
        
    }
}

public class WAV
{

    // convert two bytes to one float in the range -1 to 1
    static float bytesToFloat(byte firstByte, byte secondByte)
    {
        // convert two bytes to one short (little endian)
        short s = (short)((secondByte << 8) | firstByte);
        // convert to range from -1 to (just below) 1
        return s / 32768.0F;
    }

    static int bytesToInt(byte[] bytes, int offset = 0)
    {
        int value = 0;
        for (int i = 0; i < 4; i++)
        {
            value |= ((int)bytes[offset + i]) << (i * 8);
        }
        return value;
    }

    private static byte[] GetBytes(string filename)
    {
        return File.ReadAllBytes(filename);
    }
    // properties
    public float[] LeftChannel { get; internal set; }
    public float[] RightChannel { get; internal set; }
    public int ChannelCount { get; internal set; }
    public int SampleCount { get; internal set; }
    public int Frequency { get; internal set; }

    // Returns left and right double arrays. 'right' will be null if sound is mono.
    public WAV(string filename) :
        this(GetBytes(filename))
    { }

    public WAV(byte[] wav)
    {

        // Determine if mono or stereo
        ChannelCount = wav[22];     // Forget byte 23 as 99.999% of WAVs are 1 or 2 channels

        // Get the frequency
        Frequency = bytesToInt(wav, 24);

        // Get past all the other sub chunks to get to the data subchunk:
        int pos = 12;   // First Subchunk ID from 12 to 16

        // Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
        while (!(wav[pos] == 100 && wav[pos + 1] == 97 && wav[pos + 2] == 116 && wav[pos + 3] == 97))
        {
            pos += 4;
            int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
            pos += 4 + chunkSize;
        }
        pos += 8;

        // Pos is now positioned to start of actual sound data.
        SampleCount = (wav.Length - pos) / 2;     // 2 bytes per sample (16 bit sound mono)
        if (ChannelCount == 2) SampleCount /= 2;        // 4 bytes per sample (16 bit stereo)

        // Allocate memory (right will be null if only mono sound)
        LeftChannel = new float[SampleCount];
        if (ChannelCount == 2) RightChannel = new float[SampleCount];
        else RightChannel = null;

        // Write to double array/s:
        int i = 0;
        while (pos < wav.Length)
        {
            LeftChannel[i] = bytesToFloat(wav[pos], wav[pos + 1]);
            pos += 2;
            if (ChannelCount == 2)
            {
                RightChannel[i] = bytesToFloat(wav[pos], wav[pos + 1]);
                pos += 2;
            }
            i++;
        }
    }

    public override string ToString()
    {
        return string.Format("[WAV: LeftChannel={0}, RightChannel={1}, ChannelCount={2}, SampleCount={3}, Frequency={4}]", LeftChannel, RightChannel, ChannelCount, SampleCount, Frequency);
    }
}