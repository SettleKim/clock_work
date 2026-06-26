using UnityEngine;

public enum GearbotAttackClip
{
    HandBlade,
    Hammer,
    ComboHandHammer,
    ComboHammerWarp,
}

public static class GearbotAttackLoader
{
    const string FrameRoot = "Gearbot/attack_frames/";

    public static Sprite[] LoadHandBlade() => LoadSequence("gearbot_attack_blade", 3);
    public static Sprite[] LoadHammer() => LoadSequence("gearbot_attack_hammer", 3);
    public static Sprite[] LoadComboHandHammer() => LoadSequence("gearbot_attack_combo_hand_hammer", 3);
    public static Sprite[] LoadComboHammerWarp() => LoadSequence("gearbot_attack_combo_hammer_warp", 4);

    public static Sprite[] LoadClip(GearbotAttackClip clip)
    {
        return clip switch
        {
            GearbotAttackClip.HandBlade => LoadHandBlade(),
            GearbotAttackClip.Hammer => LoadHammer(),
            GearbotAttackClip.ComboHandHammer => LoadComboHandHammer(),
            GearbotAttackClip.ComboHammerWarp => LoadComboHammerWarp(),
            _ => System.Array.Empty<Sprite>(),
        };
    }

    static Sprite[] LoadSequence(string prefix, int count)
    {
        var sprites = new Sprite[count];
        int loaded = 0;

        for (int i = 0; i < count; i++)
        {
            string path = FrameRoot + $"{prefix}_{i + 1:00}";
            sprites[i] = GearbotMotionLoader.LoadFrame(path);
            if (sprites[i] != null)
                loaded++;
        }

        if (loaded == 0)
            Debug.LogWarning($"GearbotAttackLoader: no sprites loaded for {prefix}.");
        else if (loaded < count)
            Debug.LogWarning($"GearbotAttackLoader: partial load for {prefix} ({loaded}/{count}).");

        return sprites;
    }
}
