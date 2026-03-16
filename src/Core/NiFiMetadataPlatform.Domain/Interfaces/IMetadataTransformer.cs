namespace NiFiMetadataPlatform.Domain.Interfaces;

/// <summary>
/// Interface for transforming platform-specific metadata to a common format.
/// Each platform implementation should provide a transformer to convert
/// its native metadata format to the unified IMetadataEntity format.
/// </summary>
/// <typeparam name="TSource">The platform-specific metadata type.</typeparam>
public interface IMetadataTransformer<TSource>
{
    /// <summary>
    /// Transforms platform-specific metadata to the common metadata entity format.
    /// </summary>
    /// <param name="source">The platform-specific metadata object.</param>
    /// <returns>The transformed metadata entity.</returns>
    IMetadataEntity Transform(TSource source);

    /// <summary>
    /// Transforms a collection of platform-specific metadata to common format.
    /// </summary>
    /// <param name="sources">The collection of platform-specific metadata objects.</param>
    /// <returns>The collection of transformed metadata entities.</returns>
    IEnumerable<IMetadataEntity> TransformMany(IEnumerable<TSource> sources);

    /// <summary>
    /// Validates if the source metadata is in the correct format and can be transformed.
    /// </summary>
    /// <param name="source">The platform-specific metadata object.</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool Validate(TSource source);
}
