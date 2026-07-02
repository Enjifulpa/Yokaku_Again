using UnityEngine;

/// <summary>
/// Static holder buat nyimpen state player sesaat sebelum encounter battle terjadi,
/// supaya pas kabur (Escape) dari battle, player bisa balik lagi ke posisi & scene asalnya.
///
/// Ini class static biasa (bukan MonoBehaviour/DontDestroyOnLoad) karena static field
/// otomatis nge-survive perpindahan scene selama masih dalam 1 sesi Play yang sama.
/// </summary>
public static class EncounterState
{
    public static bool HasPendingReturn { get; private set; }
    public static string PreviousSceneName { get; private set; }
    public static Vector3 PlayerPosition { get; private set; }
    public static string PlayerDirection { get; private set; }

    /// <summary>Dipanggil PlayerMovement pas encounter mulai, sebelum load scene Battle.</summary>
    public static void SaveEncounter(string sceneName, Vector3 position, string direction)
    {
        PreviousSceneName = sceneName;
        PlayerPosition = position;
        PlayerDirection = direction;
        HasPendingReturn = true;
    }

    /// <summary>Dipanggil pas posisi udah selesai di-restore, biar gak ke-apply dua kali.</summary>
    public static void Consume()
    {
        HasPendingReturn = false;
    }
}
