using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.AIStory
{
    /// <summary>
    /// Đọc prompt markdown từ AWS S3 và cache trong memory suốt vòng đời Lambda container.
    ///
    /// Chiến lược cache:
    ///   - Lambda container sống lâu (vài phút đến vài giờ) giữa các lần invoke.
    ///   - Prompt files (< 10 KB) được cache trong static Dictionary → chỉ S3 GET lần đầu.
    ///   - Nếu muốn clear cache (update prompt), chỉ cần deploy lại Lambda hoặc
    ///     wait cho Lambda container expire tự nhiên (~15 phút idle).
    ///
    /// Cấu trúc S3 mong đợi:
    ///   s3://<ASSETS_BUCKET_NAME>/prompts/system_prompt.md
    ///   s3://<ASSETS_BUCKET_NAME>/prompts/story_prompt.md
    /// </summary>
    public class S3PromptLoader : IPromptLoader
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly ILogger<S3PromptLoader> _logger;

        // Cache static — tồn tại suốt vòng đời Lambda container (warm invocations)
        // Giảm S3 GET request xuống còn 1 lần duy nhất per container
        private static readonly Dictionary<string, string> _cache = new();
        private static readonly SemaphoreSlim _cacheLock = new(1, 1);

        // S3 key prefix cho prompt files
        private const string PromptPrefix = "prompts/";

        public S3PromptLoader(IAmazonS3 s3Client, string bucketName, ILogger<S3PromptLoader> logger)
        {
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
            _bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<string> GetSystemPromptAsync() =>
            GetCachedAsync("system_prompt.md");

        public Task<string> GetStoryPromptAsync() =>
            GetCachedAsync("story_prompt.md");

        /// <summary>
        /// Đọc file từ S3 với cache. Thread-safe nhờ SemaphoreSlim.
        /// </summary>
        private async Task<string> GetCachedAsync(string fileName)
        {
            var cacheKey = $"{_bucketName}/{PromptPrefix}{fileName}";

            // Fast path: cache hit (không lock)
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                _logger.LogInformation("[S3PromptLoader] Cache hit: {Key}", fileName);
                return cached;
            }

            // Slow path: cần đọc từ S3
            await _cacheLock.WaitAsync();
            try
            {
                // Double-check sau khi lấy lock
                if (_cache.TryGetValue(cacheKey, out cached))
                    return cached;

                var content = await FetchFromS3Async(fileName);
                _cache[cacheKey] = content;
                _logger.LogInformation("[S3PromptLoader] Loaded from S3 and cached: {Key}", fileName);
                return content;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private async Task<string> FetchFromS3Async(string fileName)
        {
            var key = $"{PromptPrefix}{fileName}";
            _logger.LogInformation("[S3PromptLoader] Fetching from S3: s3://{Bucket}/{Key}", _bucketName, key);

            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key
                };

                using var response = await _s3Client.GetObjectAsync(request);
                using var reader = new StreamReader(response.ResponseStream);
                return await reader.ReadToEndAsync();
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("[S3PromptLoader] File không tìm thấy trên S3: {Key}. Dùng fallback.", key);
                return string.Empty; // PromptBuilder có fallback mặc định
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[S3PromptLoader] Lỗi khi đọc S3: {Key}", key);
                return string.Empty;
            }
        }
    }
}
