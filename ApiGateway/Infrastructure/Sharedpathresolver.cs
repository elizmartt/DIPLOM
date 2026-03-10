using Microsoft.Extensions.Logging;

namespace ApiGateway.Core.Infrastructure;

public static class SharedPathResolver
{
    private const string EnvVariable = "SHARED_DATA_PATH";
    private static string? _cachedRoot;

    public static string GetSharedDataRoot()
    {
        if (_cachedRoot != null) return _cachedRoot;

        // 1. Environment variable (Docker)
        var fromEnv = Environment.GetEnvironmentVariable(EnvVariable);
        if (!string.IsNullOrWhiteSpace(fromEnv) && Directory.Exists(fromEnv))
            return _cachedRoot = fromEnv;

        // 2. Walk up from CWD (dotnet run sets CWD to ApiGateway project folder)
        //    and find the OUTERMOST shared-data that actually contains medical-images
        var candidates = new List<string>();

        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var dir = new DirectoryInfo(start);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "shared-data");
                if (Directory.Exists(Path.Combine(candidate, "medical-images")))
                    candidates.Add(candidate);
                dir = dir.Parent;
            }
        }

        if (candidates.Count > 0)
        {
            // Pick the one highest up the tree (shortest path = outermost = solution root)
            var best = candidates
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p.Length)
                .First();
            return _cachedRoot = best;
        }

        // 3. Last resort — create beside exe
        var fallback = Path.Combine(AppContext.BaseDirectory, "shared-data");
        Directory.CreateDirectory(Path.Combine(fallback, "medical-images"));
        return _cachedRoot = fallback;
    }

    public static string GetMedicalImagesPath()
    {
        var path = Path.Combine(GetSharedDataRoot(), "medical-images");
        Directory.CreateDirectory(path);
        return path;
    }

    public static string GetCaseImagesPath(Guid caseId)
    {
        var path = Path.Combine(GetMedicalImagesPath(), caseId.ToString());
        Directory.CreateDirectory(path);
        return path;
    }

    public static void LogResolvedPaths(ILogger logger, Guid caseId)
    {
        var root    = GetSharedDataRoot();
        var caseDir = GetCaseImagesPath(caseId);

        logger.LogInformation("=== SharedPathResolver ===");
        logger.LogInformation("  Resolved root: {R}", root);
        logger.LogInformation("  Case folder  : {F}", caseDir);
        logger.LogInformation("  Exists       : {E}", Directory.Exists(caseDir));

        if (Directory.Exists(caseDir))
        {
            var files = Directory.GetFiles(caseDir);
            logger.LogInformation("  Files ({N}):", files.Length);
            foreach (var f in files)
                logger.LogInformation("    {File}", Path.GetFileName(f));
        }
        else
        {
            // Show all shared-data candidates found to help diagnose
            logger.LogWarning("  Case folder missing. Scanning for ALL shared-data folders:");
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var c = Path.Combine(dir.FullName, "shared-data");
                if (Directory.Exists(c))
                    logger.LogWarning("    Found: {P} (has medical-images: {M})",
                        c, Directory.Exists(Path.Combine(c, "medical-images")));
                dir = dir.Parent;
            }
        }
        logger.LogInformation("==========================");
    }
}