using System.Net;

namespace CollegeWebsitePlatform.Infrastructure.Services;

/// <summary>
/// Verifies domain ownership by checking for a specific DNS TXT record.
/// The SuperAdmin adds a TXT record to their DNS, then the backend verifies it.
/// 
/// Expected DNS TXT record format:
///   Key: _codebuff-verify
///   Value: {verificationToken}
/// </summary>
public interface IDnsVerificationService
{
    /// <summary>
    /// Generates a unique verification token for a domain.
    /// </summary>
    string GenerateVerificationToken();

    /// <summary>
    /// Checks if the domain has the correct DNS TXT record with the verification token.
    /// </summary>
    Task<DnsVerificationResult> VerifyDomainAsync(string domainName, string expectedToken);
}

public class DnsVerificationResult
{
    public bool IsVerified { get; set; }
    public string? Message { get; set; }
    public List<string> DnsRecordsFound { get; set; } = new();
}

public class DnsVerificationService : IDnsVerificationService
{
    private const string TxtRecordPrefix = "_codebuff-verify";

    public string GenerateVerificationToken()
    {
        // Generate a 32-char alphanumeric token
        return Guid.NewGuid().ToString("N")[..32];
    }

    public async Task<DnsVerificationResult> VerifyDomainAsync(string domainName, string expectedToken)
    {
        var result = new DnsVerificationResult();

        try
        {
            // Use nslookup to query TXT records for the domain
            var records = await LookupTxtRecordsAsync(domainName);

            result.DnsRecordsFound = records;

            // Check if any TXT record matches the expected format
            var expectedRecord = $"{TxtRecordPrefix}={expectedToken}";
            var matchingRecord = records.FirstOrDefault(r =>
                r.Contains(expectedToken, StringComparison.OrdinalIgnoreCase) &&
                r.Contains(TxtRecordPrefix, StringComparison.OrdinalIgnoreCase));

            if (matchingRecord != null)
            {
                result.IsVerified = true;
                result.Message = $"Domain '{domainName}' verified successfully. TXT record found: {matchingRecord}";
            }
            else
            {
                result.IsVerified = false;
                result.Message = $"Domain '{domainName}' verification failed. No matching TXT record found. " +
                    $"Expected: {expectedRecord}. " +
                    $"Add this TXT record to your DNS, then try again.";
            }
        }
        catch (Exception ex)
        {
            result.IsVerified = false;
            result.Message = $"DNS lookup failed for '{domainName}': {ex.Message}";
        }

        return result;
    }

    private async Task<List<string>> LookupTxtRecordsAsync(string domainName)
    {
        var records = new List<string>();

        try
        {
            // Use .NET's built-in DNS resolution to get TXT records
            // Note: For production, consider using DnsClient NuGet package for proper TXT record support
            var hostEntry = await Dns.GetHostEntryAsync(domainName);
            
            // .NET's built-in DNS doesn't directly expose TXT records
            // For a production implementation, use the DnsClient NuGet package:
            //   var lookup = new DnsClient.LookupClient();
            //   var result = lookup.Query(domainName, QueryType.TXT);
            //   var txtRecords = result.Answers.TXTRecords();
            
            // For now, we'll use a process-based approach with nslookup
            records = await NslookupTxtRecordsAsync(domainName);
        }
        catch
        {
            // If standard DNS fails, try nslookup
            records = await NslookupTxtRecordsAsync(domainName);
        }

        return records;
    }

    private async Task<List<string>> NslookupTxtRecordsAsync(string domainName)
    {
        var records = new List<string>();

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "nslookup",
                Arguments = $"-type=TXT {domainName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null) return records;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Parse nslookup output for TXT records
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("text =", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Contains("TXT", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract the TXT record value
                    var txtMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"text\s*=\s*""(.+?)""");
                    if (txtMatch.Success)
                    {
                        records.Add(txtMatch.Groups[1].Value);
                    }
                    else if (trimmed.Contains("\""))
                    {
                        // Try to extract quoted string
                        var quoteMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"""(.+?)""");
                        if (quoteMatch.Success)
                        {
                            records.Add(quoteMatch.Groups[1].Value);
                        }
                    }
                }
            }
        }
        catch
        {
            // nslookup not available or failed
        }

        return records;
    }
}
