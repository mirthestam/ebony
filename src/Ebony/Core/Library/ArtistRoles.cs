namespace Ebony.Core.Library;

[Flags]
public enum ArtistRoles
{
    None = 0,

    /// <summary>
    ///     Someone who plays or sings the music written by a composer.
    /// </summary>
    /// <example>Itzhak Perlman (Violinist)</example>
    /// <example>Maria Callas (Opera singer)</example>
    /// <example>Lang Lang (Pianist)</example>
    /// <remarks>Used for individual musicians  or vocalists.</remarks>
    /// <seealso cref="ArtistRoles.Soloist" />
    Performer = 1 << 0,

    /// <summary>
    ///     A group of musicians performing together.
    /// </summary>
    /// <example>Warsaw Girls’ Choir</example>
    /// <example>Berliner Philharmoniker (Orchestra)</example>
    /// <remarks>Used for groups of musicians performing together.</remarks>
    Ensemble = 1 << 1,

    /// <summary>
    ///     Someone who writes or creates music, usually in the form of scores that  others later perform.
    /// </summary>
    /// <example>Johann Sebastian Bach</example>
    /// <example>Frédéric Chopin</example>
    Composer = 1 << 2,

    /// <summary>
    ///     The person who leads and directs a musical ensemble
    /// </summary>
    /// <example>Leonard Bernstein</example>
    /// <example>Herbert Von Karajan</example>
    Conductor = 1 << 3,

    /// <summary>
    ///     Someone who adapts or reworks an existing piece of music for a different instrumentation, style,  or purpose.
    /// </summary>
    /// <example>Maurice Ravel (Pictures at an Exhibition,  composed by Mussorgsky)</example>
    Arranger = 1 << 4,

    /// <summary>
    ///     A performer highlighted as the main or featured artist in a work, often in concertos or solo pieces.
    /// </summary>
    /// <example>Lang Lang (Piano Soloist in Rachmaninoff Piano Concerto No. 3)</example>
    /// <example>Itzhak Perlman (Violin Soloist in Tchaikovsky Violin Concerto)</example>
    /// <remarks>Should also be in performers.This  just a 'highlight'.</remarks>
    /// <seealso cref="ArtistRoles.Performer" />
    /// ///
    Soloist = 1 << 5,
    
    /// <summary>
    /// Role for an artist where the actual role is unknown but he has hone
    /// </summary>
    Unknown = 1 << 6
}