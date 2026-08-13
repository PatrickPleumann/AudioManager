namespace AudioFramework.Services.Mixing
{
    /// <summary>
    /// What a <see cref="CategoryVolumeWriter"/> write did to the volume dictionary. Reported as a value rather
    /// than a message so the pure layer stays free of Unity logging and the tests never depend on wording.
    /// </summary>
    public enum CategoryVolumeWriteOutcome
    {
        Updated,
        EntryCreated
    }
}
