using System;
using System.IO;
using UnityEngine;

public class MeleeMetadataWatcher : IDisposable
{
    public event EventHandler<MeleeMetadataChangedEventArgs> Changed;
    protected virtual void OnChanged(SUVMeleeMetadata metadata)
    {
        Changed?.Invoke(this, new MeleeMetadataChangedEventArgs
        {
            Metadata = metadata
        });
    }

    private readonly FileSystemWatcher fsWatcher = new FileSystemWatcher
    {
        NotifyFilter = NotifyFilters.LastWrite,
        IncludeSubdirectories = false
    };

    public void Dispose()
    {
        fsWatcher.Dispose();
    }

    public void BeginWatchingAtPath(string meleeMetadataPath)
    {
        fsWatcher.Path = Path.GetDirectoryName(meleeMetadataPath);
        fsWatcher.Filter = Path.GetFileName(meleeMetadataPath);
        fsWatcher.Changed += (sender, e) =>
        {
            HandleUpdate(meleeMetadataPath);
        };
        fsWatcher.EnableRaisingEvents = true;

        // Kick off an update immediately
        HandleUpdate(meleeMetadataPath);
    }

    private void HandleUpdate(string path)
    {
        Debug.Log($"Update to {path}");
        var json = File.ReadAllText(path);
        var metadata = JsonUtility.FromJson<SUVMeleeMetadata>(json);
        OnChanged(metadata);
    }
}

/// <summary>
/// Based on ScoreboardInfo.json
/// </summary>
[Serializable]
public class SUVMeleeMetadata
{
    public string p1Name;
    public string p1Team;
    public string p1Character;
    public string p1Skin;
    public string p1Color;
    public int p1Score;
    public string p1WL;
    public string p2Name;
    public string p2Team;
    public string p2Character;
    public string p2Skin;
    public string p2Color;
    public int p2Score;
    public string p2WL;
    public string bestOf;
    public string round;
    public string tournamentName;
    public string caster1Name;
    public string caster1Twitter;
    public string caster1Twitch;
    public string caster2Name;
    public string caster2Twitter;
    public string caster2Twitch;
    public bool allowIntro;
}

public class MeleeMetadataChangedEventArgs
{
    public SUVMeleeMetadata Metadata;
}