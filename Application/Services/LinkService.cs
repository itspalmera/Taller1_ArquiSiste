using System.Security.Cryptography;
using System.Text;
using Shortly.Application.DTOs;
using Shortly.Application.Interfaces;
using Shortly.Domain.Entities;

namespace Shortly.Application.Services;

public sealed class LinkService : ILinkService
{
    private readonly ILogger<LinkService> _logger;
    private readonly ILinkRepository _linkRepository;

    public LinkService(ILinkRepository linkRepository, ILogger<LinkService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _linkRepository = linkRepository ?? throw new ArgumentNullException(nameof(linkRepository));
    }

    /// <summary>
    /// Genera un token corto, opaco e irreversible a partir de un ULID.
    /// Explicación de Seguridad / Privacidad:
    /// Un ULID expone directamente en sus primeros caracteres la marca de tiempo (timestamp).
    /// Aplicar SHA-256 deshace la correlación temporal y destruye la capacidad de un cliente
    /// de inferir cuándo se creó el enlace o estimar el volumen de tráfico del sistema.
    /// </summary>
    private static string GenerateOpaqueShortUrl()
    {
        byte[] ulidBytes = Ulid.NewUlid().ToByteArray();
        byte[] hashBytes = SHA256.HashData(ulidBytes);
        
        // Convertir a Base64Url (removiendo +, / y =) para garantizar compatibilidad con URLs
        return Convert.ToBase64String(hashBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=')[..12]
            .ToLowerInvariant();
    }

    public async Task<LinkResponse> CreateLink(string url, long userId)
    {
        _logger.LogDebug("Creating link for URL: {Url} and userId: {UserId}", url, userId);

        var shortUrl = GenerateOpaqueShortUrl();
        var link = new Link(url, shortUrl, userId);

        await _linkRepository.AddAsync(link);
        await _linkRepository.SaveChangesAsync();

        _logger.LogInformation("Link created successfully with shortUrl: {ShortUrl} and id: {Id}.", link.ShortUrl, link.Id);
        return LinkResponse.From(link);
    }

    public async Task<LinkResponse> IncrementClicks(long linkId)
    {
        _logger.LogDebug("Incrementing clicks for linkId: {LinkId}", linkId);

        var link = await _linkRepository.GetByIdAsync(linkId);
        if (link is null)
        {
            _logger.LogWarning("IncrementClicks failed: No link found with id {LinkId}.", linkId);
            throw new KeyNotFoundException($"No link found with id '{linkId}'.");
        }

        link.IncrementClicks();
        await _linkRepository.SaveChangesAsync();

        _logger.LogInformation("Clicks incremented for linkId: {LinkId}. Total clicks: {Clicks}.", link.Id, link.Clicks);
        return LinkResponse.From(link);
    }

    public async Task<LinkResponse> GetLink(string shortUrl)
    {
        _logger.LogDebug("Retrieving link with shortUrl: {ShortUrl}", shortUrl);

        var link = await _linkRepository.GetByShortUrlAsync(shortUrl);
        if (link is null)
        {
            _logger.LogWarning("Link not found with shortUrl {ShortUrl}.", shortUrl);
            throw new KeyNotFoundException($"No link found with shortUrl '{shortUrl}'.");
        }

        _logger.LogInformation("Link retrieved successfully with shortUrl: {ShortUrl} and id: {Id}.", link.ShortUrl, link.Id);
        return LinkResponse.From(link);
    }

    public async Task<List<LinkResponse>> GetAllLinks()
    {
        _logger.LogDebug("Retrieving all links from the database ..");
        var links = await _linkRepository.GetAllAsync();

        _logger.LogInformation("Retrieved {Count} links from the database.", links.Count);
        return links.Select(LinkResponse.From).ToList();
    }

    public async Task<List<LinkResponse>> GetLinksByUserId(long userId)
    {
        _logger.LogDebug("Retrieving links for userId: {UserId}", userId);
        var links = await _linkRepository.GetByUserIdAsync(userId);

        _logger.LogInformation("Retrieved {Count} links for userId: {UserId}.", links.Count, userId);
        return links.Select(LinkResponse.From).ToList();
    }
}